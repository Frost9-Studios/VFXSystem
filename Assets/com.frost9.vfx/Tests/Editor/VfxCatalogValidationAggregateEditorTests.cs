using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Frost9.VFX.Editor;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Editor tests for programmatic project-wide validation and provenance.
    /// </summary>
    public class VfxCatalogValidationAggregateEditorTests
    {
        private const string TempFolder = "Assets/_Project/Temp/VfxValidationAggregateTests";
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
        }

        /// <summary>
        /// Verifies the aggregate reports each catalog with provenance and a result matching the
        /// direct single-catalog validator (the menu wrapper logs these same results).
        /// </summary>
        [Test]
        public void ValidateAllProjectCatalogs_ReturnsPerCatalogProvenance_MatchingDirectValidate()
        {
            var validPrefab = CreatePrefabAsset("AggValidPrefab");
            var validCatalog = CreateCatalogAsset(
                "AggValidCatalog",
                new VfxCatalogEntry(new VfxId("Effects.Valid"), validPrefab));
            var brokenCatalog = CreateCatalogAsset(
                "AggBrokenCatalog",
                new VfxCatalogEntry(new VfxId("Effects.Dup"), validPrefab),
                new VfxCatalogEntry(new VfxId("Effects.Dup"), validPrefab),
                new VfxCatalogEntry(new VfxId("Effects.NoPrefab"), null));

            var aggregate = VfxCatalogValidation.ValidateAllProjectCatalogs();

            var validReport = aggregate.Reports.FirstOrDefault(r => r.Catalog == validCatalog);
            var brokenReport = aggregate.Reports.FirstOrDefault(r => r.Catalog == brokenCatalog);

            Assert.IsNotNull(validReport, "Valid catalog missing from aggregate.");
            Assert.IsNotNull(brokenReport, "Broken catalog missing from aggregate.");

            Assert.AreEqual(AssetDatabase.GetAssetPath(validCatalog), validReport.AssetPath);
            Assert.AreEqual(AssetDatabase.GetAssetPath(brokenCatalog), brokenReport.AssetPath);

            Assert.AreEqual(0, validReport.Result.ErrorCount);
            Assert.GreaterOrEqual(brokenReport.Result.ErrorCount, 2, "Expected duplicate id + missing prefab errors.");

            // Programmatic single-catalog validation matches the aggregate's per-catalog result.
            var direct = VfxCatalogValidator.Validate(brokenCatalog);
            Assert.AreEqual(direct.ErrorCount, brokenReport.Result.ErrorCount);
            Assert.AreEqual(direct.WarningCount, brokenReport.Result.WarningCount);
        }

        /// <summary>
        /// Verifies aggregate ordering is deterministic across repeated calls.
        /// </summary>
        [Test]
        public void ValidateAllProjectCatalogs_DeterministicOrdering()
        {
            CreateCatalogAsset("AggOrderA", new VfxCatalogEntry(new VfxId("Order.A"), null));
            CreateCatalogAsset("AggOrderB", new VfxCatalogEntry(new VfxId("Order.B"), null));

            var first = VfxCatalogValidation.ValidateAllProjectCatalogs().Reports.Select(r => r.AssetPath).ToList();
            var second = VfxCatalogValidation.ValidateAllProjectCatalogs().Reports.Select(r => r.AssetPath).ToList();

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
