using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Static convenience facade for VFX service access without explicit DI wiring.
    /// </summary>
    public static class VfxManager
    {
        private static IVfxService instance;
        private static GameObject poolManagerObject;

        /// <summary>
        /// Gets singleton runtime service instance.
        /// </summary>
        public static IVfxService Instance
        {
            get
            {
                if (instance == null)
                {
                    var managerObject = new GameObject("VFXSystem_PoolManager");
                    var poolManager = managerObject.AddComponent<VfxPoolManager>();
                    Object.DontDestroyOnLoad(managerObject);

                    instance = new VfxService(poolManager, ownsPoolManagerGameObject: true);
                    poolManagerObject = managerObject;
                }

                return instance;
            }
        }

        /// <summary>
        /// Plays an effect at world position.
        /// </summary>
        /// <param name="id">Catalog identifier.</param>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <param name="options">Optional options.</param>
        /// <returns>Handle for later operations.</returns>
        public static VfxHandle PlayAt(
            VfxId id,
            Vector3 position,
            Quaternion rotation = default,
            VfxParams? parameters = null,
            VfxPlayOptions? options = null)
        {
            return Instance.PlayAt(id, position, rotation, parameters, options);
        }

        /// <summary>
        /// Plays an effect attached to a target.
        /// </summary>
        /// <param name="id">Catalog identifier.</param>
        /// <param name="target">Target object.</param>
        /// <param name="attachMode">Attach behavior.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <param name="options">Optional options.</param>
        /// <returns>Handle for later operations.</returns>
        public static VfxHandle PlayOn(
            VfxId id,
            GameObject target,
            AttachMode attachMode,
            VfxParams? parameters = null,
            VfxPlayOptions? options = null)
        {
            return Instance.PlayOn(id, target, attachMode, parameters, options);
        }

        /// <summary>
        /// Stops a specific handle.
        /// </summary>
        /// <param name="handle">Handle to stop.</param>
        /// <returns>True when stopped.</returns>
        public static bool Stop(VfxHandle handle)
        {
            return Instance.Stop(handle);
        }

        /// <summary>
        /// Stops effects matching a filter.
        /// </summary>
        /// <param name="filter">Optional stop filter.</param>
        /// <returns>Stopped instance count.</returns>
        public static int StopAll(VfxStopFilter? filter = null)
        {
            return Instance.StopAll(filter);
        }

        /// <summary>
        /// Applies parameters to an active handle.
        /// </summary>
        /// <param name="handle">Target handle.</param>
        /// <param name="parameters">Parameters to apply.</param>
        /// <returns>True when update succeeded.</returns>
        public static bool TryUpdate(VfxHandle handle, in VfxParams parameters)
        {
            return Instance.TryUpdate(handle, parameters);
        }

        /// <summary>
        /// Gets current stats snapshot.
        /// </summary>
        /// <returns>Statistics snapshot.</returns>
        public static VfxStatsSnapshot GetStats()
        {
            return Instance.GetStats();
        }

        /// <summary>
        /// Cleans up static manager state.
        /// </summary>
        public static void Cleanup()
        {
            instance?.Dispose();
            instance = null;

            if (poolManagerObject != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(poolManagerObject);
                }
                else
                {
                    Object.DestroyImmediate(poolManagerObject);
                }

                poolManagerObject = null;
            }
        }
    }
}
