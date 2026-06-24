namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Centralized serialized property names for catalog mutation, so property paths are defined once
    /// and a missing layout can be detected rather than silently corrupting data.
    /// </summary>
    internal static class VfxCatalogSerializedNames
    {
        /// <summary>Backing list field on <c>VfxCatalog</c>.</summary>
        public const string Entries = "entries";

        /// <summary>Id field on <c>VfxCatalogEntry</c>.</summary>
        public const string Id = "id";

        /// <summary>String value field on <c>VfxId</c>.</summary>
        public const string IdValue = "value";

        /// <summary>Prefab field on <c>VfxCatalogEntry</c>.</summary>
        public const string Prefab = "prefab";

        /// <summary>Initial pool size field on <c>VfxCatalogEntry</c>.</summary>
        public const string InitialPoolSize = "initialPoolSize";

        /// <summary>Max pool size field on <c>VfxCatalogEntry</c>.</summary>
        public const string MaxPoolSize = "maxPoolSize";

        /// <summary>Pool expansion field on <c>VfxCatalogEntry</c>.</summary>
        public const string AllowPoolExpansion = "allowPoolExpansion";

        /// <summary>Default channel field on <c>VfxCatalogEntry</c>.</summary>
        public const string DefaultChannel = "defaultChannel";

        /// <summary>Auto-release field on <c>VfxCatalogEntry</c>.</summary>
        public const string AutoReleaseByDefault = "autoReleaseByDefault";

        /// <summary>Fallback lifetime field on <c>VfxCatalogEntry</c>.</summary>
        public const string FallbackLifetimeSeconds = "fallbackLifetimeSeconds";

        /// <summary>Default parameters field on <c>VfxCatalogEntry</c>.</summary>
        public const string DefaultParameters = "defaultParameters";

        // Documented defaults — must mirror the C# field initializers on VfxCatalogEntry.
        public const int DefaultInitialPoolSize = 4;
        public const int DefaultMaxPoolSize = 32;
        public const bool DefaultAllowPoolExpansion = true;
        public const int DefaultChannelValue = (int)VfxChannel.Gameplay;
        public const bool DefaultAutoReleaseByDefault = true;
        public const float DefaultFallbackLifetimeSeconds = 1.25f;
    }
}
