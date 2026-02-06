using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Layer 2B tests for handle-based runtime parameter updates.
    /// </summary>
    public class VfxServiceLayer2UpdateTests
    {
        private VfxService service;
        private GameObject poolManagerObject;
        private GameObject prefab;
        private VfxCatalog catalog;
        private VfxSystemConfiguration configuration;

        /// <summary>
        /// Initializes runtime state before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            WarnOnceLogger.Clear();
            InspectableUpdatePlayable.ResetStatics();

            prefab = new GameObject("TestPrefab_L2Update");
            prefab.AddComponent<InspectableUpdatePlayable>();
            prefab.SetActive(false);

            catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(new[]
            {
                new VfxCatalogEntry(VFXRefs.Effects.VfxPrefab, prefab)
            });

            configuration = ScriptableObject.CreateInstance<VfxSystemConfiguration>();
            configuration.SetDefaultsForRuntime(
                catalog,
                initialPoolSize: 1,
                maxPoolSize: 8,
                maxActive: 32,
                dontDestroyPoolRoot: false,
                configuredPoolRootName: "TestPoolRoot_L2Update");

            poolManagerObject = new GameObject("TestPoolManager_L2Update");
            var poolManager = poolManagerObject.AddComponent<VfxPoolManager>();
            service = new VfxService(poolManager, catalog, configuration);
        }

        /// <summary>
        /// Cleans runtime state after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            service?.Dispose();

            if (poolManagerObject != null)
            {
                Object.DestroyImmediate(poolManagerObject);
            }

            if (prefab != null)
            {
                Object.DestroyImmediate(prefab);
            }

            if (catalog != null)
            {
                Object.DestroyImmediate(catalog);
            }

            if (configuration != null)
            {
                Object.DestroyImmediate(configuration);
            }

            InspectableUpdatePlayable.ResetStatics();
        }

        /// <summary>
        /// Verifies TryUpdate applies parameters for active handles.
        /// </summary>
        [Test]
        public void TryUpdate_ValidHandle_UpdatesRunnerState()
        {
            var handle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.zero,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(5f),
                VfxPlayOptions.DefaultGameplay.WithAutoRelease(false));

            Assert.IsTrue(handle.IsValid);

            var updatedParameters = VfxParams.Empty
                .WithColor(new Color(0.1f, 0.8f, 0.3f, 1f))
                .WithIntensity(1.5f)
                .WithScale(1.75f)
                .WithTargetPoint(new Vector3(4f, 0f, -2f));

            var updated = service.TryUpdate(handle, in updatedParameters);
            Assert.IsTrue(updated);

            var activeInstance = InspectableUpdatePlayable.GetAnyActiveInstance();
            Assert.IsNotNull(activeInstance);
            Assert.IsTrue(activeInstance.LastApplied.HasColor);
            Assert.AreEqual(updatedParameters.Color, activeInstance.LastApplied.Color);
            Assert.IsTrue(activeInstance.LastApplied.HasIntensity);
            Assert.AreEqual(updatedParameters.Intensity, activeInstance.LastApplied.Intensity);
            Assert.IsTrue(activeInstance.LastApplied.HasScale);
            Assert.AreEqual(updatedParameters.Scale, activeInstance.LastApplied.Scale);
            Assert.IsTrue(activeInstance.LastApplied.HasTargetPoint);
            Assert.AreEqual(updatedParameters.TargetPoint, activeInstance.LastApplied.TargetPoint);
        }

        /// <summary>
        /// Verifies stale handles are rejected by TryUpdate.
        /// </summary>
        [UnityTest]
        public IEnumerator TryUpdate_StaleHandle_ReturnsFalse()
        {
            var staleHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.zero,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(0.01f));

            Assert.IsTrue(staleHandle.IsValid);

            yield return WaitUntilOrTimeout(
                () => service.GetStats().TotalActiveInstances == 0,
                0.75f,
                "Expected first instance to auto-release before stale update check.");

            var activeHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.one,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(5f),
                VfxPlayOptions.DefaultGameplay.WithAutoRelease(false));

            Assert.IsTrue(activeHandle.IsValid);

            var parameters = VfxParams.Empty.WithTargetPoint(new Vector3(10f, 0f, 0f));
            var staleUpdated = service.TryUpdate(staleHandle, in parameters);
            var activeUpdated = service.TryUpdate(activeHandle, in parameters);

            Assert.IsFalse(staleUpdated);
            Assert.IsTrue(activeUpdated);
        }

        private static IEnumerator WaitUntilOrTimeout(System.Func<bool> predicate, float timeoutSeconds, string timeoutMessage)
        {
            var elapsed = 0f;
            while (!predicate())
            {
                if (elapsed >= timeoutSeconds)
                {
                    Assert.Fail(timeoutMessage);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Test runner that captures applied parameters for assertion.
        /// </summary>
        private sealed class InspectableUpdatePlayable : MonoBehaviour, IVfxPlayable
        {
            private static readonly List<InspectableUpdatePlayable> Instances = new List<InspectableUpdatePlayable>();

            /// <summary>
            /// Event required by IVfxPlayable.
            /// </summary>
#pragma warning disable CS0067
            public event System.Action<IVfxPlayable> Completed;
#pragma warning restore CS0067

            /// <summary>
            /// Gets whether this playable is currently active.
            /// </summary>
            public bool IsPlaying { get; private set; }

            /// <summary>
            /// Gets the most recently applied parameters.
            /// </summary>
            public VfxParams LastApplied { get; private set; }

            /// <summary>
            /// Resets static test state.
            /// </summary>
            public static void ResetStatics()
            {
                Instances.Clear();
            }

            /// <summary>
            /// Gets any active instance for assertions.
            /// </summary>
            /// <returns>Active instance or null.</returns>
            public static InspectableUpdatePlayable GetAnyActiveInstance()
            {
                for (var i = 0; i < Instances.Count; i++)
                {
                    var instance = Instances[i];
                    if (instance != null && instance.gameObject.activeInHierarchy)
                    {
                        return instance;
                    }
                }

                return null;
            }

            private void Awake()
            {
                if (!Instances.Contains(this))
                {
                    Instances.Add(this);
                }
            }

            private void OnDestroy()
            {
                Instances.Remove(this);
            }

            /// <summary>
            /// Resets instance state for pooled reuse.
            /// </summary>
            /// <param name="args">Spawn arguments.</param>
            void IVfxPlayable.Reset(in VfxSpawnArgs args)
            {
                transform.position = args.Position;
                transform.rotation = args.Rotation;
                transform.localScale = Vector3.one;
                LastApplied = args.Parameters;
                IsPlaying = false;
            }

            /// <summary>
            /// Applies runtime parameters.
            /// </summary>
            /// <param name="parameters">Updated values.</param>
            public void Apply(in VfxParams parameters)
            {
                LastApplied = parameters;
                if (parameters.HasScale)
                {
                    transform.localScale = Vector3.one * parameters.Scale;
                }
            }

            /// <summary>
            /// Starts playback.
            /// </summary>
            public void Play()
            {
                IsPlaying = true;
            }

            /// <summary>
            /// Stops playback.
            /// </summary>
            /// <param name="stopMode">Stop behavior.</param>
            public void Stop(VfxStopMode stopMode)
            {
                IsPlaying = false;
            }
        }
    }
}
