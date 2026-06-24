using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Frost9.VFX.Editor;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Editor tests for project-wide and catalog-scoped VFX id discovery.
    /// </summary>
    public class VfxCatalogDiscoveryEditorTests
    {
        private const string TempFolder = "Assets/_Project/Temp/VfxDiscoveryTests";
        private readonly List<string> tempAssetPaths = new List<string>();

        /// <summary>
        /// Removes temporary assets created during a test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < tempAssetPaths.Count; i++)
            {
                AssetDatabase.DeleteAsset(tempAssetPaths[i]);
            }

            tempAssetPaths.Clear();
            AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.Refresh();
            VfxCatalogDiscovery.InvalidateCache();
        }

        /// <summary>
        /// Verifies catalog-scoped discovery records provenance and entry order.
        /// </summary>
        [Test]
        public void DiscoverCatalog_RecordsProvenanceAndOrder()
        {
            var prefab = CreatePrefabAsset("DiscoveryPrefab");
            var catalog = CreateCatalogAsset(
                "DiscoveryCatalog",
                new VfxCatalogEntry(new VfxId("Effects.WithPrefab"), prefab),
                new VfxCatalogEntry(new VfxId("Effects.NoPrefab"), null),
                new VfxCatalogEntry(new VfxId(string.Empty), null));

            var scope = VfxCatalogDiscovery.DiscoverCatalog(catalog);

            Assert.AreEqual(3, scope.Records.Count);
            Assert.AreEqual("Effects.WithPrefab", scope.Records[0].RawId);
            Assert.AreEqual(0, scope.Records[0].EntryIndex);
            Assert.AreSame(catalog, scope.Records[0].Catalog);
            Assert.AreEqual(AssetDatabase.GetAssetPath(catalog), scope.Records[0].CatalogAssetPath);
            Assert.AreEqual(prefab, scope.Records[0].Prefab);
            Assert.IsTrue(scope.Records[0].IsWellFormedId);

            Assert.AreEqual("Effects.NoPrefab", scope.Records[1].RawId);
            Assert.IsNull(scope.Records[1].Prefab);

            Assert.IsFalse(scope.Records[2].IsWellFormedId);
            Assert.AreEqual(1, scope.InvalidIds.Count);
            Assert.AreEqual(2, scope.DistinctIds.Count);
        }

        /// <summary>
        /// Verifies duplicate raw ids are reported as a conflict group with flags.
        /// </summary>
        [Test]
        public void DiscoverCatalog_DetectsDuplicateRawIds()
        {
            var catalog = CreateCatalogAsset(
                "DuplicateCatalog",
                new VfxCatalogEntry(new VfxId("Effects.Dup"), null),
                new VfxCatalogEntry(new VfxId("Effects.Dup"), null),
                new VfxCatalogEntry(new VfxId("Effects.Unique"), null));

            var scope = VfxCatalogDiscovery.DiscoverCatalog(catalog);

            Assert.AreEqual(1, scope.DuplicateRawIdGroups.Count);
            Assert.AreEqual(2, scope.DuplicateRawIdGroups[0].Count);
            Assert.IsTrue(scope.Records.Where(r => r.RawId == "Effects.Dup").All(r => r.HasDuplicateRawId));
            Assert.IsFalse(scope.Records.First(r => r.RawId == "Effects.Unique").HasDuplicateRawId);
        }

        /// <summary>
        /// Verifies different raw ids that sanitize to the same identifier are reported as collisions.
        /// </summary>
        [Test]
        public void DiscoverCatalog_DetectsSanitizedCollisions()
        {
            var catalog = CreateCatalogAsset(
                "CollisionCatalog",
                new VfxCatalogEntry(new VfxId("Fire Ball"), null),
                new VfxCatalogEntry(new VfxId("Fire-Ball"), null),
                new VfxCatalogEntry(new VfxId("Lightning"), null));

            var scope = VfxCatalogDiscovery.DiscoverCatalog(catalog);

            Assert.AreEqual(1, scope.SanitizedCollisionGroups.Count);
            Assert.AreEqual(2, scope.SanitizedCollisionGroups[0].Count);

            var fireBall = scope.Records.First(r => r.RawId == "Fire Ball");
            var fireDash = scope.Records.First(r => r.RawId == "Fire-Ball");
            Assert.IsTrue(fireBall.HasSanitizedCollision);
            Assert.IsTrue(fireDash.HasSanitizedCollision);
            Assert.AreEqual("Fire_Ball", fireBall.SanitizedIdentifier);
            Assert.AreEqual("Fire_Ball_2", fireDash.SanitizedIdentifier);
            Assert.IsFalse(scope.Records.First(r => r.RawId == "Lightning").HasSanitizedCollision);
        }

        /// <summary>
        /// Verifies project discovery includes both catalogs while catalog-scoped discovery is isolated.
        /// </summary>
        [Test]
        public void DiscoverProject_IncludesAll_WhileScopeIsIsolated()
        {
            var idA = new VfxId("DiscoveryTest.OnlyInA");
            var idB = new VfxId("DiscoveryTest.OnlyInB");

            var catalogA = CreateCatalogAsset("CatalogA", new VfxCatalogEntry(idA, null));
            var catalogB = CreateCatalogAsset("CatalogB", new VfxCatalogEntry(idB, null));

            VfxCatalogDiscovery.InvalidateCache();
            var project = VfxCatalogDiscovery.DiscoverProject();

            CollectionAssert.Contains(project.DistinctProjectIds.ToList(), idA);
            CollectionAssert.Contains(project.DistinctProjectIds.ToList(), idB);

            var scopeA = VfxCatalogDiscovery.DiscoverCatalog(catalogA);
            CollectionAssert.Contains(scopeA.DistinctIds.ToList(), idA);
            CollectionAssert.DoesNotContain(scopeA.DistinctIds.ToList(), idB);
        }

        /// <summary>
        /// Verifies project discovery ordering is deterministic across repeated calls.
        /// </summary>
        [Test]
        public void DiscoverProject_DeterministicOrdering()
        {
            CreateCatalogAsset("OrderCatalogA", new VfxCatalogEntry(new VfxId("Order.A"), null));
            CreateCatalogAsset("OrderCatalogB", new VfxCatalogEntry(new VfxId("Order.B"), null));

            VfxCatalogDiscovery.InvalidateCache();
            var first = VfxCatalogDiscovery.DiscoverProject().Records.Select(r => r.CatalogAssetPath + "#" + r.RawId).ToList();
            VfxCatalogDiscovery.InvalidateCache();
            var second = VfxCatalogDiscovery.DiscoverProject().Records.Select(r => r.CatalogAssetPath + "#" + r.RawId).ToList();

            CollectionAssert.AreEqual(first, second);
        }

        private VfxCatalog CreateCatalogAsset(string name, params VfxCatalogEntry[] entries)
        {
            EnsureTempFolder();
            var catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(entries);
            var path = $"{TempFolder}/{name}.asset";
            AssetDatabase.CreateAsset(catalog, path);
            AssetDatabase.SaveAssets();
            tempAssetPaths.Add(path);
            return catalog;
        }

        private GameObject CreatePrefabAsset(string name)
        {
            EnsureTempFolder();
            var source = new GameObject(name);
            source.AddComponent<PrefabVfxPlayable>();
            var path = $"{TempFolder}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            tempAssetPaths.Add(path);
            return prefab;
        }

        private static void EnsureTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                return;
            }

            var segments = TempFolder.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
