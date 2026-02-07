using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Runtime tests covering pooling fallback behavior for non-cooperative effect runners.
    /// </summary>
    public class VfxPoolFallbackTests
    {
        private static readonly VfxId DefaultVfxId = new VfxId("Effects.VfxPrefab");

        private VfxService service;
        private GameObject poolManagerObject;
        private GameObject prefab;
        private VfxCatalog catalog;
        private VfxSystemConfiguration configuration;

        /// <summary>
        /// Creates isolated runtime state before each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            WarnOnceLogger.Clear();
            TestNeverCompletingPlayable.ResetStatics();

            prefab = new GameObject("TestPrefab_VfxFallback");
            prefab.AddComponent<TestNeverCompletingPlayable>();
            prefab.SetActive(false);

            catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(new[]
            {
                new VfxCatalogEntry(DefaultVfxId, prefab)
            });

            configuration = ScriptableObject.CreateInstance<VfxSystemConfiguration>();
            configuration.SetDefaultsForRuntime(
                catalog,
                initialPoolSize: 1,
                maxPoolSize: 8,
                maxActive: 16,
                dontDestroyPoolRoot: false,
                configuredPoolRootName: "TestPoolRoot");

            poolManagerObject = new GameObject("TestPoolManager_Fallback");
            var poolManager = poolManagerObject.AddComponent<VfxPoolManager>();
            service = new VfxService(poolManager, catalog, configuration);
        }

        /// <summary>
        /// Cleans up runtime state after each test.
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

            TestNeverCompletingPlayable.ResetStatics();
        }

        /// <summary>
        /// Verifies pool-level fallback auto-release works when runner completion is never fired.
        /// </summary>
        [UnityTest]
        public IEnumerator AutoReleaseFallback_Releases_WhenRunnerNeverCompletes()
        {
            var handle = service.PlayAt(
                DefaultVfxId,
                Vector3.zero,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(0.15f));

            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            yield return new WaitForSeconds(0.35f);

            var stats = service.GetStats();
            Assert.AreEqual(0, stats.TotalActiveInstances, "Fallback auto-release did not return instance to pool.");
            Assert.GreaterOrEqual(stats.TotalPooledInstances, 1, "Expected released instance to be available in pool.");
            Assert.GreaterOrEqual(stats.TotalRecycleCount, 1, "Expected at least one recycle after fallback release.");
        }

        /// <summary>
        /// Verifies externally destroyed active instances are cleaned up from active tracking.
        /// </summary>
        [UnityTest]
        public IEnumerator DestroyedActiveInstance_IsCleanedUp_AndDoesNotLeakActiveCount()
        {
            var handle = service.PlayAt(
                DefaultVfxId,
                Vector3.one,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(5f));

            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            yield return null;
            var spawned = TestNeverCompletingPlayable.GetAnyActiveInstance();
            Assert.IsNotNull(spawned, "Expected spawned playable instance.");

            Object.Destroy(spawned.gameObject);
            yield return null;
            yield return null;

            var stats = service.GetStats();
            Assert.AreEqual(0, stats.TotalActiveInstances, "Destroyed active instance was not cleaned from active tracking.");

            var stopped = service.Stop(handle);
            Assert.IsFalse(stopped, "Stale handle should not stop after underlying instance was destroyed.");
        }

        /// <summary>
        /// Test runner implementation that never fires completion callbacks.
        /// </summary>
        private sealed class TestNeverCompletingPlayable : MonoBehaviour, IVfxPlayable
        {
            private static readonly List<TestNeverCompletingPlayable> Instances = new List<TestNeverCompletingPlayable>();

            /// <summary>
            /// Event required by IVfxPlayable.
            /// </summary>
#pragma warning disable CS0067
            public event System.Action<IVfxPlayable> Completed;
#pragma warning restore CS0067

            /// <summary>
            /// Gets whether this instance is considered playing.
            /// </summary>
            public bool IsPlaying { get; private set; }

            /// <summary>
            /// Resets static test state.
            /// </summary>
            public static void ResetStatics()
            {
                Instances.Clear();
            }

            /// <summary>
            /// Gets any currently active instance.
            /// </summary>
            /// <returns>Active instance or null.</returns>
            public static TestNeverCompletingPlayable GetAnyActiveInstance()
            {
                for (var i = 0; i < Instances.Count; i++)
                {
                    if (Instances[i] != null && Instances[i].gameObject.activeInHierarchy)
                    {
                        return Instances[i];
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
            /// Resets this playable instance.
            /// </summary>
            /// <param name="args">Spawn args.</param>
            void IVfxPlayable.Reset(in VfxSpawnArgs args)
            {
                transform.position = args.Position;
                transform.rotation = args.Rotation;
                transform.localScale = Vector3.one;
                IsPlaying = false;
            }

            /// <summary>
            /// Applies runtime parameters.
            /// </summary>
            /// <param name="parameters">Parameters to apply.</param>
            public void Apply(in VfxParams parameters)
            {
                if (parameters.HasScale)
                {
                    transform.localScale = Vector3.one * parameters.Scale;
                }
            }

            /// <summary>
            /// Starts playback without emitting completion.
            /// </summary>
            public void Play()
            {
                IsPlaying = true;
            }

            /// <summary>
            /// Stops playback without emitting completion.
            /// </summary>
            /// <param name="stopMode">Stop mode.</param>
            public void Stop(VfxStopMode stopMode)
            {
                IsPlaying = false;
            }
        }
    }
}

