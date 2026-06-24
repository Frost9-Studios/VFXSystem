using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Frost9.VFX.Editor;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Editor tests for the SerializedObject-based catalog mutation API.
    /// </summary>
    public class VfxCatalogEditingEditorTests
    {
        private const string TempFolder = "Assets/_Project/Temp/VfxCatalogEditingTests";
        private readonly List<string> tempAssetPaths = new List<string>();
        private readonly List<Object> tempObjects = new List<Object>();

        private static readonly VfxId IdA = new VfxId("Effects.A");
        private static readonly VfxId IdB = new VfxId("Effects.B");

        /// <summary>
        /// Cleans up temporary assets and objects.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            for (var i = tempObjects.Count - 1; i >= 0; i--)
            {
                if (tempObjects[i] != null)
                {
                    Object.DestroyImmediate(tempObjects[i]);
                }
            }

            tempObjects.Clear();

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
        /// Verifies a newly added entry receives the documented defaults even when the list already
        /// contains a tuned entry (no array-element copy contamination).
        /// </summary>
        [Test]
        public void AddOrUpdate_AddsNewEntry_WithDocumentedDefaults()
        {
            var prefab = CreatePrefabAsset("Defaults");
            var tuned = BuildTunedEntry(IdA, prefab, 9, 17, false, VfxChannel.UI, false, 3.5f);
            var catalog = CreateInMemoryCatalog(tuned);

            var result = VfxCatalogEditing.AddOrUpdate(catalog, IdB, prefab);

            Assert.AreEqual(VfxCatalogEditOutcome.Added, result.Outcome);
            Assert.IsTrue(catalog.TryGetEntry(IdB, out var added));

            var reference = new VfxCatalogEntry(IdB, prefab);
            Assert.AreEqual(reference.InitialPoolSize, added.InitialPoolSize);
            Assert.AreEqual(reference.MaxPoolSize, added.MaxPoolSize);
            Assert.AreEqual(reference.AllowPoolExpansion, added.AllowPoolExpansion);
            Assert.AreEqual(reference.DefaultChannel, added.DefaultChannel);
            Assert.AreEqual(reference.AutoReleaseByDefault, added.AutoReleaseByDefault);
            Assert.AreEqual(reference.FallbackLifetimeSeconds, added.FallbackLifetimeSeconds);
            Assert.IsFalse(added.DefaultParameters.HasScale, "Parameters should not be copied from the tuned entry.");
        }

        /// <summary>
        /// Verifies adding the same id+prefab again reports Unchanged (idempotent).
        /// </summary>
        [Test]
        public void AddOrUpdate_SecondRun_ReturnsUnchanged()
        {
            var prefab = CreatePrefabAsset("Idempotent");
            var catalog = CreateInMemoryCatalog();

            Assert.AreEqual(VfxCatalogEditOutcome.Added, VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab).Outcome);
            Assert.AreEqual(VfxCatalogEditOutcome.Unchanged, VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab).Outcome);
            Assert.AreEqual(1, catalog.Count);
        }

        /// <summary>
        /// Verifies updating only the prefab preserves every tuned setting.
        /// </summary>
        [Test]
        public void AddOrUpdate_PrefabOnly_PreservesAllTunedSettings()
        {
            var prefab1 = CreatePrefabAsset("Tuned1");
            var prefab2 = CreatePrefabAsset("Tuned2");
            var tuned = BuildTunedEntry(IdA, prefab1, 9, 17, false, VfxChannel.Ambient, false, 4.25f, VfxParams.Empty.WithScale(2f));
            var catalog = CreateInMemoryCatalog(tuned);

            var result = VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab2);

            Assert.AreEqual(VfxCatalogEditOutcome.Updated, result.Outcome);
            Assert.IsTrue(catalog.TryGetEntry(IdA, out var updated));
            Assert.AreEqual(prefab2, updated.Prefab);
            Assert.AreEqual(9, updated.InitialPoolSize);
            Assert.AreEqual(17, updated.MaxPoolSize);
            Assert.IsFalse(updated.AllowPoolExpansion);
            Assert.AreEqual(VfxChannel.Ambient, updated.DefaultChannel);
            Assert.IsFalse(updated.AutoReleaseByDefault);
            Assert.AreEqual(4.25f, updated.FallbackLifetimeSeconds);
            Assert.IsTrue(updated.DefaultParameters.HasScale);
            Assert.AreEqual(2f, updated.DefaultParameters.Scale);
        }

        /// <summary>
        /// Verifies removal and that removing a missing id is a no-op.
        /// </summary>
        [Test]
        public void Remove_RemovesEntry_AndMissingIsUnchanged()
        {
            var prefab = CreatePrefabAsset("Remove");
            var catalog = CreateInMemoryCatalog();
            VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab);

            Assert.AreEqual(VfxCatalogEditOutcome.Removed, VfxCatalogEditing.Remove(catalog, IdA).Outcome);
            Assert.IsFalse(catalog.Contains(IdA));
            Assert.AreEqual(VfxCatalogEditOutcome.Unchanged, VfxCatalogEditing.Remove(catalog, IdA).Outcome);
        }

        /// <summary>
        /// Verifies a batch applies multiple changes and runtime lookups immediately reflect them.
        /// </summary>
        [Test]
        public void Batch_AppliesMultipleChanges_AndLookupIsCoherent()
        {
            var prefab = CreatePrefabAsset("Batch");
            var catalog = CreateInMemoryCatalog();

            using (var batch = VfxCatalogEditing.BeginBatch(catalog))
            {
                batch.AddOrUpdate(IdA, prefab);
                batch.AddOrUpdate(IdB, prefab);
            }

            Assert.IsTrue(catalog.Contains(IdA));
            Assert.IsTrue(catalog.Contains(IdB));
            Assert.AreEqual(2, catalog.Count);
        }

        /// <summary>
        /// Verifies runtime lookups reflect add/update/remove immediately with no reload.
        /// </summary>
        [Test]
        public void LookupCoherence_AfterAddUpdateRemove_NoReload()
        {
            var prefab1 = CreatePrefabAsset("Coherence1");
            var prefab2 = CreatePrefabAsset("Coherence2");
            var catalog = CreateInMemoryCatalog();

            Assert.IsFalse(catalog.Contains(IdA));

            VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab1);
            Assert.IsTrue(catalog.Contains(IdA));
            Assert.IsTrue(catalog.TryGetEntry(IdA, out var afterAdd));
            Assert.AreEqual(prefab1, afterAdd.Prefab);

            VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab2);
            Assert.IsTrue(catalog.TryGetEntry(IdA, out var afterUpdate));
            Assert.AreEqual(prefab2, afterUpdate.Prefab);

            VfxCatalogEditing.Remove(catalog, IdA);
            Assert.IsFalse(catalog.Contains(IdA));
        }

        /// <summary>
        /// Verifies project discovery reflects a mutation without any SaveAssets/Refresh/reload.
        /// </summary>
        [Test]
        public void DiscoveryCache_InvalidatedAfterMutation()
        {
            var prefab = CreatePrefabAsset("DiscPrefab");
            var catalog = CreateOnDiskCatalog("DiscCacheCatalog");

            VfxCatalogDiscovery.InvalidateCache();
            var idUnique = new VfxId("DiscCache.OnlyAfterMutation");
            Assert.IsFalse(VfxCatalogDiscovery.DiscoverProject().DistinctProjectIds.Contains(idUnique));

            VfxCatalogEditing.AddOrUpdate(catalog, idUnique, prefab);

            Assert.IsTrue(
                VfxCatalogDiscovery.DiscoverProject().DistinctProjectIds.Contains(idUnique),
                "Discovery cache was not invalidated after mutation.");
        }

        /// <summary>
        /// Verifies Sync with removeMissing=false reports stale entries without deleting them.
        /// </summary>
        [Test]
        public void Sync_RemoveMissingFalse_ReportsStale_DoesNotDelete()
        {
            var prefab = CreatePrefabAsset("SyncKeep");
            var catalog = CreateInMemoryCatalog();
            VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab);

            var result = VfxCatalogEditing.Sync(
                catalog,
                new[] { (IdB, prefab) },
                removeMissing: false);

            Assert.IsTrue(result.Succeeded);
            Assert.Contains(IdB, result.Added.ToList());
            Assert.Contains(IdA, result.Stale.ToList());
            Assert.IsTrue(catalog.Contains(IdA), "Stale entry must not be deleted when removeMissing is false.");
            Assert.IsTrue(catalog.Contains(IdB));
        }

        /// <summary>
        /// Verifies Sync with removeMissing=true deletes stale entries.
        /// </summary>
        [Test]
        public void Sync_RemoveMissingTrue_DeletesStale()
        {
            var prefab = CreatePrefabAsset("SyncPrune");
            var catalog = CreateInMemoryCatalog();
            VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab);

            var result = VfxCatalogEditing.Sync(
                catalog,
                new[] { (IdB, prefab) },
                removeMissing: true);

            Assert.IsTrue(result.Succeeded);
            Assert.Contains(IdA, result.Removed.ToList());
            Assert.IsFalse(catalog.Contains(IdA));
            Assert.IsTrue(catalog.Contains(IdB));
        }

        /// <summary>
        /// Verifies Sync rejects duplicate desired ids and applies no changes.
        /// </summary>
        [Test]
        public void Sync_RejectsDuplicateDesiredIds_NoChanges()
        {
            var prefab = CreatePrefabAsset("SyncDup");
            var catalog = CreateInMemoryCatalog();

            var result = VfxCatalogEditing.Sync(
                catalog,
                new[] { (IdA, prefab), (IdA, prefab) },
                removeMissing: false);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotEmpty(result.Conflicts);
            Assert.AreEqual(0, catalog.Count, "No changes should be applied on a conflicting desired set.");
        }

        /// <summary>
        /// Verifies Sync rejects null/non-prefab-asset references and applies no changes.
        /// </summary>
        [Test]
        public void Sync_RejectsNullPrefab_NoChanges()
        {
            var catalog = CreateInMemoryCatalog();

            var result = VfxCatalogEditing.Sync(
                catalog,
                new[] { (IdA, (GameObject)null) },
                removeMissing: false);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNotEmpty(result.Errors);
            Assert.AreEqual(0, catalog.Count);
        }

        /// <summary>
        /// Verifies Undo reverts a mutation and runtime lookups reflect it immediately.
        /// </summary>
        [Test]
        public void Undo_RevertsMutation_AndLookupReflectsIt()
        {
            var prefab = CreatePrefabAsset("UndoPrefab");
            var catalog = CreateOnDiskCatalog("UndoCatalog");

            VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab);
            Assert.IsTrue(catalog.Contains(IdA));

            Undo.PerformUndo();

            Assert.IsFalse(catalog.Contains(IdA), "Undo should revert the add and lookup should reflect it.");
        }

        /// <summary>
        /// Verifies a mutation persists when the catalog asset is saved and reloaded.
        /// </summary>
        [Test]
        public void Persistence_Mutation_SurvivesReload()
        {
            var prefab = CreatePrefabAsset("PersistPrefab");
            var path = $"{TempFolder}/PersistCatalog.asset";
            var catalog = CreateOnDiskCatalog("PersistCatalog");

            VfxCatalogEditing.AddOrUpdate(catalog, IdA, prefab);
            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(catalog);

            var reloaded = AssetDatabase.LoadAssetAtPath<VfxCatalog>(path);
            Assert.IsNotNull(reloaded);
            Assert.IsTrue(reloaded.Contains(IdA), "Mutation did not persist across reload.");
        }

        private VfxCatalog CreateInMemoryCatalog(params VfxCatalogEntry[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(entries);
            tempObjects.Add(catalog);
            return catalog;
        }

        private VfxCatalog CreateOnDiskCatalog(string name)
        {
            EnsureTempFolder();
            var catalog = ScriptableObject.CreateInstance<VfxCatalog>();
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

        private static VfxCatalogEntry BuildTunedEntry(
            VfxId id,
            GameObject prefab,
            int initial,
            int max,
            bool expansion,
            VfxChannel channel,
            bool autoRelease,
            float lifetime,
            VfxParams? parameters = null)
        {
            var entry = new VfxCatalogEntry(id, prefab);
            SetPrivateField(entry, "initialPoolSize", initial);
            SetPrivateField(entry, "maxPoolSize", max);
            SetPrivateField(entry, "allowPoolExpansion", expansion);
            SetPrivateField(entry, "defaultChannel", channel);
            SetPrivateField(entry, "autoReleaseByDefault", autoRelease);
            SetPrivateField(entry, "fallbackLifetimeSeconds", lifetime);
            if (parameters.HasValue)
            {
                SetPrivateField(entry, "defaultParameters", parameters.Value);
            }

            return entry;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
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
