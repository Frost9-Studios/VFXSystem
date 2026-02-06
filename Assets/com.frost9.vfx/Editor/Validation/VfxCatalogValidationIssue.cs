namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Single validation issue discovered while checking a VFX catalog.
    /// </summary>
    public readonly struct VfxCatalogValidationIssue
    {
        /// <summary>
        /// Initializes a validation issue.
        /// </summary>
        /// <param name="severity">Issue severity.</param>
        /// <param name="code">Stable machine-readable code.</param>
        /// <param name="message">Human-readable message.</param>
        /// <param name="entryIndex">Catalog entry index, or -1 when not entry-specific.</param>
        /// <param name="id">Optional VFX id context.</param>
        public VfxCatalogValidationIssue(
            VfxCatalogValidationSeverity severity,
            string code,
            string message,
            int entryIndex = -1,
            string id = null)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            EntryIndex = entryIndex;
            Id = id ?? string.Empty;
        }

        /// <summary>
        /// Gets issue severity.
        /// </summary>
        public VfxCatalogValidationSeverity Severity { get; }

        /// <summary>
        /// Gets stable machine-readable issue code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Gets human-readable issue message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets zero-based entry index, or -1.
        /// </summary>
        public int EntryIndex { get; }

        /// <summary>
        /// Gets optional VFX id context.
        /// </summary>
        public string Id { get; }
    }
}
