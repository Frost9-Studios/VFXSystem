using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// LineRenderer-based playable for straight line previews between origin and target point.
    /// </summary>
    public sealed class LineArcVfxPlayable : MonoBehaviour, IVfxPlayable
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int IntensityProperty = Shader.PropertyToID("_Intensity");

        [SerializeField]
        [Min(0f)]
        private float fallbackLifetimeSeconds = 0f;

        [SerializeField]
        [Min(0.001f)]
        private float defaultWidth = 0.05f;

        /// <summary>
        /// Fired when playback naturally completes.
        /// </summary>
        public event Action<IVfxPlayable> Completed;

        /// <summary>
        /// Gets whether this playable is currently running.
        /// </summary>
        public bool IsPlaying { get; private set; }

        private LineRenderer lineRenderer;
        private MaterialPropertyBlock propertyBlock;
        private float completeAtTime;
        private Vector3 startPoint;
        private Vector3 endPoint;
        private Color color;
        private float intensity;
        private bool followStartFromTransform;

        private void Awake()
        {
            CacheComponents();
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (followStartFromTransform)
            {
                startPoint = transform.position;
                lineRenderer.SetPosition(0, startPoint);
            }

            if (completeAtTime > 0f && Time.time >= completeAtTime)
            {
                CompletePlayback();
            }
        }

        /// <summary>
        /// Resets this instance for pooled reuse.
        /// </summary>
        /// <param name="args">Spawn arguments.</param>
        void IVfxPlayable.Reset(in VfxSpawnArgs args)
        {
            ResetForSpawn(args);
        }

        /// <summary>
        /// Applies runtime update parameters.
        /// </summary>
        /// <param name="parameters">Parameters to apply.</param>
        public void Apply(in VfxParams parameters)
        {
            if (parameters.HasTargetPoint)
            {
                endPoint = parameters.TargetPoint;
                lineRenderer.SetPosition(1, endPoint);
            }

            if (parameters.HasScale)
            {
                lineRenderer.widthMultiplier = Mathf.Max(0.001f, parameters.Scale);
            }

            if (parameters.HasColor)
            {
                color = parameters.Color;
            }

            if (parameters.HasIntensity)
            {
                intensity = Mathf.Max(0f, parameters.Intensity);
            }

            if (parameters.HasColor || parameters.HasIntensity)
            {
                ApplyMaterialColor();
            }

            if (parameters.HasLifetimeOverride)
            {
                var seconds = Mathf.Max(0f, parameters.LifetimeOverride);
                completeAtTime = seconds > 0f ? Time.time + seconds : 0f;
            }
        }

        /// <summary>
        /// Starts playback.
        /// </summary>
        public void Play()
        {
            lineRenderer.enabled = true;
            if (lineRenderer.positionCount != 2)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, startPoint);
                lineRenderer.SetPosition(1, endPoint);
            }

            IsPlaying = true;
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        /// <param name="stopMode">Stop behavior.</param>
        public void Stop(VfxStopMode stopMode)
        {
            IsPlaying = false;
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
        }

        private void ResetForSpawn(in VfxSpawnArgs args)
        {
            CacheComponents();

            var resolvedRotation = IsDefaultQuaternion(args.Rotation) ? Quaternion.identity : args.Rotation;
            transform.SetParent(null, true);
            transform.position = args.Position;
            transform.rotation = resolvedRotation;

            followStartFromTransform = args.AttachMode != AttachMode.WorldLocked;
            startPoint = args.Position;
            endPoint = args.Parameters.HasTargetPoint ? args.Parameters.TargetPoint : args.Position;
            color = args.Parameters.HasColor ? args.Parameters.Color : Color.white;
            intensity = args.Parameters.HasIntensity ? Mathf.Max(0f, args.Parameters.Intensity) : 1f;

            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
            lineRenderer.widthMultiplier = args.Parameters.HasScale
                ? Mathf.Max(0.001f, args.Parameters.Scale)
                : Mathf.Max(0.001f, defaultWidth);

            ApplyMaterialColor();

            var resolvedLifetime = args.Parameters.HasLifetimeOverride
                ? Mathf.Max(0f, args.Parameters.LifetimeOverride)
                : Mathf.Max(0f, Mathf.Max(args.FallbackLifetimeSeconds, fallbackLifetimeSeconds));
            completeAtTime = resolvedLifetime > 0f ? Time.time + resolvedLifetime : 0f;

            IsPlaying = false;
        }

        private void CompletePlayback()
        {
            if (!IsPlaying)
            {
                return;
            }

            IsPlaying = false;
            Completed?.Invoke(this);
        }

        private void CacheComponents()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void ApplyMaterialColor()
        {
            var resolvedColor = color * intensity;
            lineRenderer.startColor = resolvedColor;
            lineRenderer.endColor = resolvedColor;

            lineRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorProperty, resolvedColor);
            propertyBlock.SetColor(ColorProperty, resolvedColor);
            propertyBlock.SetFloat(IntensityProperty, intensity);
            lineRenderer.SetPropertyBlock(propertyBlock);
        }

        private static bool IsDefaultQuaternion(Quaternion quaternion)
        {
            return quaternion.x == 0f &&
                   quaternion.y == 0f &&
                   quaternion.z == 0f &&
                   quaternion.w == 0f;
        }
    }
}
