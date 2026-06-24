using UnityEditor;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Manual menu actions for validating VFX catalogs.
    /// </summary>
    public static class VfxCatalogValidationMenu
    {
        private const string ValidateAllMenuPath = "Tools/Frost9/VFX/Validate All Catalogs";

        /// <summary>
        /// Validates all VfxCatalog assets in the project.
        /// </summary>
        [MenuItem(ValidateAllMenuPath)]
        public static void ValidateAllCatalogs()
        {
            var aggregate = VfxCatalogValidation.ValidateAllProjectCatalogs();
            if (aggregate.CatalogCount == 0)
            {
                UnityEngine.Debug.Log("[VfxCatalogValidator] No VfxCatalog assets found.");
                return;
            }

            for (var i = 0; i < aggregate.Reports.Count; i++)
            {
                var report = aggregate.Reports[i];
                VfxCatalogValidationLogging.LogResult(report.Catalog, report.Result, "Menu");
            }

            UnityEngine.Debug.Log($"[VfxCatalogValidator][Menu] Completed. {aggregate.Summary}");
        }
    }
}
