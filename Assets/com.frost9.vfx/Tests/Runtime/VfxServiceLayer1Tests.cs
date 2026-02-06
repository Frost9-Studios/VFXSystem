using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Layer 1 regression tests for core VFX runtime behavior.
    /// </summary>
    public class VfxServiceLayer1Tests
    {
        private VfxService service;
        private GameObject poolManagerObject;
        private GameObject prefab;
        private VfxCatalog catalog;
        private VfxSystemConfiguration configuration;

        /// <summary>
        /// Creates isolated runtime state for each test.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            WarnOnceLogger.Clear();

            prefab = new GameObject("TestPrefab_VfxPrefab");
            prefab.AddComponent<PrefabVfxPlayable>();
            prefab.SetActive(false);

            catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(new[]
            {
                new VfxCatalogEntry(VFXRefs.Effects.VfxPrefab, prefab)
            });

            configuration = ScriptableObject.CreateInstance<VfxSystemConfiguration>();
            configuration.SetDefaultsForRuntime(catalog, initialPoolSize: 1, maxPoolSize: 8, maxActive: 32);

            poolManagerObject = new GameObject("TestPoolManager");
            var poolManager = poolManagerObject.AddComponent<VfxPoolManager>();
            service = new VfxService(poolManager, catalog, configuration);
        }

        /// <summary>
        /// Cleans runtime state created by each test.
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
        /// Verifies playing a known id returns a valid handle.
        /// </summary>
        [Test]
        public void PlayAt_ReturnsValidHandle_ForKnownCatalogId()
        {
            var handle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.zero,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(0.25f));

            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);
        }

        /// <summary>
        /// Verifies stale handles cannot stop recycled instances.
        /// </summary>
        [UnityTest]
        public IEnumerator Stop_IgnoresStaleHandle_AfterRecycle()
        {
            var firstHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.zero,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(0.01f));

            Assert.IsTrue(firstHandle.IsValid);

            yield return WaitUntilOrTimeout(() => service.GetStats().TotalActiveInstances == 0, 0.75f, "First effect did not recycle in time.");
            Assert.AreEqual(0, service.GetStats().TotalActiveInstances);

            var secondHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.one,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(1f));

            Assert.IsTrue(secondHandle.IsValid);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            var staleStopResult = service.Stop(firstHandle);
            Assert.IsFalse(staleStopResult);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            var validStopResult = service.Stop(secondHandle);
            Assert.IsTrue(validStopResult);
            Assert.AreEqual(0, service.GetStats().TotalActiveInstances);
        }

        /// <summary>
        /// Verifies StopAll defaults to Gameplay channel scope.
        /// </summary>
        [Test]
        public void StopAll_DefaultScope_IsGameplayOnly()
        {
            var gameplayHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.zero,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(2f));

            var uiHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.right,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(2f),
                VfxPlayOptions.DefaultGameplay.WithChannel(VfxChannel.UI));

            Assert.IsTrue(gameplayHandle.IsValid);
            Assert.IsTrue(uiHandle.IsValid);
            Assert.AreEqual(2, service.GetStats().TotalActiveInstances);

            var stoppedByDefault = service.StopAll();
            Assert.AreEqual(1, stoppedByDefault);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            var gameplayStopAfterDefault = service.Stop(gameplayHandle);
            Assert.IsFalse(gameplayStopAfterDefault, "Gameplay handle should already be stopped by default StopAll scope.");

            var uiStopAfterDefault = service.Stop(uiHandle);
            Assert.IsTrue(uiStopAfterDefault, "UI handle should remain active after default StopAll and be stoppable explicitly.");
            Assert.AreEqual(0, service.GetStats().TotalActiveInstances);

            var stoppedGlobal = service.StopAll(VfxStopFilter.Global);
            Assert.AreEqual(0, stoppedGlobal);
            Assert.AreEqual(0, service.GetStats().TotalActiveInstances);
        }

        /// <summary>
        /// Verifies per-id hard cap enforcement when pool expansion is disabled.
        /// </summary>
        [Test]
        public void PlayAt_RespectsHardCap_WhenExpansionDisabled()
        {
            RecreateServiceWithEntry(
                BuildEntry(
                    prefab,
                    initialPoolSize: 1,
                    maxPoolSize: 1,
                    allowPoolExpansion: false,
                    autoReleaseByDefault: true,
                    fallbackLifetimeSeconds: 2f),
                maxActive: 32);

            var firstHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.zero,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(2f));

            var secondHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                Vector3.right,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(2f));

            Assert.IsTrue(firstHandle.IsValid);
            Assert.IsFalse(secondHandle.IsValid, "Second play should fail when hard cap is reached and expansion is disabled.");
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            var stopped = service.Stop(firstHandle);
            Assert.IsTrue(stopped);
        }

        /// <summary>
        /// Verifies pooled instance reset applies new spawn position on reuse.
        /// </summary>
        [UnityTest]
        public IEnumerator ReusedInstance_ResetsToNewSpawnPosition()
        {
            RecreateServiceWithEntry(
                BuildEntry(
                    prefab,
                    initialPoolSize: 1,
                    maxPoolSize: 1,
                    allowPoolExpansion: false,
                    autoReleaseByDefault: true,
                    fallbackLifetimeSeconds: 0.05f),
                maxActive: 16);

            var positionA = new Vector3(1f, 0f, 1f);
            var positionB = new Vector3(-3f, 0f, 2f);

            var firstHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                positionA,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(0.05f));

            Assert.IsTrue(firstHandle.IsValid);

            yield return WaitUntilOrTimeout(() => service.GetStats().TotalActiveInstances == 0, 0.75f, "First instance did not recycle in time.");

            var pooledInstance = Object.FindFirstObjectByType<PrefabVfxPlayable>(FindObjectsInactive.Include);
            Assert.IsNotNull(pooledInstance, "Expected at least one pooled PrefabVfxPlayable instance.");

            var secondHandle = service.PlayAt(
                VFXRefs.Effects.VfxPrefab,
                positionB,
                Quaternion.identity,
                VfxParams.Empty.WithLifetimeOverride(1f));

            Assert.IsTrue(secondHandle.IsValid);
            yield return null;

            var activeInstance = Object.FindFirstObjectByType<PrefabVfxPlayable>(FindObjectsInactive.Exclude);
            Assert.IsNotNull(activeInstance, "Expected active PrefabVfxPlayable instance after second play.");
            Assert.AreSame(pooledInstance, activeInstance, "Expected pool to reuse same instance under hard cap.");
            Assert.LessOrEqual(Vector3.Distance(activeInstance.transform.position, positionB), 0.001f, "Reused instance did not reset to new spawn position.");
        }

        /// <summary>
        /// Verifies unknown ids fail safely and return invalid handles.
        /// </summary>
        [Test]
        public void PlayAt_ReturnsInvalidHandle_ForUnknownId()
        {
            var handle = service.PlayAt(new VfxId("Effects.DoesNotExist"), Vector3.zero);
            Assert.IsFalse(handle.IsValid);
        }

        private void RecreateServiceWithEntry(VfxCatalogEntry entry, int maxActive)
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
            configuration.SetDefaultsForRuntime(catalog, initialPoolSize: 1, maxPoolSize: 8, maxActive: maxActive);

            poolManagerObject = new GameObject("TestPoolManager_Recreated");
            var poolManager = poolManagerObject.AddComponent<VfxPoolManager>();
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
            var entry = new VfxCatalogEntry(VFXRefs.Effects.VfxPrefab, entryPrefab);
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
    }
}
