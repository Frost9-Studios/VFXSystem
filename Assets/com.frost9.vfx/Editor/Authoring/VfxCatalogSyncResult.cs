using System.Collections.Generic;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Structured result of a catalog synchronization.
    /// </summary>
    public sealed class VfxCatalogSyncResult
    {
        /// <summary>
        /// Initializes a synchronization result.
        /// </summary>
        public VfxCatalogSyncResult(
            bool succeeded,
            IReadOnlyList<VfxId> added,
            IReadOnlyList<VfxId> updated,
            IReadOnlyList<VfxId> unchanged,
            IReadOnlyList<VfxId> stale,
            IReadOnlyList<VfxId> removed,
            IReadOnlyList<string> conflicts,
            IReadOnlyList<string> errors)
        {
            Succeeded = succeeded;
            Added = added ?? System.Array.Empty<VfxId>();
            Updated = updated ?? System.Array.Empty<VfxId>();
            Unchanged = unchanged ?? System.Array.Empty<VfxId>();
            Stale = stale ?? System.Array.Empty<VfxId>();
            Removed = removed ?? System.Array.Empty<VfxId>();
            Conflicts = conflicts ?? System.Array.Empty<string>();
            Errors = errors ?? System.Array.Empty<string>();
        }

        /// <summary>
        /// Gets whether the synchronization applied successfully (no errors or conflicts).
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>Gets ids that were added.</summary>
        public IReadOnlyList<VfxId> Added { get; }

        /// <summary>Gets ids whose prefab was updated.</summary>
        public IReadOnlyList<VfxId> Updated { get; }

        /// <summary>Gets ids that were already up to date.</summary>
        public IReadOnlyList<VfxId> Unchanged { get; }

        /// <summary>Gets existing ids absent from the desired set (not removed unless requested).</summary>
        public IReadOnlyList<VfxId> Stale { get; }

        /// <summary>Gets ids that were removed.</summary>
        public IReadOnlyList<VfxId> Removed { get; }

        /// <summary>Gets input conflicts (for example duplicate desired ids).</summary>
        public IReadOnlyList<string> Conflicts { get; }

        /// <summary>Gets errors that prevented mutation.</summary>
        public IReadOnlyList<string> Errors { get; }

        /// <summary>
        /// Gets a concise human-readable summary.
        /// </summary>
        public string Summary => Succeeded
            ? $"Added={Added.Count}, Updated={Updated.Count}, Unchanged={Unchanged.Count}, " +
              $"Removed={Removed.Count}, Stale={Stale.Count}."
            : $"Sync failed with {Errors.Count} error(s) and {Conflicts.Count} conflict(s); no changes applied.";
    }
}
