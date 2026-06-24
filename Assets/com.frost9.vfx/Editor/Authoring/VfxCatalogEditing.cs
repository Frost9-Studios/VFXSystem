using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Sanctioned editor-only catalog mutation. All changes go through <c>SerializedObject</c> so
    /// tuned entry settings are preserved on prefab-only updates, Undo and dirty are handled, runtime
    /// lookups stay coherent, and a missing serialized layout fails with a structured error rather
    /// than corrupting data.
    /// </summary>
    public static class VfxCatalogEditing
    {
        /// <summary>
        /// Begins a batch of edits applied with a single <c>ApplyModifiedProperties</c> on dispose.
        /// </summary>
        /// <param name="catalog">Catalog to mutate.</param>
        /// <returns>Disposable batch, or null when the catalog is null.</returns>
        public static VfxCatalogEditBatch BeginBatch(VfxCatalog catalog)
        {
            return catalog == null ? null : new VfxCatalogEditBatch(catalog);
        }

        /// <summary>
        /// Adds a new entry or updates only the prefab of an existing entry (preserving tuned settings).
        /// </summary>
        public static VfxCatalogEditResult AddOrUpdate(VfxCatalog catalog, VfxId id, GameObject prefab)
        {
            if (catalog == null)
            {
                return Error(id, "Catalog is null.");
            }

            using (var batch = new VfxCatalogEditBatch(catalog))
            {
                if (!batch.LayoutValid)
                {
                    return Error(id, "Catalog serialized layout not found.");
                }

                return batch.AddOrUpdate(id, prefab);
            }
        }

        /// <summary>
        /// Removes an entry by id.
        /// </summary>
        public static VfxCatalogEditResult Remove(VfxCatalog catalog, VfxId id)
        {
            if (catalog == null)
            {
                return Error(id, "Catalog is null.");
            }

            using (var batch = new VfxCatalogEditBatch(catalog))
            {
                if (!batch.LayoutValid)
                {
                    return Error(id, "Catalog serialized layout not found.");
                }

                return batch.Remove(id);
            }
        }

        /// <summary>
        /// Synchronizes the catalog toward a desired set of (id, prefab) pairs. Stale entries are
        /// reported but only removed when <paramref name="removeMissing"/> is true. The whole desired
        /// set is preflighted before any mutation; on conflict or error no change is applied.
        /// </summary>
        public static VfxCatalogSyncResult Sync(
            VfxCatalog catalog,
            IReadOnlyCollection<(VfxId id, GameObject prefab)> desired,
            bool removeMissing = false)
        {
            var conflicts = new List<string>();
            var errors = new List<string>();

            if (catalog == null)
            {
                errors.Add("Catalog is null.");
                return Failure(conflicts, errors);
            }

            // Preflight: validate the whole desired set before touching the SerializedObject or Undo.
            var validated = new List<(VfxId id, GameObject prefab)>();
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            if (desired != null)
            {
                foreach (var pair in desired)
                {
                    if (!pair.id.IsValid)
                    {
                        errors.Add("Desired set contains an empty or invalid id.");
                        continue;
                    }

                    if (!seen.Add(pair.id.Value))
                    {
                        conflicts.Add($"Duplicate desired id '{pair.id.Value}'.");
                        continue;
                    }

                    if (!IsValidPrefabAsset(pair.prefab))
                    {
                        errors.Add($"Desired id '{pair.id.Value}' has a null or non-prefab-asset reference.");
                        continue;
                    }

                    validated.Add(pair);
                }
            }

            if (conflicts.Count > 0 || errors.Count > 0)
            {
                return Failure(conflicts, errors);
            }

            var added = new List<VfxId>();
            var updated = new List<VfxId>();
            var unchanged = new List<VfxId>();
            var stale = new List<VfxId>();
            var removed = new List<VfxId>();

            using (var batch = new VfxCatalogEditBatch(catalog))
            {
                if (!batch.LayoutValid)
                {
                    errors.Add("Catalog serialized layout not found.");
                    batch.Abort();
                    return Failure(conflicts, errors);
                }

                var desiredIds = new HashSet<string>(System.StringComparer.Ordinal);
                for (var i = 0; i < validated.Count; i++)
                {
                    desiredIds.Add(validated[i].id.Value);
                    var result = batch.AddOrUpdate(validated[i].id, validated[i].prefab);
                    switch (result.Outcome)
                    {
                        case VfxCatalogEditOutcome.Added:
                            added.Add(result.Id);
                            break;
                        case VfxCatalogEditOutcome.Updated:
                            updated.Add(result.Id);
                            break;
                        case VfxCatalogEditOutcome.Unchanged:
                            unchanged.Add(result.Id);
                            break;
                        default:
                            errors.Add(result.Message);
                            batch.Abort();
                            return Failure(conflicts, errors);
                    }
                }

                var existingIds = SnapshotExistingIds(catalog);
                for (var i = 0; i < existingIds.Count; i++)
                {
                    var existing = existingIds[i];
                    if (desiredIds.Contains(existing.Value))
                    {
                        continue;
                    }

                    if (!removeMissing)
                    {
                        stale.Add(existing);
                        continue;
                    }

                    var removeResult = batch.Remove(existing);
                    if (removeResult.Outcome == VfxCatalogEditOutcome.Removed)
                    {
                        removed.Add(existing);
                    }
                    else if (!removeResult.IsSuccess)
                    {
                        errors.Add(removeResult.Message);
                        batch.Abort();
                        return Failure(conflicts, errors);
                    }
                }

                // Applied on dispose.
            }

            return new VfxCatalogSyncResult(true, added, updated, unchanged, stale, removed, conflicts, errors);
        }

        internal static bool IsValidPrefabAsset(GameObject prefab)
        {
            return prefab != null && PrefabUtility.IsPartOfPrefabAsset(prefab);
        }

        private static List<VfxId> SnapshotExistingIds(VfxCatalog catalog)
        {
            var ids = new List<VfxId>();
            var seen = new HashSet<string>(System.StringComparer.Ordinal);
            var entries = catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.Id.IsValid && seen.Add(entry.Id.Value))
                {
                    ids.Add(entry.Id);
                }
            }

            return ids;
        }

        private static VfxCatalogEditResult Error(VfxId id, string message)
        {
            return new VfxCatalogEditResult(VfxCatalogEditOutcome.Error, id, message);
        }

        private static VfxCatalogSyncResult Failure(List<string> conflicts, List<string> errors)
        {
            return new VfxCatalogSyncResult(false, null, null, null, null, null, conflicts, errors);
        }
    }
}
