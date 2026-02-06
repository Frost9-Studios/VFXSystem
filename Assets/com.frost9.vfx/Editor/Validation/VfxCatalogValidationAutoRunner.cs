using System.Collections.Generic;
using UnityEditor;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Throttled automatic catalog validation when related assets change.
    /// </summary>
    public sealed class VfxCatalogValidationAutoRunner : AssetPostprocessor
    {
        private static readonly HashSet<string> PendingCatalogPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        private static bool updateHookRegistered;
        private static bool runScheduled;
        private static double scheduledRunAt;

        private const double DelaySeconds = 0.35d;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            CollectCatalogPaths(importedAssets);
            CollectCatalogPaths(movedAssets);

            if (PendingCatalogPaths.Count == 0)
            {
                return;
            }

            ScheduleRun();
        }

        private static void CollectCatalogPaths(string[] assetPaths)
        {
            if (assetPaths == null || assetPaths.Length == 0)
            {
                return;
            }

            for (var i = 0; i < assetPaths.Length; i++)
            {
                var path = assetPaths[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalog>(path);
                if (catalog == null)
                {
                    continue;
                }

                PendingCatalogPaths.Add(path);
            }
        }

        private static void ScheduleRun()
        {
            scheduledRunAt = EditorApplication.timeSinceStartup + DelaySeconds;
            runScheduled = true;

            if (updateHookRegistered)
            {
                return;
            }

            updateHookRegistered = true;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (!runScheduled || EditorApplication.timeSinceStartup < scheduledRunAt)
            {
                return;
            }

            runScheduled = false;
            ValidatePendingCatalogs();
        }

        private static void ValidatePendingCatalogs()
        {
            if (PendingCatalogPaths.Count == 0)
            {
                return;
            }

            var paths = new string[PendingCatalogPaths.Count];
            PendingCatalogPaths.CopyTo(paths);
            PendingCatalogPaths.Clear();

            for (var i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalog>(path);
                if (catalog == null)
                {
                    continue;
                }

                VfxCatalogValidationLogging.ValidateAndLog(catalog, "Auto");
            }
        }
    }
}
