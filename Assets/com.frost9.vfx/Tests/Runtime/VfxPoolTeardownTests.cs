using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Regression tests for Play Mode exit / teardown safety and for retiring instances whose
    /// backing GameObject or runner has been destroyed without leaking pool capacity.
    /// </summary>
    public class VfxPoolTeardownTests
    {
        private static readonly VfxId DefaultVfxId = new VfxId("Effects.VfxPrefab");

        private VfxService service;
        private VfxPoolManager poolManager;
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

            prefab = new GameObject("TestPrefab_Teardown");
            prefab.AddComponent<PrefabVfxPlayable>();
            prefab.SetActive(false);

            BuildService(
                BuildEntry(
                    prefab,
                    initialPoolSize: 1,
                    maxPoolSize: 8,
                    allowPoolExpansion: true,
                    autoReleaseByDefault: true,
                    fallbackLifetimeSeconds: 30f),
                maxActive: 32);
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
        }

        /// <summary>
        /// Reproduces the reported defect: pooled GameObjects destroyed before VfxService.Dispose
        /// runs must not raise MissingReferenceException during teardown.
        /// </summary>
        [Test]
        public void Dispose_AfterPooledGameObjectsDestroyed_DoesNotThrow()
        {
            var handle = PlayPersistent();
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            DestroyActiveCloneGameObjectsImmediate();

            Assert.DoesNotThrow(() => service.Dispose());
        }

        /// <summary>
        /// Deterministic destroyed-GameObject teardown: ClearAll must complete and zero active state
        /// even when the pooled GameObjects were destroyed first.
        /// </summary>
        [Test]
        public void ClearAll_AfterPooledGameObjectsDestroyed_ZeroesActive()
        {
            PlayPersistent();
            PlayPersistent();
            Assert.AreEqual(2, service.GetStats().TotalActiveInstances);

            DestroyActiveCloneGameObjectsImmediate();

            Assert.DoesNotThrow(() => poolManager.ClearAll());

            var stats = poolManager.GetStats();
            Assert.AreEqual(0, stats.TotalActiveInstances);
            Assert.AreEqual(0, stats.TotalPooledInstances);
        }

        /// <summary>
        /// Repeated ClearAll, disposal and destruction must all be harmless.
        /// </summary>
        [Test]
        public void RepeatedClearAllAndDispose_IsHarmless()
        {
            PlayPersistent();

            Assert.DoesNotThrow(() => poolManager.ClearAll());
            Assert.DoesNotThrow(() => poolManager.ClearAll());
            Assert.DoesNotThrow(() => service.Dispose());
            Assert.DoesNotThrow(() => service.Dispose());
        }

        /// <summary>
        /// Runner component destroyed while its GameObject is still alive: the instance must be
        /// retired exactly once and the orphan GameObject destroyed (no leak), with no exception.
        /// </summary>
        [UnityTest]
        public IEnumerator RunnerDestroyedGameObjectAlive_Retired_NoLeakNoException()
        {
            var handle = PlayPersistent();
            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            var clone = GetActiveClone();
            Assert.IsNotNull(clone, "Expected one pooled clone.");
            var cloneObject = clone.gameObject;

            // Destroy only the runner component; the GameObject stays alive.
            Object.DestroyImmediate(clone);

            // Let the manager's Update detect and retire it.
            yield return null;
            yield return null;

            var stats = service.GetStats();
            Assert.AreEqual(0, stats.TotalActiveInstances, "Retired instance still counted active.");
            Assert.IsTrue(cloneObject == null, "Orphan GameObject was not destroyed (leak).");
            Assert.IsFalse(service.Stop(handle), "Stale handle should not stop a retired instance.");
        }

        /// <summary>
        /// Filling a non-expanding pool to its hard cap, destroying one active GameObject, then
        /// proving a replacement can spawn and statistics are correct.
        /// </summary>
        [UnityTest]
        public IEnumerator HardCap_DestroyActiveGameObject_AllowsRespawn_StatsCorrect()
        {
            RebuildHardCappedService(maxInstances: 2);

            var first = PlayPersistent();
            var second = PlayPersistent();
            var overCap = PlayPersistent();

            Assert.IsTrue(first.IsValid);
            Assert.IsTrue(second.IsValid);
            Assert.IsFalse(overCap.IsValid, "Third play should fail at the hard cap.");
            Assert.AreEqual(2, service.GetStats().TotalActiveInstances);

            // Externally destroy one active GameObject.
            var clone = GetActiveClone();
            Assert.IsNotNull(clone);
            Object.Destroy(clone.gameObject);
            yield return null;
            yield return null;

            Assert.AreEqual(1, service.GetStats().TotalActiveInstances, "Cleanup did not free active slot.");

            // Capacity must be freed: a replacement spawns.
            var replacement = PlayPersistent();
            Assert.IsTrue(replacement.IsValid, "Replacement failed to spawn after destroyed GameObject.");

            AssertStatsCoherent(expectedActive: 2);
        }

        /// <summary>
        /// Same as above but the runner component is destroyed while its GameObject remains alive.
        /// </summary>
        [UnityTest]
        public IEnumerator HardCap_DestroyRunnerGameObjectAlive_AllowsRespawn_StatsCorrect()
        {
            RebuildHardCappedService(maxInstances: 2);

            PlayPersistent();
            PlayPersistent();
            Assert.AreEqual(2, service.GetStats().TotalActiveInstances);

            var clone = GetActiveClone();
            Assert.IsNotNull(clone);
            var cloneObject = clone.gameObject;
            Object.DestroyImmediate(clone);
            yield return null;
            yield return null;

            Assert.AreEqual(1, service.GetStats().TotalActiveInstances, "Cleanup did not free active slot.");
            Assert.IsTrue(cloneObject == null, "Orphan GameObject was not destroyed (leak).");

            var replacement = PlayPersistent();
            Assert.IsTrue(replacement.IsValid, "Replacement failed to spawn after destroyed runner.");

            AssertStatsCoherent(expectedActive: 2);
        }

        private VfxHandle PlayPersistent()
        {
            return service.PlayAt(
                DefaultVfxId,
                Vector3.zero,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(30f),
                VfxPlayOptions.DefaultGameplay.WithAutoRelease(false));
        }

        private void AssertStatsCoherent(int expectedActive)
        {
            var stats = service.GetStats();
            Assert.AreEqual(expectedActive, stats.TotalActiveInstances, "Unexpected active count.");
            Assert.AreEqual(
                stats.TotalActiveInstances + stats.TotalPooledInstances,
                stats.TotalCreatedInstances,
                "Created should equal active + pooled (no phantom instances).");
        }

        private PrefabVfxPlayable GetActiveClone()
        {
            // Active instances detach from the pool root (PrefabVfxPlayable re-parents to null on
            // play), so search the scene for an active runner that is not the inactive prefab template.
            var clones = Object.FindObjectsByType<PrefabVfxPlayable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < clones.Length; i++)
            {
                if (clones[i] != null && clones[i].gameObject != prefab)
                {
                    return clones[i];
                }
            }

            return null;
        }

        private void DestroyActiveCloneGameObjectsImmediate()
        {
            var clones = Object.FindObjectsByType<PrefabVfxPlayable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (var i = 0; i < clones.Length; i++)
            {
                if (clones[i] != null && clones[i].gameObject != prefab)
                {
                    Object.DestroyImmediate(clones[i].gameObject);
                }
            }
        }

        private void RebuildHardCappedService(int maxInstances)
        {
            BuildService(
                BuildEntry(
                    prefab,
                    initialPoolSize: maxInstances,
                    maxPoolSize: maxInstances,
                    allowPoolExpansion: false,
                    autoReleaseByDefault: false,
                    fallbackLifetimeSeconds: 30f),
                maxActive: 32);
        }

        private void BuildService(VfxCatalogEntry entry, int maxActive)
        {
            service?.Dispose();
            if (poolManagerObject != null)
            {
                Object.DestroyImmediate(poolManagerObject);
            }

            if (catalog != null)
            {
                Object.DestroyImmediate(catalog);
            }

            if (configuration != null)
            {
                Object.DestroyImmediate(configuration);
            }

            catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(new[] { entry });

            configuration = ScriptableObject.CreateInstance<VfxSystemConfiguration>();
            configuration.SetDefaultsForRuntime(
                catalog,
                initialPoolSize: 1,
                maxPoolSize: 8,
                maxActive: maxActive,
                dontDestroyPoolRoot: false,
                configuredPoolRootName: "TestPoolRoot_Teardown");

            poolManagerObject = new GameObject("TestPoolManager_Teardown");
            poolManager = poolManagerObject.AddComponent<VfxPoolManager>();
            service = new VfxService(poolManager, catalog, configuration);
        }

        private static VfxCatalogEntry BuildEntry(
            GameObject entryPrefab,
            int initialPoolSize,
            int maxPoolSize,
            bool allowPoolExpansion,
            bool autoReleaseByDefault,
            float fallbackLifetimeSeconds)
        {
            var entry = new VfxCatalogEntry(DefaultVfxId, entryPrefab);
            SetPrivateField(entry, "initialPoolSize", initialPoolSize);
            SetPrivateField(entry, "maxPoolSize", maxPoolSize);
            SetPrivateField(entry, "allowPoolExpansion", allowPoolExpansion);
            SetPrivateField(entry, "autoReleaseByDefault", autoReleaseByDefault);
            SetPrivateField(entry, "fallbackLifetimeSeconds", fallbackLifetimeSeconds);
            return entry;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
