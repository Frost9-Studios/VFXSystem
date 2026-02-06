using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Manual menu actions for generating runtime VFX refs.
    /// </summary>
    public static class VfxRefsGenerationMenu
    {
        private const string GenerateMenuPath = "Tools/Frost9/VFX/Generate VFXRefs";

        /// <summary>
        /// Generates refs from all project catalogs.
        /// </summary>
        [MenuItem(GenerateMenuPath)]
        public static void Generate()
        {
            var result = VfxRefsGenerator.GenerateFromProject();
            Debug.Log(
                $"[VfxRefsGenerator] Generated {result.IdCount} ids from {result.CatalogCount} catalog(s). " +
                $"Changed={result.Changed}. Output='{result.OutputPath}'.");
        }
    }
}
