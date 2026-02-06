namespace Frost9.VFX
{
    /// <summary>
    /// Logical VFX channels used for scoped stopping and diagnostics.
    /// </summary>
    public enum VfxChannel
    {
        /// <summary>
        /// Gameplay feedback effects.
        /// </summary>
        Gameplay = 0,

        /// <summary>
        /// User interface effects.
        /// </summary>
        UI = 1,

        /// <summary>
        /// Ambient world effects.
        /// </summary>
        Ambient = 2,

        /// <summary>
        /// Long-lived persistent effects.
        /// </summary>
        Persistent = 3
    }
}
