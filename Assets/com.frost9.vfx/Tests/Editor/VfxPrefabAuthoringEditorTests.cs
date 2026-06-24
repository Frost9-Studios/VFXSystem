using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Frost9.VFX.Editor;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Editor tests for the EnsurePlayable prefab helper.
    /// </summary>
    public class VfxPrefabAuthoringEditorTests
    {
        private const string TempFolder = "Assets/_Project/Temp/VfxPrefabAuthoringTests";
        private readonly List<string> tempAssetPaths = new List<string>();

        /// <summary>
        /// Removes temporary assets.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < tempAssetPaths.Count; i++)
            {
                AssetDatabase.DeleteAsset(tempAssetPaths[i]);
            }

            tempAssetPaths.Clear();
            AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Verifies a prefab without a runner receives exactly one PrefabVfxPlayable, persisted.
        /// </summary>
        [Test]
        public void EnsurePlayable_AddsOnePlayable_WhenMissing_AndPersists()
        {
            var path = CreatePlainPrefab("NoPlayable");

            var result = VfxPrefabAuthoring.EnsurePlayable(path);

            Assert.AreEqual(VfxEnsurePlayableOutcome.Changed, result.Outcome);
            Assert.AreEqual(1, CountPlayables(path));

            // Reopen the saved asset and confirm the component persisted.
            var reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(reloaded.GetComponent<PrefabVfxPlayable>());
        }

        /// <summary>
        /// Verifies a second run is unchanged and does not add a second runner.
        /// </summary>
        [Test]
        public void EnsurePlayable_SecondRun_Unchanged()
        {
            var path = CreatePlainPrefab("Idempotent");

            Assert.AreEqual(VfxEnsurePlayableOutcome.Changed, VfxPrefabAuthoring.EnsurePlayable(path).Outcome);
            Assert.AreEqual(VfxEnsurePlayableOutcome.Unchanged, VfxPrefabAuthoring.EnsurePlayable(path).Outcome);
            Assert.AreEqual(1, CountPlayables(path));
        }

        /// <summary>
        /// Verifies a prefab that already has a runner is not modified.
        /// </summary>
        [Test]
        public void EnsurePlayable_ExistingPlayable_NotModified()
        {
            var path = CreatePrefabWithPlayable("HasPlayable");

            var result = VfxPrefabAuthoring.EnsurePlayable(path);

            Assert.AreEqual(VfxEnsurePlayableOutcome.Unchanged, result.Outcome);
            Assert.AreEqual(1, CountPlayables(path));
        }

        /// <summary>
        /// Verifies the GameObject overload resolves to the asset path.
        /// </summary>
        [Test]
        public void EnsurePlayable_GameObjectOverload_Works()
        {
            var path = CreatePlainPrefab("ByObject");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var result = VfxPrefabAuthoring.EnsurePlayable(asset);

            Assert.AreEqual(VfxEnsurePlayableOutcome.Changed, result.Outcome);
            Assert.AreEqual(1, CountPlayables(path));
        }

        /// <summary>
        /// Verifies unsupported inputs return an actionable error without corruption.
        /// </summary>
        [Test]
        public void EnsurePlayable_UnsupportedInputs_ReturnError()
        {
            Assert.AreEqual(VfxEnsurePlayableOutcome.Error, VfxPrefabAuthoring.EnsurePlayable((GameObject)null).Outcome);
            Assert.AreEqual(VfxEnsurePlayableOutcome.Error, VfxPrefabAuthoring.EnsurePlayable(string.Empty).Outcome);
            Assert.AreEqual(VfxEnsurePlayableOutcome.Error, VfxPrefabAuthoring.EnsurePlayable("Assets/DoesNotExist.prefab").Outcome);

            // A non-prefab asset path is rejected.
            var scriptableObjectPath = CreateScriptableObjectAsset("NotAPrefab");
            Assert.AreEqual(VfxEnsurePlayableOutcome.Error, VfxPrefabAuthoring.EnsurePlayable(scriptableObjectPath).Outcome);
        }

        private int CountPlayables(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(asset, $"No prefab asset at '{path}'.");
            return asset.GetComponentsInChildren<IVfxPlayable>(true).Length;
        }

        private string CreatePlainPrefab(string name)
        {
            EnsureTempFolder();
            var source = new GameObject(name);
            var path = $"{TempFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            tempAssetPaths.Add(path);
            return path;
        }

        private string CreatePrefabWithPlayable(string name)
        {
            EnsureTempFolder();
            var source = new GameObject(name);
            source.AddComponent<PrefabVfxPlayable>();
            var path = $"{TempFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            tempAssetPaths.Add(path);
            return path;
        }

        private string CreateScriptableObjectAsset(string name)
        {
            EnsureTempFolder();
            var catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            var path = $"{TempFolder}/{name}.asset";
            AssetDatabase.CreateAsset(catalog, path);
            AssetDatabase.SaveAssets();
            tempAssetPaths.Add(path);
            return path;
        }

        private static void EnsureTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                return;
            }

            var segments = TempFolder.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
