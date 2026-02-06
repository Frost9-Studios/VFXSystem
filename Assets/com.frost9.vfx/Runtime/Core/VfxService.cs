using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Default runtime implementation of <see cref="IVfxService"/>.
    /// </summary>
    public class VfxService : IVfxService
    {
        private readonly VfxPoolManager poolManager;
        private readonly VfxCatalog catalog;
        private readonly VfxSystemConfiguration configuration;
        private readonly bool ownsPoolManagerGameObject;
        private bool disposed;

        /// <summary>
        /// Initializes a service from an existing pool manager.
        /// </summary>
        /// <param name="poolManager">Pool manager component.</param>
        /// <param name="catalog">Optional catalog override.</param>
        /// <param name="configuration">Optional configuration override.</param>
        /// <param name="ownsPoolManagerGameObject">Whether this service destroys the manager object on dispose.</param>
        public VfxService(
            VfxPoolManager poolManager,
            VfxCatalog catalog = null,
            VfxSystemConfiguration configuration = null,
            bool ownsPoolManagerGameObject = false)
        {
            this.poolManager = poolManager ?? throw new ArgumentNullException(nameof(poolManager));
            this.configuration = configuration ?? VfxSystemConfiguration.GetOrCreateDefault();
            this.catalog = catalog ?? this.configuration.DefaultCatalog ?? VfxCatalog.LoadFromResources();
            this.ownsPoolManagerGameObject = ownsPoolManagerGameObject;

            this.poolManager.Initialize(this.catalog, this.configuration);
        }

        /// <summary>
        /// Plays an effect at world position.
        /// </summary>
        /// <param name="id">Catalog identifier.</param>
        /// <param name="position">World position.</param>
        /// <param name="rotation">World rotation.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <param name="options">Optional play options.</param>
        /// <returns>Resulting handle, or invalid when spawn fails.</returns>
        public VfxHandle PlayAt(
            VfxId id,
            Vector3 position,
            Quaternion rotation = default,
            VfxParams? parameters = null,
            VfxPlayOptions? options = null)
        {
            ThrowIfDisposed();
            return PlayInternal(id, position, rotation, null, AttachMode.WorldLocked, parameters, options);
        }

        /// <summary>
        /// Plays an effect using a target object and attach mode.
        /// </summary>
        /// <param name="id">Catalog identifier.</param>
        /// <param name="target">Target object.</param>
        /// <param name="attachMode">Attach behavior.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <param name="options">Optional play options.</param>
        /// <returns>Resulting handle, or invalid when spawn fails.</returns>
        public VfxHandle PlayOn(
            VfxId id,
            GameObject target,
            AttachMode attachMode,
            VfxParams? parameters = null,
            VfxPlayOptions? options = null)
        {
            ThrowIfDisposed();
            if (target == null)
            {
                WarnOnceLogger.Log("vfx_target_null", "[VfxService] PlayOn called with null target.");
                return VfxHandle.Invalid;
            }

            return PlayInternal(id, target.transform.position, target.transform.rotation, target.transform, attachMode, parameters, options);
        }

        /// <summary>
        /// Stops an active effect by handle.
        /// </summary>
        /// <param name="handle">Handle to stop.</param>
        /// <returns>True when instance was stopped.</returns>
        public bool Stop(VfxHandle handle)
        {
            ThrowIfDisposed();
            return poolManager.Stop(handle);
        }

        /// <summary>
        /// Stops all effects that match the provided filter.
        /// </summary>
        /// <param name="filter">Optional filter. Null defaults to gameplay channel.</param>
        /// <returns>Number of stopped effects.</returns>
        public int StopAll(VfxStopFilter? filter = null)
        {
            ThrowIfDisposed();
            var resolvedFilter = filter ?? VfxStopFilter.GameplayDefault;
            return poolManager.StopAll(resolvedFilter);
        }

        /// <summary>
        /// Applies parameter updates to an active effect.
        /// </summary>
        /// <param name="handle">Target handle.</param>
        /// <param name="parameters">Parameters to apply.</param>
        /// <returns>True when update succeeded.</returns>
        public bool TryUpdate(VfxHandle handle, in VfxParams parameters)
        {
            ThrowIfDisposed();
            return poolManager.TryUpdate(handle, parameters);
        }

        /// <summary>
        /// Gets current pool statistics.
        /// </summary>
        /// <returns>Statistics snapshot.</returns>
        public VfxStatsSnapshot GetStats()
        {
            ThrowIfDisposed();
            return poolManager.GetStats();
        }

        /// <summary>
        /// Disposes the service and owned runtime resources.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            poolManager.ClearAll();

            if (ownsPoolManagerGameObject && poolManager != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(poolManager.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(poolManager.gameObject);
                }
            }
        }

        private VfxHandle PlayInternal(
            VfxId id,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            AttachMode attachMode,
            VfxParams? parameterOverrides,
            VfxPlayOptions? optionOverrides)
        {
            if (catalog == null)
            {
                WarnOnceLogger.Log("vfx_service_missing_catalog", "[VfxService] No VFX catalog available.");
                return VfxHandle.Invalid;
            }

            if (!catalog.TryGetEntry(id, out var entry))
            {
                WarnOnceLogger.Log($"vfx_service_missing_id_{id}", $"[VfxService] Unknown VFX id: {id}");
                return VfxHandle.Invalid;
            }

            var mergedParameters = entry.ResolveParameters(parameterOverrides);
            var resolvedOptions = entry.ResolveOptions(optionOverrides);

            var resolvedRotation = IsDefaultQuaternion(rotation) ? Quaternion.identity : rotation;
            var spawnArgs = new VfxSpawnArgs(
                id,
                position,
                resolvedRotation,
                parent,
                attachMode,
                mergedParameters,
                resolvedOptions,
                entry.FallbackLifetimeSeconds);

            return poolManager.TryPlay(spawnArgs, out var handle)
                ? handle
                : VfxHandle.Invalid;
        }

        private static bool IsDefaultQuaternion(Quaternion quaternion)
        {
            return quaternion.x == 0f &&
                   quaternion.y == 0f &&
                   quaternion.z == 0f &&
                   quaternion.w == 0f;
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(VfxService));
            }
        }
    }
}
