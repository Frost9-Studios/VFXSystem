namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Outcome of an EnsurePlayable operation.
    /// </summary>
    public enum VfxEnsurePlayableOutcome
    {
        /// <summary>A runner was added and the prefab was saved.</summary>
        Changed,

        /// <summary>The prefab already had a valid runner; nothing changed.</summary>
        Unchanged,

        /// <summary>A non-fatal warning.</summary>
        Warning,

        /// <summary>The prefab type is unsupported or the operation failed; nothing changed.</summary>
        Error
    }

    /// <summary>
    /// Result of ensuring a prefab contains a valid <see cref="IVfxPlayable"/>.
    /// </summary>
    public readonly struct VfxEnsurePlayableResult
    {
        /// <summary>
        /// Initializes an EnsurePlayable result.
        /// </summary>
        public VfxEnsurePlayableResult(VfxEnsurePlayableOutcome outcome, string assetPath, string message)
        {
            Outcome = outcome;
            AssetPath = assetPath ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>Gets the operation outcome.</summary>
        public VfxEnsurePlayableOutcome Outcome { get; }

        /// <summary>Gets the affected asset path.</summary>
        public string AssetPath { get; }

        /// <summary>Gets a human-readable message.</summary>
        public string Message { get; }

        /// <summary>Gets whether the operation completed without error.</summary>
        public bool IsSuccess => Outcome != VfxEnsurePlayableOutcome.Error;
    }
}
