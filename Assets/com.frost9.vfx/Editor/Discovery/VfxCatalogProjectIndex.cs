using System.Collections.Generic;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Deterministic, project-wide index of every <see cref="VfxCatalog"/> entry.
    /// </summary>
    /// <remarks>
    /// These ids are <b>project-known</b> (authored somewhere in the project). They are not
    /// guaranteed playable by any particular <see cref="IVfxService"/>, because a service binds a
    /// single catalog. Use <see cref="VfxCatalogScopeIndex"/> when only one catalog's ids apply.
    /// </remarks>
    public sealed class VfxCatalogProjectIndex
    {
        /// <summary>
        /// Initializes a project index.
        /// </summary>
        public VfxCatalogProjectIndex(
            IReadOnlyList<VfxCatalogIdRecord> records,
            IReadOnlyList<VfxId> distinctProjectIds,
            IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> duplicateRawIdGroups,
            IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> sanitizedCollisionGroups,
            IReadOnlyList<VfxCatalogIdRecord> invalidIds)
        {
            Records = records ?? System.Array.Empty<VfxCatalogIdRecord>();
            DistinctProjectIds = distinctProjectIds ?? System.Array.Empty<VfxId>();
            DuplicateRawIdGroups = duplicateRawIdGroups ?? System.Array.Empty<IReadOnlyList<VfxCatalogIdRecord>>();
            SanitizedCollisionGroups = sanitizedCollisionGroups ?? System.Array.Empty<IReadOnlyList<VfxCatalogIdRecord>>();
            InvalidIds = invalidIds ?? System.Array.Empty<VfxCatalogIdRecord>();
        }

        /// <summary>
        /// Gets every discovered entry, ordered by catalog asset path then entry index.
        /// </summary>
        public IReadOnlyList<VfxCatalogIdRecord> Records { get; }

        /// <summary>
        /// Gets the distinct well-formed ids known across the project, ordered ordinal.
        /// </summary>
        public IReadOnlyList<VfxId> DistinctProjectIds { get; }

        /// <summary>
        /// Gets groups of records that share the same raw id (duplicate authoring conflicts).
        /// </summary>
        public IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> DuplicateRawIdGroups { get; }

        /// <summary>
        /// Gets groups of records whose ids collide to the same sanitized identifier.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> SanitizedCollisionGroups { get; }

        /// <summary>
        /// Gets records whose ids are empty or invalid.
        /// </summary>
        public IReadOnlyList<VfxCatalogIdRecord> InvalidIds { get; }

        /// <summary>
        /// Gets whether any duplicate, collision or invalid id was discovered.
        /// </summary>
        public bool HasConflicts =>
            DuplicateRawIdGroups.Count > 0 || SanitizedCollisionGroups.Count > 0 || InvalidIds.Count > 0;
    }
}
