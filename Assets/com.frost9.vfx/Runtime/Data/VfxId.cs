using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Stable identifier for a VFX catalog entry.
    /// </summary>
    [Serializable]
    public struct VfxId : IEquatable<VfxId>
    {
        [SerializeField]
        private string value;

        /// <summary>
        /// Initializes a new VFX identifier.
        /// </summary>
        /// <param name="value">Stable identifier value.</param>
        public VfxId(string value)
        {
            this.value = value;
        }

        /// <summary>
        /// Gets the raw identifier value.
        /// </summary>
        public string Value => value ?? string.Empty;

        /// <summary>
        /// Gets whether the identifier contains a non-empty value.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// Creates an identifier from a string value.
        /// </summary>
        /// <param name="value">Identifier text.</param>
        /// <returns>Created identifier.</returns>
        public static VfxId From(string value)
        {
            return new VfxId(value);
        }

        /// <summary>
        /// Converts the identifier to its string value.
        /// </summary>
        /// <returns>Identifier text.</returns>
        public override string ToString()
        {
            return Value;
        }

        /// <summary>
        /// Compares this identifier with another identifier.
        /// </summary>
        /// <param name="other">Other identifier.</param>
        /// <returns>True when values match exactly.</returns>
        public bool Equals(VfxId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares this identifier with another object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when object is equal identifier.</returns>
        public override bool Equals(object obj)
        {
            return obj is VfxId other && Equals(other);
        }

        /// <summary>
        /// Gets hash code for dictionary usage.
        /// </summary>
        /// <returns>Stable hash code.</returns>
        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        /// <summary>
        /// Equality operator for identifiers.
        /// </summary>
        /// <param name="left">Left identifier.</param>
        /// <param name="right">Right identifier.</param>
        /// <returns>True when equal.</returns>
        public static bool operator ==(VfxId left, VfxId right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for identifiers.
        /// </summary>
        /// <param name="left">Left identifier.</param>
        /// <param name="right">Right identifier.</param>
        /// <returns>True when not equal.</returns>
        public static bool operator !=(VfxId left, VfxId right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Implicit conversion from string to VfxId.
        /// </summary>
        /// <param name="value">Identifier text.</param>
        public static implicit operator VfxId(string value)
        {
            return new VfxId(value);
        }
    }
}
