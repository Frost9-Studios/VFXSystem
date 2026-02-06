using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Spawn arguments consumed by <see cref="IVfxPlayable"/> implementations.
    /// </summary>
    public readonly struct VfxSpawnArgs
    {
        /// <summary>
        /// Initializes spawn arguments.
        /// </summary>
        /// <param name="id">Catalog identifier.</param>
        /// <param name="position">Spawn position.</param>
        /// <param name="rotation">Spawn rotation.</param>
        /// <param name="parent">Optional parent transform.</param>
        /// <param name="attachMode">Attachment behavior.</param>
        /// <param name="parameters">Merged runtime parameters.</param>
        /// <param name="options">Resolved play options.</param>
        /// <param name="fallbackLifetimeSeconds">Fallback duration used by simple runners.</param>
        public VfxSpawnArgs(
            VfxId id,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            AttachMode attachMode,
            in VfxParams parameters,
            in VfxPlayOptions options,
            float fallbackLifetimeSeconds)
        {
            Id = id;
            Position = position;
            Rotation = rotation;
            Parent = parent;
            AttachMode = attachMode;
            Parameters = parameters;
            Options = options;
            FallbackLifetimeSeconds = fallbackLifetimeSeconds;
        }

        /// <summary>
        /// Gets identifier for this spawn request.
        /// </summary>
        public VfxId Id { get; }

        /// <summary>
        /// Gets world position.
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets world rotation.
        /// </summary>
        public Quaternion Rotation { get; }

        /// <summary>
        /// Gets optional parent transform.
        /// </summary>
        public Transform Parent { get; }

        /// <summary>
        /// Gets attachment behavior.
        /// </summary>
        public AttachMode AttachMode { get; }

        /// <summary>
        /// Gets merged parameters.
        /// </summary>
        public VfxParams Parameters { get; }

        /// <summary>
        /// Gets resolved play options.
        /// </summary>
        public VfxPlayOptions Options { get; }

        /// <summary>
        /// Gets fallback lifetime in seconds.
        /// </summary>
        public float FallbackLifetimeSeconds { get; }
    }
}
