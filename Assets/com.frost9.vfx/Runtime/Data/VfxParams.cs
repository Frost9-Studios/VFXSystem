using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Typed runtime parameters applied to a spawned effect instance.
    /// </summary>
    [Serializable]
    public struct VfxParams
    {
        [SerializeField]
        private bool hasColor;
        [SerializeField]
        private Color color;

        [SerializeField]
        private bool hasScale;
        [SerializeField]
        private float scale;

        [SerializeField]
        private bool hasIntensity;
        [SerializeField]
        private float intensity;

        [SerializeField]
        private bool hasLifetimeOverride;
        [SerializeField]
        private float lifetimeOverride;

        [SerializeField]
        private bool hasTargetPoint;
        [SerializeField]
        private Vector3 targetPoint;

        /// <summary>
        /// Gets an empty parameters struct.
        /// </summary>
        public static VfxParams Empty => default;

        /// <summary>
        /// Gets whether a color value is set.
        /// </summary>
        public bool HasColor => hasColor;

        /// <summary>
        /// Gets configured color.
        /// </summary>
        public Color Color => color;

        /// <summary>
        /// Gets whether a scale override is set.
        /// </summary>
        public bool HasScale => hasScale;

        /// <summary>
        /// Gets configured scale multiplier.
        /// </summary>
        public float Scale => scale;

        /// <summary>
        /// Gets whether an intensity value is set.
        /// </summary>
        public bool HasIntensity => hasIntensity;

        /// <summary>
        /// Gets configured intensity multiplier.
        /// </summary>
        public float Intensity => intensity;

        /// <summary>
        /// Gets whether lifetime override is set.
        /// </summary>
        public bool HasLifetimeOverride => hasLifetimeOverride;

        /// <summary>
        /// Gets override lifetime in seconds.
        /// </summary>
        public float LifetimeOverride => lifetimeOverride;

        /// <summary>
        /// Gets whether target point is set.
        /// </summary>
        public bool HasTargetPoint => hasTargetPoint;

        /// <summary>
        /// Gets configured world-space target point.
        /// </summary>
        public Vector3 TargetPoint => targetPoint;

        /// <summary>
        /// Returns a copy with color override.
        /// </summary>
        /// <param name="value">Color value.</param>
        /// <returns>Updated parameters.</returns>
        public VfxParams WithColor(Color value)
        {
            hasColor = true;
            color = value;
            return this;
        }

        /// <summary>
        /// Returns a copy with scale override.
        /// </summary>
        /// <param name="value">Scale multiplier.</param>
        /// <returns>Updated parameters.</returns>
        public VfxParams WithScale(float value)
        {
            hasScale = true;
            scale = value;
            return this;
        }

        /// <summary>
        /// Returns a copy with intensity override.
        /// </summary>
        /// <param name="value">Intensity multiplier.</param>
        /// <returns>Updated parameters.</returns>
        public VfxParams WithIntensity(float value)
        {
            hasIntensity = true;
            intensity = value;
            return this;
        }

        /// <summary>
        /// Returns a copy with lifetime override.
        /// </summary>
        /// <param name="seconds">Lifetime in seconds.</param>
        /// <returns>Updated parameters.</returns>
        public VfxParams WithLifetimeOverride(float seconds)
        {
            hasLifetimeOverride = true;
            lifetimeOverride = Mathf.Max(0f, seconds);
            return this;
        }

        /// <summary>
        /// Returns a copy with world-space target point override.
        /// </summary>
        /// <param name="value">World-space target position.</param>
        /// <returns>Updated parameters.</returns>
        public VfxParams WithTargetPoint(Vector3 value)
        {
            hasTargetPoint = true;
            targetPoint = value;
            return this;
        }

        /// <summary>
        /// Merges this struct with fallback values.
        /// </summary>
        /// <param name="fallback">Fallback parameters.</param>
        /// <returns>Merged result.</returns>
        public VfxParams Merge(in VfxParams fallback)
        {
            var merged = this;

            if (!merged.hasColor && fallback.hasColor)
            {
                merged.hasColor = true;
                merged.color = fallback.color;
            }

            if (!merged.hasScale && fallback.hasScale)
            {
                merged.hasScale = true;
                merged.scale = fallback.scale;
            }

            if (!merged.hasIntensity && fallback.hasIntensity)
            {
                merged.hasIntensity = true;
                merged.intensity = fallback.intensity;
            }

            if (!merged.hasLifetimeOverride && fallback.hasLifetimeOverride)
            {
                merged.hasLifetimeOverride = true;
                merged.lifetimeOverride = fallback.lifetimeOverride;
            }

            if (!merged.hasTargetPoint && fallback.hasTargetPoint)
            {
                merged.hasTargetPoint = true;
                merged.targetPoint = fallback.targetPoint;
            }

            return merged;
        }
    }
}
