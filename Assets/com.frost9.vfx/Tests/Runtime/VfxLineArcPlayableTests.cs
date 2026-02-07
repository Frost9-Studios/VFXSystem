using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Layer 2C tests for line-runner playback, update, stop, and pooling reuse.
    /// </summary>
    public class VfxLineArcPlayableTests
    {
        private static readonly VfxId DefaultVfxId = new VfxId("Effects.VfxPrefab");

        private VfxService service;
        private GameObject poolManagerObject;
        private GameObject prefab;
        private VfxCatalog catalog;
        private VfxSystemConfiguration configuration;

        /// <summary>
        /// Creates test runtime state.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            WarnOnceLogger.Clear();

            prefab = new GameObject("TestPrefab_LineArc");
            prefab.AddComponent<LineRenderer>();
            prefab.AddComponent<LineArcVfxPlayable>();
            prefab.SetActive(false);

            catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(new[]
            {
                BuildEntry(
                    prefab,
                    initialPoolSize: 1,
                    maxPoolSize: 1,
                    allowPoolExpansion: false,
                    autoReleaseByDefault: true,
                    fallbackLifetimeSeconds: 10f)
            });

            configuration = ScriptableObject.CreateInstance<VfxSystemConfiguration>();
            configuration.SetDefaultsForRuntime(
                catalog,
                initialPoolSize: 1,
                maxPoolSize: 4,
                maxActive: 32,
                dontDestroyPoolRoot: false,
                configuredPoolRootName: "TestPoolRoot_LineArc");

            poolManagerObject = new GameObject("TestPoolManager_LineArc");
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
        /// Verifies line runner supports play, update, stop, and pooled reuse.
        /// </summary>
        [UnityTest]
        public IEnumerator LineRunner_PlayUpdateStop_WorksWithPooling()
        {
            var startA = new Vector3(0f, 1f, 0f);
            var targetA = new Vector3(2f, 1f, 4f);
            var startB = new Vector3(-3f, 2f, -1f);
            var targetB = new Vector3(5f, 2f, -2f);
            var targetC = new Vector3(8f, 3f, 0f);

            var firstHandle = service.PlayAt(
                DefaultVfxId,
                startA,
                Quaternion.identity,
                VfxParams.Empty.WithTargetPoint(targetA).WithLifetimeOverride(10f),
                VfxPlayOptions.DefaultGameplay.WithAutoRelease(false));

            Assert.IsTrue(firstHandle.IsValid);
            yield return null;

            var firstRunner = Object.FindFirstObjectByType<LineArcVfxPlayable>(FindObjectsInactive.Exclude);
            Assert.IsNotNull(firstRunner, "Expected active line runner instance.");

            var firstLine = firstRunner.GetComponent<LineRenderer>();
            Assert.IsNotNull(firstLine);
            Assert.AreEqual(2, firstLine.positionCount);
            Assert.LessOrEqual(Vector3.Distance(firstLine.GetPosition(0), startA), 0.001f);
            Assert.LessOrEqual(Vector3.Distance(firstLine.GetPosition(1), targetA), 0.001f);

            var updated = service.TryUpdate(
                firstHandle,
                VfxParams.Empty
                    .WithTargetPoint(targetB)
                    .WithScale(0.2f)
                    .WithColor(new Color(0.4f, 0.7f, 1f, 1f))
                    .WithIntensity(1.2f));

            Assert.IsTrue(updated);
            yield return null;

            Assert.AreEqual(2, firstLine.positionCount);
            Assert.LessOrEqual(Vector3.Distance(firstLine.GetPosition(1), targetB), 0.001f);
            Assert.AreEqual(0.2f, firstLine.widthMultiplier, 0.0001f);

            var firstStopped = service.Stop(firstHandle);
            Assert.IsTrue(firstStopped);
            Assert.AreEqual(0, service.GetStats().TotalActiveInstances);

            var secondHandle = service.PlayAt(
                DefaultVfxId,
                startB,
                Quaternion.identity,
                VfxParams.Empty.WithTargetPoint(targetC).WithLifetimeOverride(10f),
                VfxPlayOptions.DefaultGameplay.WithAutoRelease(false));

            Assert.IsTrue(secondHandle.IsValid);
            yield return null;

            var secondRunner = Object.FindFirstObjectByType<LineArcVfxPlayable>(FindObjectsInactive.Exclude);
            Assert.IsNotNull(secondRunner);
            Assert.AreSame(firstRunner, secondRunner, "Expected pooled runner reuse under hard-cap config.");

            var secondLine = secondRunner.GetComponent<LineRenderer>();
            Assert.IsNotNull(secondLine);
            Assert.AreEqual(2, secondLine.positionCount);
            Assert.LessOrEqual(Vector3.Distance(secondLine.GetPosition(0), startB), 0.001f);
            Assert.LessOrEqual(Vector3.Distance(secondLine.GetPosition(1), targetC), 0.001f);
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

