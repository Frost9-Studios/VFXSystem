namespace Frost9.VFX
{
    /// <summary>
    /// Attachment behavior for effects spawned with a target object.
    /// </summary>
    public enum AttachMode
    {
        /// <summary>
        /// Effect remains in world space after spawn.
        /// </summary>
        WorldLocked = 0,

        /// <summary>
        /// Effect follows target position and rotation.
        /// </summary>
        FollowTransform = 1,

        /// <summary>
        /// Effect follows target position while retaining independent rotation.
        /// </summary>
        FollowPositionOnly = 2,

        /// <summary>
        /// Deprecated alias for <see cref="FollowTransform"/>.
        /// </summary>
        [System.Obsolete("Use FollowTransform.")]
        AttachToTransform = FollowTransform
    }
}
