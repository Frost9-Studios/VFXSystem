#if VCONTAINER_AVAILABLE
using Frost9.VFX.Integration.VContainer;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Runtime integration tests for VContainer helper registration.
    /// </summary>
    public class VfxVContainerIntegrationTests
    {
        private IObjectResolver resolver;

        /// <summary>
        /// Cleans container and created pool-manager objects after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            resolver?.Dispose();
            resolver = null;

            DestroyManagersByName("TestVfxPoolManager_Reused");
            DestroyManagersByName("VFXSystem_PoolManager");
            DestroyManagersByName("TestVfxPoolManager_Existing");
        }

        /// <summary>
        /// Verifies registration reuses an already existing pool-manager object by name.
        /// </summary>
        [Test]
        public void RegisterVfx_ReusesExistingPoolManagerByName()
        {
            var existingObject = new GameObject("TestVfxPoolManager_Reused");
            var existingManager = existingObject.AddComponent<VfxPoolManager>();

            var builder = new ContainerBuilder();
            builder.RegisterVfx(poolManagerObjectName: "TestVfxPoolManager_Reused", dontDestroyOnLoad: true);
            resolver = builder.Build();

            var resolvedManager = resolver.Resolve<VfxPoolManager>();
            Assert.AreSame(existingManager, resolvedManager);
            Assert.IsNotNull(resolver.Resolve<IVfxService>());

            var managers = FindManagersByName("TestVfxPoolManager_Reused");
            Assert.AreEqual(1, managers.Length, "Expected exactly one reused pool-manager object.");
        }

        /// <summary>
        /// Verifies default registration keeps the created pool-manager object in DontDestroyOnLoad.
        /// </summary>
        [Test]
        public void RegisterVfx_DefaultDontDestroyOnLoad_IsTrue()
        {
            var builder = new ContainerBuilder();
            builder.RegisterVfx();
            resolver = builder.Build();

            var resolvedManager = resolver.Resolve<VfxPoolManager>();
            Assert.IsNotNull(resolvedManager);
            Assert.AreEqual("DontDestroyOnLoad", resolvedManager.gameObject.scene.name);
            Assert.IsNotNull(resolver.Resolve<IVfxService>());
        }

        /// <summary>
        /// Verifies explicit existing-pool registration resolves the exact supplied manager.
        /// </summary>
        [Test]
        public void RegisterVfxWithExistingPool_ResolvesProvidedPoolManager()
        {
            var existingObject = new GameObject("TestVfxPoolManager_Existing");
            var existingManager = existingObject.AddComponent<VfxPoolManager>();

            var builder = new ContainerBuilder();
            builder.RegisterVfxWithExistingPool(existingManager);
            resolver = builder.Build();

            var resolvedManager = resolver.Resolve<VfxPoolManager>();
            Assert.AreSame(existingManager, resolvedManager);
            Assert.IsNotNull(resolver.Resolve<IVfxService>());
        }

        private static VfxPoolManager[] FindManagersByName(string name)
        {
            var all = Object.FindObjectsByType<VfxPoolManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var matches = new System.Collections.Generic.List<VfxPoolManager>();
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject != null && all[i].gameObject.name == name)
                {
                    matches.Add(all[i]);
                }
            }

            return matches.ToArray();
        }

        private static void DestroyManagersByName(string name)
        {
            var managers = FindManagersByName(name);
            for (var i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null && managers[i].gameObject != null)
                {
                    Object.DestroyImmediate(managers[i].gameObject);
                }
            }
        }
    }
}
#endif
