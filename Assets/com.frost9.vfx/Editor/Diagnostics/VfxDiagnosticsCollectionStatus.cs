namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Status returned by diagnostics collection.
    /// </summary>
    public enum VfxDiagnosticsCollectionStatus
    {
        /// <summary>
        /// Unity is not in play mode, so runtime stats are unavailable.
        /// </summary>
        NotInPlayMode = 0,

        /// <summary>
        /// No runtime pool manager instances were found.
        /// </summary>
        NoPoolManagers = 1,

        /// <summary>
        /// Runtime stats were collected successfully.
        /// </summary>
        Success = 2
    }
}
