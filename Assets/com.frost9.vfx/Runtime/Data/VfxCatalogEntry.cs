using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Catalog entry describing a playable effect and its pooling defaults.
    /// </summary>
    [Serializable]
    public class VfxCatalogEntry
    {
        [SerializeField]
        private VfxId id;

        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        [Min(0)]
        private int initialPoolSize = 4;

        [SerializeField]
        [Min(1)]
        private int maxPoolSize = 32;

        [SerializeField]
        private bool allowPoolExpansion = true;

        [SerializeField]
        private VfxChannel defaultChannel = VfxChannel.Gameplay;

        [SerializeField]
        private bool autoReleaseByDefault = true;

        [SerializeField]
        [Min(0f)]
        private float fallbackLifetimeSeconds = 1.25f;

        [SerializeField]
        private VfxParams defaultParameters;

        /// <summary>
        /// Initializes an empty catalog entry.
        /// </summary>
        public VfxCatalogEntry()
        {
        }

        /// <summary>
        /// Initializes a catalog entry with an id and prefab.
        /// </summary>
        /// <param name="id">Identifier value.</param>
        /// <param name="prefab">Playable prefab.</param>
        public VfxCatalogEntry(VfxId id, GameObject prefab)
        {
            this.id = id;
            this.prefab = prefab;
        }

        /// <summary>
        /// Gets effect identifier.
        /// </summary>
        public VfxId Id => id;

        /// <summary>
        /// Gets effect prefab.
        /// </summary>
        public GameObject Prefab => prefab;

        /// <summary>
        /// Gets initial pool size for this entry.
        /// </summary>
        public int InitialPoolSize => initialPoolSize;

        /// <summary>
        /// Gets maximum pool size for this entry.
        /// </summary>
        public int MaxPoolSize => maxPoolSize;

        /// <summary>
        /// Gets whether this entry allows growth beyond prewarmed count.
        /// </summary>
        public bool AllowPoolExpansion => allowPoolExpansion;

        /// <summary>
        /// Gets default channel for this entry.
        /// </summary>
        public VfxChannel DefaultChannel => defaultChannel;

        /// <summary>
        /// Gets default auto-release behavior.
        /// </summary>
        public bool AutoReleaseByDefault => autoReleaseByDefault;

        /// <summary>
        /// Gets fallback lifetime in seconds.
        /// </summary>
        public float FallbackLifetimeSeconds => fallbackLifetimeSeconds;

        /// <summary>
        /// Gets default parameter values for this entry.
        /// </summary>
        public VfxParams DefaultParameters => defaultParameters;

        /// <summary>
        /// Resolves runtime parameters for a play request.
        /// </summary>
        /// <param name="overrideParameters">Optional override values.</param>
        /// <returns>Merged parameter set.</returns>
        public VfxParams ResolveParameters(VfxParams? overrideParameters)
        {
            if (!overrideParameters.HasValue)
            {
                return defaultParameters;
            }

            return overrideParameters.Value.Merge(defaultParameters);
        }

        /// <summary>
        /// Resolves runtime play options for a play request.
        /// </summary>
        /// <param name="overrideOptions">Optional override options.</param>
        /// <returns>Resolved options.</returns>
        public VfxPlayOptions ResolveOptions(VfxPlayOptions? overrideOptions)
        {
            if (!overrideOptions.HasValue)
            {
                return VfxPlayOptions.DefaultGameplay
                    .WithChannel(defaultChannel)
                    .WithAutoRelease(autoReleaseByDefault);
            }

            var options = overrideOptions.Value;
            if (!options.HasAutoReleaseOverride)
            {
                options = options.WithAutoRelease(autoReleaseByDefault);
            }

            return options;
        }
    }
}
