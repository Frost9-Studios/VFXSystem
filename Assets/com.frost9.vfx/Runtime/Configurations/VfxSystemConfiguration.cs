using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Global runtime configuration for Frost9 VFX services.
    /// </summary>
    [CreateAssetMenu(fileName = "VfxSystemConfiguration", menuName = "Frost9/VFX/System Configuration")]
    public class VfxSystemConfiguration : ScriptableObject
    {
        [SerializeField]
        private VfxCatalog defaultCatalog;

        [SerializeField]
        private string poolRootName = "VFXSystem_PoolRoot";

        [SerializeField]
        private bool dontDestroyPoolRootOnLoad = true;

        [SerializeField]
        [Min(0)]
        private int defaultInitialPoolSize = 4;

        [SerializeField]
        [Min(1)]
        private int defaultMaxPoolSize = 32;

        [SerializeField]
        [Min(1)]
        private int maxActiveInstances = 256;

        [SerializeField]
        private bool verboseDiagnostics;

        /// <summary>
        /// Gets default catalog asset.
        /// </summary>
        public VfxCatalog DefaultCatalog => defaultCatalog;

        /// <summary>
        /// Gets pool root object name.
        /// </summary>
        public string PoolRootName => poolRootName;

        /// <summary>
        /// Gets whether pool root should persist across scene loads.
        /// </summary>
        public bool DontDestroyPoolRootOnLoad => dontDestroyPoolRootOnLoad;

        /// <summary>
        /// Gets default initial pool size.
        /// </summary>
        public int DefaultInitialPoolSize => defaultInitialPoolSize;

        /// <summary>
        /// Gets default maximum pool size.
        /// </summary>
        public int DefaultMaxPoolSize => defaultMaxPoolSize;

        /// <summary>
        /// Gets global safety cap for active instances.
        /// </summary>
        public int MaxActiveInstances => maxActiveInstances;

        /// <summary>
        /// Gets whether verbose diagnostics are enabled.
        /// </summary>
        public bool VerboseDiagnostics => verboseDiagnostics;

        /// <summary>
        /// Loads default configuration from resources or creates a transient fallback.
        /// </summary>
        /// <param name="resourcesPath">Resources path without extension.</param>
        /// <returns>Configuration instance.</returns>
        public static VfxSystemConfiguration GetOrCreateDefault(string resourcesPath = "VfxSystemConfiguration")
        {
            var existing = Resources.Load<VfxSystemConfiguration>(resourcesPath);
            if (existing != null)
            {
                return existing;
            }

            var runtimeConfig = CreateInstance<VfxSystemConfiguration>();
            runtimeConfig.name = "RuntimeVfxSystemConfiguration";
            return runtimeConfig;
        }

        /// <summary>
        /// Runtime helper to assign defaults from code.
        /// </summary>
        /// <param name="catalog">Catalog to assign as default.</param>
        /// <param name="initialPoolSize">Default initial pool size.</param>
        /// <param name="maxPoolSize">Default max pool size.</param>
        /// <param name="maxActive">Global max active instances.</param>
        /// <param name="dontDestroyPoolRoot">Whether pool root should move to DontDestroyOnLoad scene.</param>
        /// <param name="configuredPoolRootName">Optional runtime pool root name override.</param>
        public void SetDefaultsForRuntime(
            VfxCatalog catalog,
            int initialPoolSize = 1,
            int maxPoolSize = 16,
            int maxActive = 128,
            bool dontDestroyPoolRoot = true,
            string configuredPoolRootName = null)
        {
            defaultCatalog = catalog;
            defaultInitialPoolSize = Mathf.Max(0, initialPoolSize);
            defaultMaxPoolSize = Mathf.Max(1, maxPoolSize);
            maxActiveInstances = Mathf.Max(1, maxActive);
            dontDestroyPoolRootOnLoad = dontDestroyPoolRoot;

            if (!string.IsNullOrWhiteSpace(configuredPoolRootName))
            {
                poolRootName = configuredPoolRootName;
            }
        }
    }
}
