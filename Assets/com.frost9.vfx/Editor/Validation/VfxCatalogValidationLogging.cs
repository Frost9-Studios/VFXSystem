using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Logging helper for catalog validation output.
    /// </summary>
    public static class VfxCatalogValidationLogging
    {
        /// <summary>
        /// Validates a catalog and writes issues to the console.
        /// </summary>
        /// <param name="catalog">Catalog to validate.</param>
        /// <param name="source">Validation trigger source label.</param>
        /// <returns>Validation result.</returns>
        public static VfxCatalogValidationResult ValidateAndLog(VfxCatalog catalog, string source)
        {
            var result = VfxCatalogValidator.Validate(catalog);
            var path = catalog != null ? AssetDatabase.GetAssetPath(catalog) : "<null>";
            var sourceLabel = string.IsNullOrWhiteSpace(source) ? "Manual" : source;

            if (result.Issues.Count == 0)
            {
                Debug.Log($"[VfxCatalogValidator][{sourceLabel}] Valid catalog: {path}", catalog);
                return result;
            }

            for (var i = 0; i < result.Issues.Count; i++)
            {
                var issue = result.Issues[i];
                var prefix = $"[VfxCatalogValidator][{sourceLabel}] {issue.Code}";
                var entryLabel = issue.EntryIndex >= 0 ? $" Entry={issue.EntryIndex}" : string.Empty;
                var idLabel = string.IsNullOrWhiteSpace(issue.Id) ? string.Empty : $" Id='{issue.Id}'";
                var message = $"{prefix}:{entryLabel}{idLabel} {issue.Message}";

                if (issue.Severity == VfxCatalogValidationSeverity.Error)
                {
                    Debug.LogError(message, catalog);
                }
                else if (issue.Severity == VfxCatalogValidationSeverity.Warning)
                {
                    Debug.LogWarning(message, catalog);
                }
                else
                {
                    Debug.Log(message, catalog);
                }
            }

            Debug.Log(
                $"[VfxCatalogValidator][{sourceLabel}] {path} -> Errors={result.ErrorCount}, Warnings={result.WarningCount}",
                catalog);

            return result;
        }
    }
}
