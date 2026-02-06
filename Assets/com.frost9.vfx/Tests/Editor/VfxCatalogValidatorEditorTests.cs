using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Editor tests for the catalog validator in the editor assembly.
    /// </summary>
    public class VfxCatalogValidatorEditorTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        /// <summary>
        /// Cleans objects created during each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                var obj = createdObjects[i];
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            createdObjects.Clear();
        }

        /// <summary>
        /// Verifies duplicate ids, missing prefab, and invalid pool config are reported as errors.
        /// </summary>
        [Test]
        public void Validate_ReturnsExpectedErrors_ForBrokenCatalog()
        {
            var validPrefab = CreateGameObject("ValidPlayablePrefab");
            validPrefab.AddComponent<PrefabVfxPlayable>();
            validPrefab.SetActive(false);

            var duplicateEntryA = new VfxCatalogEntry("Effects.Duplicate", validPrefab);
            var duplicateEntryB = new VfxCatalogEntry("Effects.Duplicate", validPrefab);
            var missingPrefabEntry = new VfxCatalogEntry("Effects.MissingPrefab", null);
            var invalidPoolEntry = new VfxCatalogEntry("Effects.InvalidPool", validPrefab);
            SetPrivateField(invalidPoolEntry, "initialPoolSize", 4);
            SetPrivateField(invalidPoolEntry, "maxPoolSize", 2);
            SetPrivateField(invalidPoolEntry, "fallbackLifetimeSeconds", -1f);

            var catalog = CreateCatalog(duplicateEntryA, duplicateEntryB, missingPrefabEntry, invalidPoolEntry);
            var result = ValidateCatalog(catalog);

            Assert.GreaterOrEqual(result.ErrorCount, 4, "Expected duplicate id, missing prefab, invalid max/initial, and negative fallback lifetime.");
            Assert.IsTrue(result.ContainsCode("entry.id.duplicate"));
            Assert.IsTrue(result.ContainsCode("entry.prefab.missing"));
            Assert.IsTrue(result.ContainsCode("entry.pool.max_less_than_initial"));
            Assert.IsTrue(result.ContainsCode("entry.lifetime.negative"));
        }

        /// <summary>
        /// Verifies prefab entries without IVfxPlayable are reported as warnings with actionable guidance.
        /// </summary>
        [Test]
        public void Validate_ReturnsWarning_ForMissingPlayable()
        {
            var plainPrefab = CreateGameObject("PlainPrefab_NoPlayable");
            plainPrefab.SetActive(false);

            var catalog = CreateCatalog(new VfxCatalogEntry("Effects.Plain", plainPrefab));
            var result = ValidateCatalog(catalog);

            Assert.AreEqual(0, result.ErrorCount);
            Assert.AreEqual(1, result.WarningCount);
            Assert.IsTrue(result.ContainsCode("entry.prefab.playable_missing"));
        }

        /// <summary>
        /// Verifies a valid single-entry catalog returns zero issues.
        /// </summary>
        [Test]
        public void Validate_ReturnsNoIssues_ForValidCatalog()
        {
            var prefab = CreateGameObject("ValidPrefab");
            prefab.AddComponent<PrefabVfxPlayable>();
            prefab.SetActive(false);

            var catalog = CreateCatalog(new VfxCatalogEntry("Effects.Valid", prefab));
            var result = ValidateCatalog(catalog);

            Assert.AreEqual(0, result.ErrorCount);
            Assert.AreEqual(0, result.WarningCount);
            Assert.AreEqual(0, result.IssueCount);
        }

        private static ValidationResultProxy ValidateCatalog(VfxCatalog catalog)
        {
            var validatorType = GetTypeOrFail("Frost9.VFX.Editor.VfxCatalogValidator, Frost9.VFX.Editor");
            var validateMethod = validatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(validateMethod, "Could not find VfxCatalogValidator.Validate method.");

            var rawResult = validateMethod.Invoke(null, new object[] { catalog });
            Assert.IsNotNull(rawResult, "Validator returned null result.");
            return new ValidationResultProxy(rawResult);
        }

        private VfxCatalog CreateCatalog(params VfxCatalogEntry[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(entries);
            createdObjects.Add(catalog);
            return catalog;
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static Type GetTypeOrFail(string assemblyQualifiedTypeName)
        {
            var type = Type.GetType(assemblyQualifiedTypeName, throwOnError: false);
            Assert.IsNotNull(type, $"Could not resolve type '{assemblyQualifiedTypeName}'.");
            return type;
        }

        private sealed class ValidationResultProxy
        {
            private readonly object rawResult;

            public ValidationResultProxy(object rawResult)
            {
                this.rawResult = rawResult;
            }

            public int ErrorCount => ReadIntProperty("ErrorCount");

            public int WarningCount => ReadIntProperty("WarningCount");

            public int IssueCount => ReadIssues().Count;

            public bool ContainsCode(string code)
            {
                var issues = ReadIssues();
                return issues.Any(issue =>
                {
                    var codeProperty = issue.GetType().GetProperty("Code", BindingFlags.Public | BindingFlags.Instance);
                    return codeProperty != null &&
                           string.Equals(codeProperty.GetValue(issue) as string, code, System.StringComparison.Ordinal);
                });
            }

            private int ReadIntProperty(string propertyName)
            {
                var property = rawResult.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(property, $"Missing property '{propertyName}' on validation result.");
                var value = property.GetValue(rawResult);
                Assert.IsNotNull(value, $"Property '{propertyName}' returned null.");
                return (int)value;
            }

            private List<object> ReadIssues()
            {
                var property = rawResult.GetType().GetProperty("Issues", BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(property, "Missing Issues property on validation result.");
                var value = property.GetValue(rawResult) as System.Collections.IEnumerable;
                Assert.IsNotNull(value, "Issues property is not enumerable.");

                var list = new List<object>();
                foreach (var item in value)
                {
                    if (item != null)
                    {
                        list.Add(item);
                    }
                }

                return list;
            }
        }
    }
}
