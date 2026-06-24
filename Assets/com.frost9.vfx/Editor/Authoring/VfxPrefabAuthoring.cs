using System;
using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Editor helper that ensures a prefab contains a valid <see cref="IVfxPlayable"/> runner. The
    /// package owns this because it owns the runner contract and the default runner; it knows nothing
    /// about any project's VFX folder or id policy.
    /// </summary>
    public static class VfxPrefabAuthoring
    {
        /// <summary>
        /// Ensures a prefab asset contains a valid runner, adding <see cref="PrefabVfxPlayable"/> when
        /// none exists. Idempotent and safe; preserves existing contents and overrides.
        /// </summary>
        /// <param name="prefabAsset">Prefab asset root.</param>
        /// <returns>Structured result.</returns>
        public static VfxEnsurePlayableResult EnsurePlayable(GameObject prefabAsset)
        {
            if (prefabAsset == null)
            {
                return Error(string.Empty, "Prefab asset is null.");
            }

            var path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path))
            {
                return Error(string.Empty, "Object is not a saved prefab asset.");
            }

            return EnsurePlayable(path);
        }

        /// <summary>
        /// Ensures the prefab at a path contains a valid runner.
        /// </summary>
        /// <param name="assetPath">Prefab asset path.</param>
        /// <returns>Structured result.</returns>
        public static VfxEnsurePlayableResult EnsurePlayable(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return Error(assetPath, "Asset path is empty.");
            }

            if (!assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return Error(assetPath, $"'{assetPath}' is not a .prefab asset.");
            }

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                return Error(assetPath, $"No prefab asset found at '{assetPath}'.");
            }

            var assetType = PrefabUtility.GetPrefabAssetType(asset);
            switch (assetType)
            {
                case PrefabAssetType.Model:
                    return Error(assetPath, $"'{assetPath}' is a model prefab and cannot host a runner. Create a regular prefab or variant.");
                case PrefabAssetType.MissingAsset:
                    return Error(assetPath, $"'{assetPath}' has missing prefab data.");
                case PrefabAssetType.NotAPrefab:
                    return Error(assetPath, $"'{assetPath}' is not a prefab asset.");
            }

            if (PrefabUtility.IsPartOfImmutablePrefab(asset))
            {
                return Error(assetPath, $"'{assetPath}' is an immutable prefab and cannot be modified.");
            }

            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(assetPath);
                if (contents == null)
                {
                    return Error(assetPath, $"Could not open prefab contents for '{assetPath}'.");
                }

                if (HasPlayable(contents))
                {
                    return new VfxEnsurePlayableResult(
                        VfxEnsurePlayableOutcome.Unchanged,
                        assetPath,
                        $"'{assetPath}' already contains a valid IVfxPlayable.");
                }

                contents.AddComponent<PrefabVfxPlayable>();
                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);

                return new VfxEnsurePlayableResult(
                    VfxEnsurePlayableOutcome.Changed,
                    assetPath,
                    $"Added {nameof(PrefabVfxPlayable)} to '{assetPath}'.");
            }
            catch (Exception exception)
            {
                return Error(assetPath, $"Failed to ensure playable for '{assetPath}': {exception.Message}");
            }
            finally
            {
                if (contents != null)
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }

        private static bool HasPlayable(GameObject root)
        {
            return root.GetComponent<IVfxPlayable>() != null ||
                   root.GetComponentInChildren<IVfxPlayable>(true) != null;
        }

        private static VfxEnsurePlayableResult Error(string assetPath, string message)
        {
            return new VfxEnsurePlayableResult(VfxEnsurePlayableOutcome.Error, assetPath, message);
        }
    }
}
