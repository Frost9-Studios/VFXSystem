using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Discovers <see cref="VfxCatalog"/> ids across the project (or within a single catalog),
    /// retaining provenance and authoring conflicts. This is the shared source used by the package's
    /// own <see cref="VfxId"/> drawer and by project tooling.
    /// </summary>
    public static class VfxCatalogDiscovery
    {
        private static VfxCatalogProjectIndex cachedProjectIndex;

        /// <summary>
        /// Raised after the cached project index is invalidated so observers (drawers) can repaint.
        /// </summary>
        public static event System.Action CacheInvalidated;

        /// <summary>
        /// Discovers every catalog entry in the project, ordered deterministically.
        /// </summary>
        /// <returns>Cached project index (rebuilt after invalidation).</returns>
        public static VfxCatalogProjectIndex DiscoverProject()
        {
            return cachedProjectIndex ?? (cachedProjectIndex = BuildProjectIndex());
        }

        /// <summary>
        /// Discovers the entries of a single catalog, scoped to that catalog's own conflicts.
        /// </summary>
        /// <param name="catalog">Catalog to inspect.</param>
        /// <returns>Catalog-scoped index. Never cached (always reflects current entries).</returns>
        public static VfxCatalogScopeIndex DiscoverCatalog(VfxCatalog catalog)
        {
            if (catalog == null)
            {
                return new VfxCatalogScopeIndex(null, string.Empty, null, null, null, null, null);
            }

            var path = AssetDatabase.GetAssetPath(catalog);
            var data = BuildRecords(new List<CatalogSource> { new CatalogSource(catalog, path) });
            return new VfxCatalogScopeIndex(
                catalog,
                path,
                data.Records,
                data.DistinctIds,
                data.DuplicateGroups,
                data.CollisionGroups,
                data.Invalid);
        }

        /// <summary>
        /// Invalidates the cached project index. Editor mutation must call this after a successful
        /// apply so discovery reflects the change without a save/import or domain reload.
        /// </summary>
        public static void InvalidateCache()
        {
            cachedProjectIndex = null;
            CacheInvalidated?.Invoke();
        }

        [InitializeOnLoadMethod]
        private static void RegisterEditorHooks()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private static void OnUndoRedo()
        {
            // Undo/Redo bypasses the mutation API's post-apply invalidation, so refresh both the
            // discovery cache and the runtime catalog lookups here. Lookups are rebuilt lazily from
            // serialized entries, so this keeps Contains/TryGetEntry coherent without any reload.
            InvalidateCache();

            var loaded = Resources.FindObjectsOfTypeAll<VfxCatalog>();
            for (var i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null)
                {
                    loaded[i].InvalidateLookup();
                }
            }
        }

        private static VfxCatalogProjectIndex BuildProjectIndex()
        {
            var guids = AssetDatabase.FindAssets("t:VfxCatalog");
            var sources = new List<CatalogSource>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalog>(path);
                if (catalog != null)
                {
                    sources.Add(new CatalogSource(catalog, path));
                }
            }

            sources.Sort((a, b) =>
            {
                var byPath = string.CompareOrdinal(a.Path, b.Path);
                return byPath != 0 ? byPath : a.Catalog.GetInstanceID().CompareTo(b.Catalog.GetInstanceID());
            });

            var data = BuildRecords(sources);
            return new VfxCatalogProjectIndex(
                data.Records,
                data.DistinctIds,
                data.DuplicateGroups,
                data.CollisionGroups,
                data.Invalid);
        }

        private static BuildData BuildRecords(List<CatalogSource> sources)
        {
            var raws = new List<RawRecord>();
            for (var s = 0; s < sources.Count; s++)
            {
                var source = sources[s];
                var entries = source.Catalog.Entries;
                for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    var entry = entries[entryIndex];
                    var rawId = entry != null ? entry.Id.Value : string.Empty;
                    raws.Add(new RawRecord
                    {
                        RawId = rawId,
                        TrimmedId = (rawId ?? string.Empty).Trim(),
                        Catalog = source.Catalog,
                        Path = source.Path,
                        EntryIndex = entryIndex,
                        Prefab = entry != null ? entry.Prefab : null,
                        IsWellFormed = entry != null && entry.Id.IsValid
                    });
                }
            }

            var countByTrimmed = new Dictionary<string, int>(System.StringComparer.Ordinal);
            var wellFormedIds = new List<string>();
            for (var i = 0; i < raws.Count; i++)
            {
                if (!raws[i].IsWellFormed)
                {
                    continue;
                }

                wellFormedIds.Add(raws[i].TrimmedId);
                countByTrimmed.TryGetValue(raws[i].TrimmedId, out var count);
                countByTrimmed[raws[i].TrimmedId] = count + 1;
            }

            var analysis = VfxIdentifierAnalysis.Analyze(wellFormedIds);
            var nameByTrimmed = new Dictionary<string, string>(System.StringComparer.Ordinal);
            for (var i = 0; i < analysis.Identifiers.Count; i++)
            {
                nameByTrimmed[analysis.Identifiers[i].RawId] = analysis.Identifiers[i].GeneratedName;
            }

            var collisionMembers = new HashSet<string>(System.StringComparer.Ordinal);
            for (var i = 0; i < analysis.CollisionGroups.Count; i++)
            {
                var group = analysis.CollisionGroups[i];
                for (var j = 0; j < group.Count; j++)
                {
                    collisionMembers.Add(group[j]);
                }
            }

            var records = new List<VfxCatalogIdRecord>(raws.Count);
            for (var i = 0; i < raws.Count; i++)
            {
                var raw = raws[i];
                var sanitized = string.Empty;
                var collision = false;
                var duplicate = false;
                if (raw.IsWellFormed)
                {
                    nameByTrimmed.TryGetValue(raw.TrimmedId, out sanitized);
                    collision = collisionMembers.Contains(raw.TrimmedId);
                    duplicate = countByTrimmed.TryGetValue(raw.TrimmedId, out var dupCount) && dupCount > 1;
                }

                records.Add(new VfxCatalogIdRecord(
                    raw.RawId,
                    raw.Catalog,
                    raw.Path,
                    raw.EntryIndex,
                    raw.Prefab,
                    raw.IsWellFormed,
                    sanitized ?? string.Empty,
                    duplicate,
                    collision));
            }

            var distinctSet = new SortedSet<string>(System.StringComparer.Ordinal);
            for (var i = 0; i < raws.Count; i++)
            {
                if (raws[i].IsWellFormed)
                {
                    distinctSet.Add(raws[i].TrimmedId);
                }
            }

            var distinctIds = new List<VfxId>(distinctSet.Count);
            foreach (var id in distinctSet)
            {
                distinctIds.Add(new VfxId(id));
            }

            var duplicateGroups = BuildDuplicateGroups(records);
            var collisionGroups = BuildCollisionGroups(records, analysis);
            var invalid = new List<VfxCatalogIdRecord>();
            for (var i = 0; i < records.Count; i++)
            {
                if (!records[i].IsWellFormedId)
                {
                    invalid.Add(records[i]);
                }
            }

            return new BuildData
            {
                Records = records,
                DistinctIds = distinctIds,
                DuplicateGroups = duplicateGroups,
                CollisionGroups = collisionGroups,
                Invalid = invalid
            };
        }

        private static List<IReadOnlyList<VfxCatalogIdRecord>> BuildDuplicateGroups(List<VfxCatalogIdRecord> records)
        {
            var byTrimmed = new Dictionary<string, List<VfxCatalogIdRecord>>(System.StringComparer.Ordinal);
            var order = new List<string>();
            for (var i = 0; i < records.Count; i++)
            {
                if (!records[i].IsWellFormedId)
                {
                    continue;
                }

                var key = records[i].RawId.Trim();
                if (!byTrimmed.TryGetValue(key, out var list))
                {
                    list = new List<VfxCatalogIdRecord>();
                    byTrimmed.Add(key, list);
                    order.Add(key);
                }

                list.Add(records[i]);
            }

            order.Sort(System.StringComparer.Ordinal);
            var groups = new List<IReadOnlyList<VfxCatalogIdRecord>>();
            for (var i = 0; i < order.Count; i++)
            {
                var list = byTrimmed[order[i]];
                if (list.Count > 1)
                {
                    groups.Add(list);
                }
            }

            return groups;
        }

        private static List<IReadOnlyList<VfxCatalogIdRecord>> BuildCollisionGroups(
            List<VfxCatalogIdRecord> records,
            VfxIdentifierAnalysis analysis)
        {
            var groups = new List<IReadOnlyList<VfxCatalogIdRecord>>();
            for (var i = 0; i < analysis.CollisionGroups.Count; i++)
            {
                var idSet = new HashSet<string>(analysis.CollisionGroups[i], System.StringComparer.Ordinal);
                var groupRecords = new List<VfxCatalogIdRecord>();
                for (var r = 0; r < records.Count; r++)
                {
                    if (records[r].IsWellFormedId && idSet.Contains(records[r].RawId.Trim()))
                    {
                        groupRecords.Add(records[r]);
                    }
                }

                groups.Add(groupRecords);
            }

            return groups;
        }

        private readonly struct CatalogSource
        {
            public CatalogSource(VfxCatalog catalog, string path)
            {
                Catalog = catalog;
                Path = path ?? string.Empty;
            }

            public VfxCatalog Catalog { get; }

            public string Path { get; }
        }

        private sealed class RawRecord
        {
            public string RawId;
            public string TrimmedId;
            public VfxCatalog Catalog;
            public string Path;
            public int EntryIndex;
            public GameObject Prefab;
            public bool IsWellFormed;
        }

        private struct BuildData
        {
            public List<VfxCatalogIdRecord> Records;
            public List<VfxId> DistinctIds;
            public List<IReadOnlyList<VfxCatalogIdRecord>> DuplicateGroups;
            public List<IReadOnlyList<VfxCatalogIdRecord>> CollisionGroups;
            public List<VfxCatalogIdRecord> Invalid;
        }
    }

    /// <summary>
    /// Invalidates the discovery cache whenever project assets change.
    /// </summary>
    internal sealed class VfxCatalogDiscoveryAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            VfxCatalogDiscovery.InvalidateCache();
        }
    }
}
