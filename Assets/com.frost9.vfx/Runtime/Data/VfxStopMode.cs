namespace Frost9.VFX
{
    /// <summary>
    /// Stop behavior for active visual effects.
    /// </summary>
    public enum VfxStopMode
    {
        /// <summary>
        /// Stop emission immediately and clear active visuals.
        /// </summary>
        StopEmittingAndClear = 0,

        /// <summary>
        /// Stop emission while allowing existing particles/trails to finish.
        /// </summary>
        StopEmitting = 1
    }
}
