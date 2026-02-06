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
        /// Effect follows full transform by parenting.
        /// </summary>
        AttachToTransform = 1,

        /// <summary>
        /// Effect follows target position while retaining independent rotation.
        /// </summary>
        FollowPositionOnly = 2
    }
}
