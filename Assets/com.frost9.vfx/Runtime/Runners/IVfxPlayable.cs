using System;

namespace Frost9.VFX
{
    /// <summary>
    /// Runtime contract implemented by pooled playable effect components.
    /// </summary>
    public interface IVfxPlayable
    {
        /// <summary>
        /// Fired when effect playback naturally completes.
        /// </summary>
        event Action<IVfxPlayable> Completed;

        /// <summary>
        /// Gets whether the effect is actively playing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Resets internal visual state for reuse.
        /// </summary>
        /// <param name="args">Spawn arguments.</param>
        void Reset(in VfxSpawnArgs args);

        /// <summary>
        /// Applies runtime parameter updates.
        /// </summary>
        /// <param name="parameters">Parameters to apply.</param>
        void Apply(in VfxParams parameters);

        /// <summary>
        /// Starts effect playback.
        /// </summary>
        void Play();

        /// <summary>
        /// Stops effect playback.
        /// </summary>
        /// <param name="stopMode">Stop behavior.</param>
        void Stop(VfxStopMode stopMode);
    }
}
