using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Unified runtime service interface for spawning and controlling VFX.
    /// </summary>
    public interface IVfxService : IDisposable
    {
        /// <summary>
        /// Plays an effect at world position.
        /// </summary>
        /// <param name="id">Catalog identifier.</param>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation. Default quaternion resolves to identity.</param>
        /// <param name="parameters">Optional parameter overrides.</param>
        /// <param name="options">Optional play options.</param>
        /// <returns>Handle for later stop/update calls.</returns>
        VfxHandle PlayAt(
            VfxId id,
            Vector3 position,
            Quaternion rotation = default,
            VfxParams? parameters = null,
            VfxPlayOptions? options = null);

        /// <summary>
        /// Plays an effect in relation to a target object.
        /// </summary>
        /// <param name="id">Catalog identifier.</param>
        /// <param name="target">Target object.</param>
        /// <param name="attachMode">Attachment behavior.</param>
        /// <param name="parameters">Optional parameter overrides.</param>
        /// <param name="options">Optional play options.</param>
        /// <returns>Handle for later stop/update calls.</returns>
        VfxHandle PlayOn(
            VfxId id,
            GameObject target,
            AttachMode attachMode,
            VfxParams? parameters = null,
            VfxPlayOptions? options = null);

        /// <summary>
        /// Stops an active effect by handle.
        /// </summary>
        /// <param name="handle">Effect handle.</param>
        /// <returns>True when a matching active instance was stopped.</returns>
        bool Stop(VfxHandle handle);

        /// <summary>
        /// Stops active effects matching the filter.
        /// </summary>
        /// <param name="filter">Optional scope filter. Null defaults to gameplay channel only.</param>
        /// <returns>Number of stopped instances.</returns>
        int StopAll(VfxStopFilter? filter = null);

        /// <summary>
        /// Attempts to apply parameter updates to a currently active handle.
        /// </summary>
        /// <param name="handle">Effect handle.</param>
        /// <param name="parameters">Parameters to apply.</param>
        /// <returns>True when handle is active and updated.</returns>
        bool TryUpdate(VfxHandle handle, in VfxParams parameters);

        /// <summary>
        /// Gets runtime pool statistics snapshot.
        /// </summary>
        /// <returns>Current statistics.</returns>
        VfxStatsSnapshot GetStats();
    }
}
