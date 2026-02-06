using UnityEngine;

namespace Project.VfxSandbox
{
    /// <summary>
    /// Simple orbit motion helper for visualizing PlayOn attachment behavior.
    /// </summary>
    public class VfxSandboxOrbitTarget : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Orbit center in world space.")]
        private Vector3 center = new Vector3(0f, 1f, 3f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("Orbit radius in world units.")]
        private float radius = 2f;

        [SerializeField]
        [Tooltip("Angular speed in degrees per second.")]
        private float speedDegrees = 45f;

        private float currentAngleDegrees;

        /// <summary>
        /// Configures default orbit values.
        /// </summary>
        /// <param name="radius">Orbit radius.</param>
        /// <param name="speedDegrees">Angular speed in degrees per second.</param>
        /// <param name="center">Center point.</param>
        public void SetDefaults(float radius, float speedDegrees, Vector3 center)
        {
            this.radius = radius;
            this.speedDegrees = speedDegrees;
            this.center = center;
        }

        /// <summary>
        /// Updates target position each frame.
        /// </summary>
        private void Update()
        {
            currentAngleDegrees += speedDegrees * Time.deltaTime;
            var radians = currentAngleDegrees * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * radius;
            transform.position = center + offset;
        }
    }
}
