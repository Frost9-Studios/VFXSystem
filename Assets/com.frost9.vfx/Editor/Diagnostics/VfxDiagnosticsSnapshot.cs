using System;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Immutable diagnostics payload for the VFX diagnostics window.
    /// </summary>
    public readonly struct VfxDiagnosticsSnapshot
    {
        /// <summary>
        /// Initializes a diagnostics snapshot.
        /// </summary>
        /// <param name="status">Collection status.</param>
        /// <param name="message">Status message.</param>
        /// <param name="managers">Collected manager snapshots.</param>
        /// <param name="selectedIndex">Selected manager index.</param>
        public VfxDiagnosticsSnapshot(
            VfxDiagnosticsCollectionStatus status,
            string message,
            VfxDiagnosticsManagerSnapshot[] managers,
            int selectedIndex)
        {
            Status = status;
            Message = message ?? string.Empty;
            Managers = managers ?? Array.Empty<VfxDiagnosticsManagerSnapshot>();
            SelectedIndex = selectedIndex;
        }

        /// <summary>
        /// Gets collection status.
        /// </summary>
        public VfxDiagnosticsCollectionStatus Status { get; }

        /// <summary>
        /// Gets status message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets collected managers.
        /// </summary>
        public VfxDiagnosticsManagerSnapshot[] Managers { get; }

        /// <summary>
        /// Gets selected manager index.
        /// </summary>
        public int SelectedIndex { get; }
    }

    /// <summary>
    /// Snapshot for a single runtime pool manager instance.
    /// </summary>
    public readonly struct VfxDiagnosticsManagerSnapshot
    {
        /// <summary>
        /// Initializes a manager snapshot.
        /// </summary>
        /// <param name="instanceId">Manager instance id.</param>
        /// <param name="name">Manager game object name.</param>
        /// <param name="hierarchyPath">Manager hierarchy path.</param>
        /// <param name="stats">Runtime stats snapshot.</param>
        public VfxDiagnosticsManagerSnapshot(
            int instanceId,
            string name,
            string hierarchyPath,
            VfxStatsSnapshot stats)
        {
            InstanceId = instanceId;
            Name = name ?? string.Empty;
            HierarchyPath = hierarchyPath ?? string.Empty;
            Stats = stats;
        }

        /// <summary>
        /// Gets manager instance id.
        /// </summary>
        public int InstanceId { get; }

        /// <summary>
        /// Gets manager object name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets manager transform hierarchy path.
        /// </summary>
        public string HierarchyPath { get; }

        /// <summary>
        /// Gets runtime stats for this manager.
        /// </summary>
        public VfxStatsSnapshot Stats { get; }
    }
}
