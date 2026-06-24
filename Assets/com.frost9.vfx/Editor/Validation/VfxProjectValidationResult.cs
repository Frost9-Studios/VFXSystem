using System.Collections.Generic;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Aggregate validation result across all project catalogs.
    /// </summary>
    public sealed class VfxProjectValidationResult
    {
        /// <summary>
        /// Initializes an aggregate validation result.
        /// </summary>
        /// <param name="reports">Per-catalog reports, deterministically ordered.</param>
        public VfxProjectValidationResult(IReadOnlyList<VfxCatalogValidationReport> reports)
        {
            Reports = reports ?? System.Array.Empty<VfxCatalogValidationReport>();

            var errors = 0;
            var warnings = 0;
            for (var i = 0; i < Reports.Count; i++)
            {
                var result = Reports[i].Result;
                if (result == null)
                {
                    continue;
                }

                errors += result.ErrorCount;
                warnings += result.WarningCount;
            }

            TotalErrors = errors;
            TotalWarnings = warnings;
            Summary = $"Catalogs={Reports.Count}, Errors={errors}, Warnings={warnings}.";
        }

        /// <summary>
        /// Gets per-catalog reports, ordered by asset path.
        /// </summary>
        public IReadOnlyList<VfxCatalogValidationReport> Reports { get; }

        /// <summary>
        /// Gets the number of catalogs validated.
        /// </summary>
        public int CatalogCount => Reports.Count;

        /// <summary>
        /// Gets the total error count across all catalogs.
        /// </summary>
        public int TotalErrors { get; }

        /// <summary>
        /// Gets the total warning count across all catalogs.
        /// </summary>
        public int TotalWarnings { get; }

        /// <summary>
        /// Gets whether any catalog reported an error.
        /// </summary>
        public bool HasErrors => TotalErrors > 0;

        /// <summary>
        /// Gets a concise human-readable summary.
        /// </summary>
        public string Summary { get; }
    }
}
