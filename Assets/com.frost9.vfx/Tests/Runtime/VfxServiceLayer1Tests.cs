using System.Collections;
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

            yield return new WaitForSeconds(0.05f);
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

            var stoppedGlobal = service.StopAll(VfxStopFilter.Global);
            Assert.AreEqual(1, stoppedGlobal);
            Assert.AreEqual(0, service.GetStats().TotalActiveInstances);
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
    }
}
