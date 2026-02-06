using System.Collections.Generic;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Immutable summary of catalog validation output.
    /// </summary>
    public sealed class VfxCatalogValidationResult
    {
        /// <summary>
        /// Initializes a validation result.
        /// </summary>
        /// <param name="issues">Issues discovered during validation.</param>
        public VfxCatalogValidationResult(IReadOnlyList<VfxCatalogValidationIssue> issues)
        {
            Issues = issues ?? new List<VfxCatalogValidationIssue>();

            var errorCount = 0;
            var warningCount = 0;
            for (var i = 0; i < Issues.Count; i++)
            {
                var severity = Issues[i].Severity;
                if (severity == VfxCatalogValidationSeverity.Error)
                {
                    errorCount++;
                }
                else if (severity == VfxCatalogValidationSeverity.Warning)
                {
                    warningCount++;
                }
            }

            ErrorCount = errorCount;
            WarningCount = warningCount;
        }

        /// <summary>
        /// Gets discovered issues.
        /// </summary>
        public IReadOnlyList<VfxCatalogValidationIssue> Issues { get; }

        /// <summary>
        /// Gets error count.
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Gets warning count.
        /// </summary>
        public int WarningCount { get; }

        /// <summary>
        /// Gets whether at least one error was reported.
        /// </summary>
        public bool HasErrors => ErrorCount > 0;
    }
}
