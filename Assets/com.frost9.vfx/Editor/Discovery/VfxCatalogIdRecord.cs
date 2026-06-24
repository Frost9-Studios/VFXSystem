using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// A single catalog entry discovered in the project, retaining provenance and conflict state.
    /// </summary>
    public readonly struct VfxCatalogIdRecord
    {
        /// <summary>
        /// Initializes a discovery record.
        /// </summary>
        /// <param name="rawId">Raw VFX id string (as authored).</param>
        /// <param name="catalog">Owning catalog asset.</param>
        /// <param name="catalogAssetPath">Owning catalog asset path.</param>
        /// <param name="entryIndex">Zero-based entry index within the catalog.</param>
        /// <param name="prefab">Entry prefab reference, if any.</param>
        /// <param name="isWellFormedId">Whether the id is non-empty/valid.</param>
        /// <param name="sanitizedIdentifier">Generated C# identifier (disambiguated), or empty.</param>
        /// <param name="hasDuplicateRawId">Whether this raw id appears in more than one record in scope.</param>
        /// <param name="hasSanitizedCollision">Whether this id collides with another after sanitization.</param>
        public VfxCatalogIdRecord(
            string rawId,
            VfxCatalog catalog,
            string catalogAssetPath,
            int entryIndex,
            GameObject prefab,
            bool isWellFormedId,
            string sanitizedIdentifier,
            bool hasDuplicateRawId,
            bool hasSanitizedCollision)
        {
            RawId = rawId ?? string.Empty;
            Catalog = catalog;
            CatalogAssetPath = catalogAssetPath ?? string.Empty;
            EntryIndex = entryIndex;
            Prefab = prefab;
            IsWellFormedId = isWellFormedId;
            SanitizedIdentifier = sanitizedIdentifier ?? string.Empty;
            HasDuplicateRawId = hasDuplicateRawId;
            HasSanitizedCollision = hasSanitizedCollision;
        }

        /// <summary>
        /// Gets the raw VFX id string.
        /// </summary>
        public string RawId { get; }

        /// <summary>
        /// Gets the owning catalog asset.
        /// </summary>
        public VfxCatalog Catalog { get; }

        /// <summary>
        /// Gets the owning catalog asset path.
        /// </summary>
        public string CatalogAssetPath { get; }

        /// <summary>
        /// Gets the zero-based entry index within the catalog.
        /// </summary>
        public int EntryIndex { get; }

        /// <summary>
        /// Gets the entry prefab reference, if any.
        /// </summary>
        public GameObject Prefab { get; }

        /// <summary>
        /// Gets whether the id is non-empty and valid.
        /// </summary>
        public bool IsWellFormedId { get; }

        /// <summary>
        /// Gets the generated C# identifier (disambiguated) for this id, or empty when not well-formed.
        /// </summary>
        public string SanitizedIdentifier { get; }

        /// <summary>
        /// Gets whether this raw id appears in more than one record in the discovery scope.
        /// </summary>
        public bool HasDuplicateRawId { get; }

        /// <summary>
        /// Gets whether this id collides with another id after sanitization.
        /// </summary>
        public bool HasSanitizedCollision { get; }

        /// <summary>
        /// Gets the id as a <see cref="VfxId"/>.
        /// </summary>
        public VfxId Id => new VfxId(RawId);
    }
}
