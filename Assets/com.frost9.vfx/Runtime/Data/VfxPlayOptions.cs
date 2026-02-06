using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Per-play options that control channeling, ownership, and lifecycle behavior.
    /// </summary>
    [Serializable]
    public struct VfxPlayOptions
    {
        [SerializeField]
        private VfxChannel channel;

        [SerializeField]
        private GameObject owner;

        [SerializeField]
        private bool hasAutoReleaseOverride;

        [SerializeField]
        private bool autoRelease;

        [SerializeField]
        private bool ignoreTargetScale;

        /// <summary>
        /// Gets default options targeting gameplay channel.
        /// </summary>
        public static VfxPlayOptions DefaultGameplay => new VfxPlayOptions
        {
            channel = VfxChannel.Gameplay
        };

        /// <summary>
        /// Gets channel assigned to this play request.
        /// </summary>
        public VfxChannel Channel => channel;

        /// <summary>
        /// Gets optional owner object used for scoped stop operations.
        /// </summary>
        public GameObject Owner => owner;

        /// <summary>
        /// Gets whether auto-release behavior was explicitly overridden.
        /// </summary>
        public bool HasAutoReleaseOverride => hasAutoReleaseOverride;

        /// <summary>
        /// Gets explicit auto-release value.
        /// </summary>
        public bool AutoRelease => autoRelease;

        /// <summary>
        /// Gets whether target scale should be ignored for attached effects.
        /// </summary>
        public bool IgnoreTargetScale => ignoreTargetScale;

        /// <summary>
        /// Returns a copy with channel assignment.
        /// </summary>
        /// <param name="value">Target channel.</param>
        /// <returns>Updated options.</returns>
        public VfxPlayOptions WithChannel(VfxChannel value)
        {
            channel = value;
            return this;
        }

        /// <summary>
        /// Returns a copy with owner assignment.
        /// </summary>
        /// <param name="value">Owner object.</param>
        /// <returns>Updated options.</returns>
        public VfxPlayOptions WithOwner(GameObject value)
        {
            owner = value;
            return this;
        }

        /// <summary>
        /// Returns a copy with explicit auto-release behavior.
        /// </summary>
        /// <param name="value">Auto-release value.</param>
        /// <returns>Updated options.</returns>
        public VfxPlayOptions WithAutoRelease(bool value)
        {
            hasAutoReleaseOverride = true;
            autoRelease = value;
            return this;
        }

        /// <summary>
        /// Returns a copy with target-scale follow behavior.
        /// </summary>
        /// <param name="value">True to ignore target scale for attached effects.</param>
        /// <returns>Updated options.</returns>
        public VfxPlayOptions WithIgnoreTargetScale(bool value)
        {
            ignoreTargetScale = value;
            return this;
        }
    }
}
