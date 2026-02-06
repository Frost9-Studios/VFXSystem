namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Severity classification for catalog validation issues.
    /// </summary>
    public enum VfxCatalogValidationSeverity
    {
        /// <summary>
        /// Informational issue that does not require changes.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Non-fatal issue that should usually be fixed.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Fatal issue that should block content usage.
        /// </summary>
        Error = 2
    }
}
