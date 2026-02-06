using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Inspector for manual validation of VfxCatalog assets.
    /// </summary>
    [CustomEditor(typeof(VfxCatalog))]
    public sealed class VfxCatalogEditor : UnityEditor.Editor
    {
        private VfxCatalogValidationResult lastResult;

        /// <summary>
        /// Draws the custom inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validate Catalog"))
            {
                var catalog = target as VfxCatalog;
                lastResult = VfxCatalogValidationLogging.ValidateAndLog(catalog, "Inspector");
            }

            if (GUILayout.Button("Generate VFXRefs"))
            {
                var generationResult = VfxRefsGenerator.GenerateFromProject();
                Debug.Log(
                    $"[VfxRefsGenerator][Inspector] Generated {generationResult.IdCount} ids from {generationResult.CatalogCount} catalog(s). " +
                    $"Changed={generationResult.Changed}. Output='{generationResult.OutputPath}'.",
                    target);
            }

            if (lastResult == null)
            {
                return;
            }

            var messageType = lastResult.HasErrors
                ? MessageType.Error
                : (lastResult.WarningCount > 0 ? MessageType.Warning : MessageType.Info);

            var message = lastResult.Issues.Count == 0
                ? "No validation issues found."
                : $"Validation found {lastResult.ErrorCount} error(s) and {lastResult.WarningCount} warning(s).";
            EditorGUILayout.HelpBox(message, messageType);
        }
    }
}
