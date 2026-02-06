using System;
using UnityEngine;

namespace Frost9.VFX
{
    /// <summary>
    /// Generic prefab-based runner that supports particle systems and renderer parameter overrides.
    /// </summary>
    public class PrefabVfxPlayable : MonoBehaviour, IVfxPlayable
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly int IntensityProperty = Shader.PropertyToID("_Intensity");

        [SerializeField]
        [Min(0f)]
        private float fallbackLifetimeSeconds = 1.25f;

        /// <summary>
        /// Fired when playback naturally finishes.
        /// </summary>
        public event Action<IVfxPlayable> Completed;

        /// <summary>
        /// Gets whether this playable is currently running.
        /// </summary>
        public bool IsPlaying { get; private set; }

        private ParticleSystem[] particleSystems;
        private TrailRenderer[] trailRenderers;
        private LineRenderer[] lineRenderers;
        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseLocalScale;
        private bool hasCachedBaseScale;
        private float completeAtTime;

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

            if (HasParticleSystems())
            {
                var anyAlive = false;
                for (var i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] != null && particleSystems[i].IsAlive(true))
                    {
                        anyAlive = true;
                        break;
                    }
                }

                if (!anyAlive)
                {
                    CompletePlayback();
                    return;
                }
            }

            if (Time.time >= completeAtTime && completeAtTime > 0f)
            {
                CompletePlayback();
            }
        }

        /// <summary>
        /// Resets this instance for reuse.
        /// </summary>
        /// <param name="args">Spawn arguments.</param>
        void IVfxPlayable.Reset(in VfxSpawnArgs args)
        {
            ResetForSpawn(args);
        }

        private void ResetForSpawn(in VfxSpawnArgs args)
        {
            CacheComponents();
            ConfigureTransform(in args);
            ResetRenderableState();
            Apply(args.Parameters);

            var resolvedLifetime = args.Parameters.HasLifetimeOverride
                ? args.Parameters.LifetimeOverride
                : Mathf.Max(args.FallbackLifetimeSeconds, fallbackLifetimeSeconds);

            completeAtTime = resolvedLifetime > 0f ? Time.time + resolvedLifetime : 0f;
            IsPlaying = false;
        }

        /// <summary>
        /// Applies runtime parameters to this instance.
        /// </summary>
        /// <param name="parameters">Parameters to apply.</param>
        public void Apply(in VfxParams parameters)
        {
            if (parameters.HasScale)
            {
                transform.localScale = baseLocalScale * parameters.Scale;
            }

            if (parameters.HasLifetimeOverride)
            {
                completeAtTime = Time.time + Mathf.Max(0f, parameters.LifetimeOverride);
            }

            if (!parameters.HasColor && !parameters.HasIntensity)
            {
                return;
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);

                if (parameters.HasColor)
                {
                    propertyBlock.SetColor(BaseColorProperty, parameters.Color);
                    propertyBlock.SetColor(ColorProperty, parameters.Color);
                }

                if (parameters.HasIntensity)
                {
                    propertyBlock.SetFloat(IntensityProperty, parameters.Intensity);
                }

                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        /// <summary>
        /// Starts playback.
        /// </summary>
        public void Play()
        {
            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Play(true);
            }

            IsPlaying = true;
        }

        /// <summary>
        /// Stops playback.
        /// </summary>
        /// <param name="stopMode">Requested stop behavior.</param>
        public void Stop(VfxStopMode stopMode)
        {
            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                var behavior = stopMode == VfxStopMode.StopEmitting
                    ? ParticleSystemStopBehavior.StopEmitting
                    : ParticleSystemStopBehavior.StopEmittingAndClear;

                particleSystem.Stop(true, behavior);
            }

            for (var i = 0; i < trailRenderers.Length; i++)
            {
                trailRenderers[i]?.Clear();
            }

            for (var i = 0; i < lineRenderers.Length; i++)
            {
                var line = lineRenderers[i];
                if (line != null)
                {
                    line.positionCount = 0;
                }
            }

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

        private void ConfigureTransform(in VfxSpawnArgs args)
        {
            var targetRotation = IsDefaultQuaternion(args.Rotation) ? Quaternion.identity : args.Rotation;
            var parent = args.Parent;

            if (args.AttachMode == AttachMode.AttachToTransform && parent != null)
            {
                transform.SetParent(parent, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }

            if (args.AttachMode == AttachMode.FollowPositionOnly && parent != null)
            {
                transform.SetParent(parent, true);
                transform.position = parent.position;
                transform.rotation = targetRotation;
                return;
            }

            transform.SetParent(null, true);
            transform.position = args.Position;
            transform.rotation = targetRotation;
        }

        private void ResetRenderableState()
        {
            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Clear(true);
            }

            for (var i = 0; i < trailRenderers.Length; i++)
            {
                trailRenderers[i]?.Clear();
            }

            for (var i = 0; i < lineRenderers.Length; i++)
            {
                var line = lineRenderers[i];
                if (line != null)
                {
                    line.positionCount = 0;
                }
            }

            transform.localScale = baseLocalScale;
        }

        private bool HasParticleSystems()
        {
            return particleSystems != null && particleSystems.Length > 0;
        }

        private void CacheComponents()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (particleSystems == null || particleSystems.Length == 0)
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }

            if (trailRenderers == null || trailRenderers.Length == 0)
            {
                trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
            }

            if (lineRenderers == null || lineRenderers.Length == 0)
            {
                lineRenderers = GetComponentsInChildren<LineRenderer>(true);
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }

            if (!hasCachedBaseScale)
            {
                baseLocalScale = transform.localScale;
                hasCachedBaseScale = true;
            }
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
