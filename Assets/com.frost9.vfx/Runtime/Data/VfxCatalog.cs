using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Scriptable catalog mapping <see cref="VfxId"/> values to spawnable effect entries.
    /// </summary>
    [CreateAssetMenu(fileName = "VfxCatalog", menuName = "Frost9/VFX/Catalog")]
    public class VfxCatalog : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Catalog entries keyed by string-based VfxId values.")]
        private List<VfxCatalogEntry> entries = new List<VfxCatalogEntry>();

        private readonly Dictionary<VfxId, VfxCatalogEntry> lookup = new Dictionary<VfxId, VfxCatalogEntry>();
        private bool lookupBuilt;

        /// <summary>
        /// Gets read-only catalog entries.
        /// </summary>
        public IReadOnlyList<VfxCatalogEntry> Entries => entries;

        /// <summary>
        /// Sets entries for runtime bootstrap or tests.
        /// </summary>
        /// <param name="newEntries">Entries to assign.</param>
        public void SetEntries(IEnumerable<VfxCatalogEntry> newEntries)
        {
            entries.Clear();
            if (newEntries != null)
            {
                entries.AddRange(newEntries);
            }

            lookupBuilt = false;
            BuildLookup();
        }

        /// <summary>
        /// Tries to resolve a catalog entry by id.
        /// </summary>
        /// <param name="id">Identifier to resolve.</param>
        /// <param name="entry">Resolved entry when found.</param>
        /// <returns>True when entry exists.</returns>
        public bool TryGetEntry(VfxId id, out VfxCatalogEntry entry)
        {
            BuildLookup();
            return lookup.TryGetValue(id, out entry);
        }

        /// <summary>
        /// Gets a catalog from Resources using default path.
        /// </summary>
        /// <param name="resourcesPath">Resources path without extension.</param>
        /// <returns>Catalog instance or null.</returns>
        public static VfxCatalog LoadFromResources(string resourcesPath = "VfxCatalog")
        {
            return Resources.Load<VfxCatalog>(resourcesPath);
        }

        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            lookupBuilt = false;
            BuildLookup();
        }

        private void BuildLookup()
        {
            if (lookupBuilt)
            {
                return;
            }

            lookup.Clear();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || !entry.Id.IsValid)
                {
                    continue;
                }

                if (lookup.ContainsKey(entry.Id))
                {
                    Debug.LogWarning($"[VfxCatalog] Duplicate id found: {entry.Id}");
                    continue;
                }

                lookup.Add(entry.Id, entry);
            }

            lookupBuilt = true;
        }
    }
}
