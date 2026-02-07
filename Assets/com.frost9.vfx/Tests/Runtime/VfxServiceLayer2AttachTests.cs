using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Layer 2A tests for target attachment semantics and lifecycle safety.
    /// </summary>
    public class VfxServiceLayer2AttachTests
    {
        private static readonly VfxId DefaultVfxId = new VfxId("Effects.VfxPrefab");

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

            prefab = new GameObject("TestPrefab_L2Attach");
            prefab.AddComponent<PrefabVfxPlayable>();
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
                maxActive: 32,
                dontDestroyPoolRoot: false,
                configuredPoolRootName: "TestPoolRoot_L2Attach");

            poolManagerObject = new GameObject("TestPoolManager_L2Attach");
            var poolManager = poolManagerObject.AddComponent<VfxPoolManager>();
            service = new VfxService(poolManager, catalog, configuration);
        }

        /// <summary>
        /// Cleans test runtime state.
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
        /// Verifies PlayOn fails safely for null or already destroyed targets.
        /// </summary>
        [Test]
        public void PlayOn_NullOrDestroyedTarget_ReturnsInvalidHandle()
        {
            var nullTargetHandle = service.PlayOn(
                DefaultVfxId,
                null,
                AttachMode.FollowTransform);

            Assert.IsFalse(nullTargetHandle.IsValid);

            var destroyedTarget = new GameObject("DestroyedTarget");
            Object.DestroyImmediate(destroyedTarget);

            var destroyedTargetHandle = service.PlayOn(
                DefaultVfxId,
                destroyedTarget,
                AttachMode.FollowTransform);

            Assert.IsFalse(destroyedTargetHandle.IsValid);
        }

        /// <summary>
        /// Verifies FollowPositionOnly tracks position while preserving independent rotation.
        /// </summary>
        [UnityTest]
        public IEnumerator PlayOn_FollowPositionOnly_TracksTargetMovement()
        {
            var target = new GameObject("FollowPositionTarget");
            target.transform.position = new Vector3(1f, 2f, 3f);
            target.transform.rotation = Quaternion.Euler(0f, 35f, 0f);

            var handle = service.PlayOn(
                DefaultVfxId,
                target,
                AttachMode.FollowPositionOnly,
                VfxParams.Empty.WithLifetimeOverride(5f),
                VfxPlayOptions.DefaultGameplay.WithAutoRelease(false));

            Assert.IsTrue(handle.IsValid);
            yield return null;

            var playable = Object.FindFirstObjectByType<PrefabVfxPlayable>(FindObjectsInactive.Exclude);
            Assert.IsNotNull(playable, "Expected active playable instance.");
            var initialRotation = playable.transform.rotation;

            target.transform.position = new Vector3(7f, -1f, 4f);
            target.transform.rotation = Quaternion.Euler(0f, 125f, 0f);
            yield return null;

            Assert.LessOrEqual(Vector3.Distance(playable.transform.position, target.transform.position), 0.001f);
            Assert.LessOrEqual(Quaternion.Angle(playable.transform.rotation, initialRotation), 0.001f);

            Object.DestroyImmediate(target);
        }

        /// <summary>
        /// Verifies attached instances are released safely when targets are destroyed.
        /// </summary>
        [UnityTest]
        public IEnumerator PlayOn_TargetDestroyed_ReleasesInstanceAndInvalidatesHandle()
        {
            var target = new GameObject("DestroyAttachTarget");
            var handle = service.PlayOn(
                DefaultVfxId,
                target,
                AttachMode.FollowTransform,
                VfxParams.Empty.WithLifetimeOverride(5f),
                VfxPlayOptions.DefaultGameplay.WithAutoRelease(false));

            Assert.IsTrue(handle.IsValid);
            Assert.AreEqual(1, service.GetStats().TotalActiveInstances);

            Object.Destroy(target);
            yield return null;
            yield return null;

            var stats = service.GetStats();
            Assert.AreEqual(0, stats.TotalActiveInstances);
            Assert.GreaterOrEqual(stats.TotalPooledInstances, 1, "Expected released instance to return to pool.");
            Assert.IsFalse(service.Stop(handle), "Handle should be stale after target-destroy release.");
        }
    }
}

