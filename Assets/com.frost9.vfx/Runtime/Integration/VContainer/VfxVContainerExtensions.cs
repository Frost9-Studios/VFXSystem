#if VCONTAINER_AVAILABLE
using System;
using UnityEngine;
using VContainer;

namespace Frost9.VFX.Integration.VContainer
{
    /// <summary>
    /// VContainer registration helpers for Frost9.VFX runtime services.
    /// </summary>
    public static class VfxVContainerExtensions
    {
        /// <summary>
        /// Registers Frost9.VFX runtime services in a VContainer builder.
        /// </summary>
        /// <param name="builder">Container builder.</param>
        /// <param name="catalog">Optional explicit catalog instance.</param>
        /// <param name="configuration">Optional explicit runtime configuration instance.</param>
        /// <param name="poolManagerObjectName">Pool manager GameObject name used for create-or-reuse.</param>
        /// <param name="dontDestroyOnLoad">Whether the pool manager should persist across scene loads.</param>
        public static void RegisterVfx(
            this IContainerBuilder builder,
            VfxCatalog catalog = null,
            VfxSystemConfiguration configuration = null,
            string poolManagerObjectName = "VFXSystem_PoolManager",
            bool dontDestroyOnLoad = true)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configuration != null)
            {
                builder.RegisterInstance(configuration);
            }
            else
            {
                builder.Register<VfxSystemConfiguration>(_ => VfxSystemConfiguration.GetOrCreateDefault(), Lifetime.Singleton);
            }

            if (catalog != null)
            {
                builder.RegisterInstance(catalog);
            }
            else
            {
                builder.Register<VfxCatalog>(resolver => ResolveCatalog(resolver, null), Lifetime.Singleton);
            }

            builder.Register<VfxPoolManager>(
                _ => GetOrCreatePoolManager(poolManagerObjectName, dontDestroyOnLoad),
                Lifetime.Singleton);

            builder.Register<IVfxService, VfxService>(Lifetime.Singleton)
                .WithParameter(false);
        }

        /// <summary>
        /// Registers Frost9.VFX runtime services with a pre-existing pool manager.
        /// </summary>
        /// <param name="builder">Container builder.</param>
        /// <param name="existingPoolManager">Pre-existing pool manager instance.</param>
        /// <param name="catalog">Optional explicit catalog instance.</param>
        /// <param name="configuration">Optional explicit runtime configuration instance.</param>
        public static void RegisterVfxWithExistingPool(
            this IContainerBuilder builder,
            VfxPoolManager existingPoolManager,
            VfxCatalog catalog = null,
            VfxSystemConfiguration configuration = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (existingPoolManager == null)
            {
                throw new ArgumentNullException(nameof(existingPoolManager));
            }

            builder.RegisterInstance(existingPoolManager);

            if (configuration != null)
            {
                builder.RegisterInstance(configuration);
            }
            else
            {
                builder.Register<VfxSystemConfiguration>(_ => VfxSystemConfiguration.GetOrCreateDefault(), Lifetime.Singleton);
            }

            if (catalog != null)
            {
                builder.RegisterInstance(catalog);
            }
            else
            {
                builder.Register<VfxCatalog>(resolver => ResolveCatalog(resolver, null), Lifetime.Singleton);
            }

            builder.Register<IVfxService, VfxService>(Lifetime.Singleton)
                .WithParameter(false);
        }

        private static VfxPoolManager GetOrCreatePoolManager(string poolManagerObjectName, bool dontDestroyOnLoad)
        {
            var resolvedName = string.IsNullOrWhiteSpace(poolManagerObjectName)
                ? "VFXSystem_PoolManager"
                : poolManagerObjectName;

            var managers = UnityEngine.Object.FindObjectsByType<VfxPoolManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < managers.Length; i++)
            {
                var manager = managers[i];
                if (manager == null || manager.gameObject == null)
                {
                    continue;
                }

                if (!string.Equals(manager.gameObject.name, resolvedName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (dontDestroyOnLoad)
                {
                    UnityEngine.Object.DontDestroyOnLoad(manager.gameObject);
                }

                return manager;
            }

            var poolManagerObject = new GameObject(resolvedName);
            var createdManager = poolManagerObject.AddComponent<VfxPoolManager>();
            if (dontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(poolManagerObject);
            }

            return createdManager;
        }

        private static VfxCatalog ResolveCatalog(IObjectResolver resolver, VfxCatalog explicitCatalog)
        {
            if (explicitCatalog != null)
            {
                return explicitCatalog;
            }

            if (resolver != null &&
                resolver.TryResolve(typeof(VfxSystemConfiguration), out var configObject) &&
                configObject is VfxSystemConfiguration resolvedConfiguration &&
                resolvedConfiguration.DefaultCatalog != null)
            {
                return resolvedConfiguration.DefaultCatalog;
            }

            var resourceCatalog = VfxCatalog.LoadFromResources();
            if (resourceCatalog != null)
            {
                return resourceCatalog;
            }

            WarnOnceLogger.Log(
                "vfx_vcontainer_missing_catalog",
                "[VfxVContainerExtensions] No VfxCatalog found (explicit/config/resources). Registered a runtime empty catalog.");

            var runtimeCatalog = ScriptableObject.CreateInstance<VfxCatalog>();
            runtimeCatalog.name = "RuntimeEmptyVfxCatalog";
            runtimeCatalog.hideFlags = HideFlags.DontSave;
            return runtimeCatalog;
        }
    }
}
#endif
