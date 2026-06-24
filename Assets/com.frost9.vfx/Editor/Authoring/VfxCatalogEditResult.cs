namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Outcome of a single catalog edit operation.
    /// </summary>
    public enum VfxCatalogEditOutcome
    {
        /// <summary>A new entry was added.</summary>
        Added,

        /// <summary>An existing entry's prefab was updated.</summary>
        Updated,

        /// <summary>No change was needed.</summary>
        Unchanged,

        /// <summary>An entry was removed.</summary>
        Removed,

        /// <summary>An entry exists that is not in the desired set (reported, not removed).</summary>
        Stale,

        /// <summary>Input conflicts with itself (for example a duplicate desired id).</summary>
        Conflict,

        /// <summary>A non-fatal warning.</summary>
        Warning,

        /// <summary>The operation failed and made no change.</summary>
        Error
    }

    /// <summary>
    /// Result of a single catalog edit operation.
    /// </summary>
    public readonly struct VfxCatalogEditResult
    {
        /// <summary>
        /// Initializes an edit result.
        /// </summary>
        /// <param name="outcome">Operation outcome.</param>
        /// <param name="id">Affected id.</param>
        /// <param name="message">Human-readable message.</param>
        public VfxCatalogEditResult(VfxCatalogEditOutcome outcome, VfxId id, string message)
        {
            Outcome = outcome;
            Id = id;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// Gets the operation outcome.
        /// </summary>
        public VfxCatalogEditOutcome Outcome { get; }

        /// <summary>
        /// Gets the affected id.
        /// </summary>
        public VfxId Id { get; }

        /// <summary>
        /// Gets a human-readable message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets whether the operation succeeded (did not error or conflict).
        /// </summary>
        public bool IsSuccess => Outcome != VfxCatalogEditOutcome.Error && Outcome != VfxCatalogEditOutcome.Conflict;
    }
}
