using System.Collections.Generic;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Deterministic index of a single <see cref="VfxCatalog"/>'s entries.
    /// </summary>
    /// <remarks>
    /// Use this when ids must be scoped to the catalog a particular service actually registered,
    /// for example a project-side dropdown bound to its own gameplay catalog.
    /// </remarks>
    public sealed class VfxCatalogScopeIndex
    {
        /// <summary>
        /// Initializes a catalog-scoped index.
        /// </summary>
        public VfxCatalogScopeIndex(
            VfxCatalog catalog,
            string catalogAssetPath,
            IReadOnlyList<VfxCatalogIdRecord> records,
            IReadOnlyList<VfxId> distinctIds,
            IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> duplicateRawIdGroups,
            IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> sanitizedCollisionGroups,
            IReadOnlyList<VfxCatalogIdRecord> invalidIds)
        {
            Catalog = catalog;
            CatalogAssetPath = catalogAssetPath ?? string.Empty;
            Records = records ?? System.Array.Empty<VfxCatalogIdRecord>();
            DistinctIds = distinctIds ?? System.Array.Empty<VfxId>();
            DuplicateRawIdGroups = duplicateRawIdGroups ?? System.Array.Empty<IReadOnlyList<VfxCatalogIdRecord>>();
            SanitizedCollisionGroups = sanitizedCollisionGroups ?? System.Array.Empty<IReadOnlyList<VfxCatalogIdRecord>>();
            InvalidIds = invalidIds ?? System.Array.Empty<VfxCatalogIdRecord>();
        }

        /// <summary>
        /// Gets the catalog this index describes.
        /// </summary>
        public VfxCatalog Catalog { get; }

        /// <summary>
        /// Gets the catalog asset path (empty for in-memory catalogs).
        /// </summary>
        public string CatalogAssetPath { get; }

        /// <summary>
        /// Gets every entry in this catalog, ordered by entry index.
        /// </summary>
        public IReadOnlyList<VfxCatalogIdRecord> Records { get; }

        /// <summary>
        /// Gets the distinct well-formed ids in this catalog, ordered ordinal.
        /// </summary>
        public IReadOnlyList<VfxId> DistinctIds { get; }

        /// <summary>
        /// Gets groups of records that share the same raw id within this catalog.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> DuplicateRawIdGroups { get; }

        /// <summary>
        /// Gets groups of records whose ids collide to the same sanitized identifier within this catalog.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> SanitizedCollisionGroups { get; }

        /// <summary>
        /// Gets records whose ids are empty or invalid within this catalog.
        /// </summary>
        public IReadOnlyList<VfxCatalogIdRecord> InvalidIds { get; }

        /// <summary>
        /// Gets whether any duplicate, collision or invalid id exists in this catalog.
        /// </summary>
        public bool HasConflicts =>
            DuplicateRawIdGroups.Count > 0 || SanitizedCollisionGroups.Count > 0 || InvalidIds.Count > 0;
    }
}
