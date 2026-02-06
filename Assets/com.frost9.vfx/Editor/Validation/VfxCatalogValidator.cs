using System.Collections.Generic;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Validates <see cref="VfxCatalog"/> assets and reports actionable authoring issues.
    /// </summary>
    public static class VfxCatalogValidator
    {
        /// <summary>
        /// Validates a catalog instance and returns structured issues.
        /// </summary>
        /// <param name="catalog">Catalog to validate.</param>
        /// <returns>Validation result with errors and warnings.</returns>
        public static VfxCatalogValidationResult Validate(VfxCatalog catalog)
        {
            var issues = new List<VfxCatalogValidationIssue>();

            if (catalog == null)
            {
                issues.Add(new VfxCatalogValidationIssue(
                    VfxCatalogValidationSeverity.Error,
                    "catalog.null",
                    "Catalog reference is null. Assign a valid VfxCatalog asset."));
                return new VfxCatalogValidationResult(issues);
            }

            var idToFirstIndex = new Dictionary<string, int>(System.StringComparer.Ordinal);
            var entries = catalog.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    issues.Add(new VfxCatalogValidationIssue(
                        VfxCatalogValidationSeverity.Error,
                        "entry.null",
                        "Catalog entry is null. Remove the empty slot or assign a valid entry.",
                        i));
                    continue;
                }

                var id = entry.Id.Value;
                if (string.IsNullOrWhiteSpace(id))
                {
                    issues.Add(new VfxCatalogValidationIssue(
                        VfxCatalogValidationSeverity.Error,
                        "entry.id.missing",
                        "Entry id is missing. Assign a stable VfxId string.",
                        i));
                }
                else if (idToFirstIndex.TryGetValue(id, out var firstIndex))
                {
                    issues.Add(new VfxCatalogValidationIssue(
                        VfxCatalogValidationSeverity.Error,
                        "entry.id.duplicate",
                        $"Duplicate id '{id}' at entry {i}. First occurrence is entry {firstIndex}. Use unique ids.",
                        i,
                        id));
                }
                else
                {
                    idToFirstIndex.Add(id, i);
                }

                if (entry.Prefab == null)
                {
                    issues.Add(new VfxCatalogValidationIssue(
                        VfxCatalogValidationSeverity.Error,
                        "entry.prefab.missing",
                        $"Entry '{id}' has no prefab. Assign a prefab with an IVfxPlayable component.",
                        i,
                        id));
                }
                else
                {
                    var hasPlayable = entry.Prefab.GetComponent<IVfxPlayable>() != null ||
                                      entry.Prefab.GetComponentInChildren<IVfxPlayable>(true) != null;
                    if (!hasPlayable)
                    {
                        issues.Add(new VfxCatalogValidationIssue(
                            VfxCatalogValidationSeverity.Warning,
                            "entry.prefab.playable_missing",
                            $"Entry '{id}' prefab '{entry.Prefab.name}' has no IVfxPlayable component. Add one to the prefab (self or child).",
                            i,
                            id));
                    }
                }

                if (entry.InitialPoolSize < 0)
                {
                    issues.Add(new VfxCatalogValidationIssue(
                        VfxCatalogValidationSeverity.Error,
                        "entry.pool.initial_negative",
                        $"Entry '{id}' has InitialPoolSize < 0. Use a value >= 0.",
                        i,
                        id));
                }

                if (entry.MaxPoolSize < 1)
                {
                    issues.Add(new VfxCatalogValidationIssue(
                        VfxCatalogValidationSeverity.Error,
                        "entry.pool.max_too_small",
                        $"Entry '{id}' has MaxPoolSize < 1. Use a value >= 1.",
                        i,
                        id));
                }
                else if (entry.MaxPoolSize < entry.InitialPoolSize)
                {
                    issues.Add(new VfxCatalogValidationIssue(
                        VfxCatalogValidationSeverity.Error,
                        "entry.pool.max_less_than_initial",
                        $"Entry '{id}' has MaxPoolSize ({entry.MaxPoolSize}) less than InitialPoolSize ({entry.InitialPoolSize}).",
                        i,
                        id));
                }

                if (entry.FallbackLifetimeSeconds < 0f)
                {
                    issues.Add(new VfxCatalogValidationIssue(
                        VfxCatalogValidationSeverity.Error,
                        "entry.lifetime.negative",
                        $"Entry '{id}' has negative fallback lifetime. Use a value >= 0.",
                        i,
                        id));
                }
            }

            return new VfxCatalogValidationResult(issues);
        }
    }
}
