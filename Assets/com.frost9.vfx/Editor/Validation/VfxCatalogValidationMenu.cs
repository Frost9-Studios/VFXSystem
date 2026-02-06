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
            var guids = AssetDatabase.FindAssets("t:VfxCatalog");
            if (guids == null || guids.Length == 0)
            {
                UnityEngine.Debug.Log("[VfxCatalogValidator] No VfxCatalog assets found.");
                return;
            }

            var totalErrors = 0;
            var totalWarnings = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalog>(path);
                var result = VfxCatalogValidationLogging.ValidateAndLog(catalog, "Menu");
                totalErrors += result.ErrorCount;
                totalWarnings += result.WarningCount;
            }

            UnityEngine.Debug.Log(
                $"[VfxCatalogValidator][Menu] Completed. Catalogs={guids.Length}, Errors={totalErrors}, Warnings={totalWarnings}.");
        }
    }
}
