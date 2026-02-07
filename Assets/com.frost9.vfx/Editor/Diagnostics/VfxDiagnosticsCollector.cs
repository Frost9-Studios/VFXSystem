using System;
using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Collects read-only runtime diagnostics snapshots for editor tooling.
    /// </summary>
    public static class VfxDiagnosticsCollector
    {
        /// <summary>
        /// Collects diagnostics data for all runtime pool managers.
        /// </summary>
        /// <param name="preferredManagerInstanceId">Optional preferred manager instance id.</param>
        /// <returns>Diagnostics snapshot payload.</returns>
        public static VfxDiagnosticsSnapshot Collect(int preferredManagerInstanceId = 0)
        {
            if (!EditorApplication.isPlaying)
            {
                return new VfxDiagnosticsSnapshot(
                    VfxDiagnosticsCollectionStatus.NotInPlayMode,
                    "Runtime VFX diagnostics are available only in Play Mode.",
                    Array.Empty<VfxDiagnosticsManagerSnapshot>(),
                    -1);
            }

            var managers = UnityEngine.Object.FindObjectsByType<VfxPoolManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (managers == null || managers.Length == 0)
            {
                return new VfxDiagnosticsSnapshot(
                    VfxDiagnosticsCollectionStatus.NoPoolManagers,
                    "No VfxPoolManager instances found in the running player context.",
                    Array.Empty<VfxDiagnosticsManagerSnapshot>(),
                    -1);
            }

            Array.Sort(managers, CompareManagers);

            var managerSnapshots = new VfxDiagnosticsManagerSnapshot[managers.Length];
            var selectedIndex = 0;
            for (var i = 0; i < managers.Length; i++)
            {
                var manager = managers[i];
                managerSnapshots[i] = new VfxDiagnosticsManagerSnapshot(
                    manager.GetInstanceID(),
                    manager.gameObject.name,
                    BuildHierarchyPath(manager.transform),
                    manager.GetStats());

                if (preferredManagerInstanceId != 0 && manager.GetInstanceID() == preferredManagerInstanceId)
                {
                    selectedIndex = i;
                }
            }

            return new VfxDiagnosticsSnapshot(
                VfxDiagnosticsCollectionStatus.Success,
                "Runtime VFX diagnostics collected successfully.",
                managerSnapshots,
                selectedIndex);
        }

        private static int CompareManagers(VfxPoolManager left, VfxPoolManager right)
        {
            var byName = string.CompareOrdinal(left.gameObject.name, right.gameObject.name);
            if (byName != 0)
            {
                return byName;
            }

            return left.GetInstanceID().CompareTo(right.GetInstanceID());
        }

        private static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            var path = transform.name;
            var current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
