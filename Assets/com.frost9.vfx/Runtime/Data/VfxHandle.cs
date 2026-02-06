using System;

namespace Frost9.VFX
{
    /// <summary>
    /// Opaque handle identifying a currently active VFX instance.
    /// </summary>
    [Serializable]
    public readonly struct VfxHandle : IEquatable<VfxHandle>
    {
        /// <summary>
        /// Gets invalid handle value.
        /// </summary>
        public static VfxHandle Invalid => default;

        /// <summary>
        /// Initializes a new handle.
        /// </summary>
        /// <param name="slotIndex">Pool slot index.</param>
        /// <param name="generation">Pool slot generation.</param>
        internal VfxHandle(int slotIndex, uint generation)
        {
            SlotIndex = slotIndex;
            Generation = generation;
        }

        /// <summary>
        /// Gets pool slot index.
        /// </summary>
        internal int SlotIndex { get; }

        /// <summary>
        /// Gets slot generation.
        /// </summary>
        internal uint Generation { get; }

        /// <summary>
        /// Gets whether this handle appears valid.
        /// </summary>
        public bool IsValid => SlotIndex > 0 && Generation > 0;

        /// <summary>
        /// Compares this handle with another handle.
        /// </summary>
        /// <param name="other">Other handle.</param>
        /// <returns>True when equal.</returns>
        public bool Equals(VfxHandle other)
        {
            return SlotIndex == other.SlotIndex && Generation == other.Generation;
        }

        /// <summary>
        /// Compares this handle with another object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when equal handle.</returns>
        public override bool Equals(object obj)
        {
            return obj is VfxHandle other && Equals(other);
        }

        /// <summary>
        /// Gets hash code for dictionary usage.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (SlotIndex * 397) ^ (int)Generation;
            }
        }

        /// <summary>
        /// Equality operator for handles.
        /// </summary>
        /// <param name="left">Left handle.</param>
        /// <param name="right">Right handle.</param>
        /// <returns>True when equal.</returns>
        public static bool operator ==(VfxHandle left, VfxHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for handles.
        /// </summary>
        /// <param name="left">Left handle.</param>
        /// <param name="right">Right handle.</param>
        /// <returns>True when not equal.</returns>
        public static bool operator !=(VfxHandle left, VfxHandle right)
        {
            return !left.Equals(right);
        }
    }
}
