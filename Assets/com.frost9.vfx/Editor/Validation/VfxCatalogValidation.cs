using System.Collections.Generic;
using UnityEditor;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Programmatic entry points for catalog validation that other editor tools can call directly
    /// without simulating a menu click or relying on logging.
    /// </summary>
    public static class VfxCatalogValidation
    {
        /// <summary>
        /// Validates a single catalog and attaches its source provenance.
        /// </summary>
        /// <param name="catalog">Catalog to validate.</param>
        /// <returns>Validation report.</returns>
        public static VfxCatalogValidationReport ValidateCatalog(VfxCatalog catalog)
        {
            var result = VfxCatalogValidator.Validate(catalog);
            var path = catalog != null ? AssetDatabase.GetAssetPath(catalog) : string.Empty;
            return new VfxCatalogValidationReport(catalog, path, result);
        }

        /// <summary>
        /// Validates every <see cref="VfxCatalog"/> asset in the project deterministically.
        /// </summary>
        /// <returns>Aggregate validation result.</returns>
        public static VfxProjectValidationResult ValidateAllProjectCatalogs()
        {
            var guids = AssetDatabase.FindAssets("t:VfxCatalog");
            var pairs = new List<(VfxCatalog catalog, string path)>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalog>(path);
                if (catalog != null)
                {
                    pairs.Add((catalog, path));
                }
            }

            pairs.Sort((a, b) =>
            {
                var byPath = string.CompareOrdinal(a.path, b.path);
                return byPath != 0 ? byPath : a.catalog.GetInstanceID().CompareTo(b.catalog.GetInstanceID());
            });

            var reports = new List<VfxCatalogValidationReport>(pairs.Count);
            for (var i = 0; i < pairs.Count; i++)
            {
                var result = VfxCatalogValidator.Validate(pairs[i].catalog);
                reports.Add(new VfxCatalogValidationReport(pairs[i].catalog, pairs[i].path, result));
            }

            return new VfxProjectValidationResult(reports);
        }
    }
}
