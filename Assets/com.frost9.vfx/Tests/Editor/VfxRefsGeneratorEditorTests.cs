using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Editor tests for deterministic refs generation in the editor assembly.
    /// </summary>
    public class VfxRefsGeneratorEditorTests
    {
        private const string TempFolder = "Assets/_Project/Temp/VfxRefsGeneratorTests";
        private const string TempCatalogPath = TempFolder + "/TempCatalog.asset";
        private const string TempOutputPath = TempFolder + "/GeneratedVfxRefs.cs";

        /// <summary>
        /// Cleans temporary test assets.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempOutputPath);
            AssetDatabase.DeleteAsset(TempCatalogPath);
            AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Verifies source generation order is stable and sorted by VfxId string.
        /// </summary>
        [Test]
        public void GenerateSource_SortsByIdString()
        {
            var source = GenerateSource(new[]
            {
                "Effects.Zeta",
                "Effects.Alpha",
                "Gameplay.Hit",
                "Effects.Beta"
            });

            var alphaIndex = source.IndexOf("Effects.Alpha", StringComparison.Ordinal);
            var betaIndex = source.IndexOf("Effects.Beta", StringComparison.Ordinal);
            var zetaIndex = source.IndexOf("Effects.Zeta", StringComparison.Ordinal);

            Assert.GreaterOrEqual(alphaIndex, 0);
            Assert.GreaterOrEqual(betaIndex, 0);
            Assert.GreaterOrEqual(zetaIndex, 0);
            Assert.Less(alphaIndex, betaIndex);
            Assert.Less(betaIndex, zetaIndex);
        }

        /// <summary>
        /// Verifies sanitization and deterministic collision suffixing.
        /// </summary>
        [Test]
        public void GenerateSource_SanitizesAndDisambiguatesIdentifiers()
        {
            var source = GenerateSource(new[]
            {
                "Effects.Fire Ball",
                "Effects.Fire-Ball",
                "Effects.1Start",
                "class.switch"
            });

            AssertContains(source, "public static readonly VfxId Fire_Ball = new VfxId(\"Effects.Fire Ball\")");
            AssertContains(source, "public static readonly VfxId Fire_Ball_2 = new VfxId(\"Effects.Fire-Ball\")");
            AssertContains(source, "public static readonly VfxId _1Start");
            AssertContains(source, "public static class _class");
            AssertContains(source, "public static readonly VfxId _switch");
        }

        /// <summary>
        /// Verifies second generation run does not rewrite unchanged output.
        /// </summary>
        [Test]
        public void GenerateFromProject_SecondRunIsUnchanged()
        {
            EnsureFolder(TempFolder);

            var catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(new[]
            {
                new VfxCatalogEntry("Effects.A", null),
                new VfxCatalogEntry("Effects.B", null)
            });
            AssetDatabase.CreateAsset(catalog, TempCatalogPath);
            AssetDatabase.SaveAssets();

            var first = GenerateFromProject(TempOutputPath);
            var second = GenerateFromProject(TempOutputPath);

            Assert.IsTrue(first.Changed);
            Assert.IsFalse(second.Changed);
            Assert.AreEqual(2, second.IdCount);
            Assert.AreEqual(TempOutputPath, second.OutputPath);
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<MonoScript>(TempOutputPath) != null);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var segments = path.Split('/');
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

        private static string GenerateSource(IEnumerable ids)
        {
            var generatorType = GetGeneratorType();
            var method = generatorType.GetMethod("GenerateSource", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Could not resolve GenerateSource.");
            var output = method.Invoke(null, new object[] { ids });
            Assert.IsInstanceOf<string>(output);
            return (string)output;
        }

        private static GenerationResultProxy GenerateFromProject(string outputPath)
        {
            var generatorType = GetGeneratorType();
            var method = generatorType.GetMethod("GenerateFromProject", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Could not resolve GenerateFromProject.");

            var rawResult = method.Invoke(null, new object[] { outputPath });
            Assert.IsNotNull(rawResult, "GenerateFromProject returned null.");
            return new GenerationResultProxy(rawResult);
        }

        private static Type GetGeneratorType()
        {
            var type = Type.GetType("Frost9.VFX.Editor.VfxRefsGenerator, Frost9.VFX.Editor", throwOnError: false);
            Assert.IsNotNull(type, "Could not resolve VfxRefsGenerator type.");
            return type;
        }

        private sealed class GenerationResultProxy
        {
            private readonly object rawResult;

            public GenerationResultProxy(object rawResult)
            {
                this.rawResult = rawResult;
            }

            public bool Changed => ReadValue<bool>("Changed");

            public int IdCount => ReadValue<int>("IdCount");

            public string OutputPath => ReadValue<string>("OutputPath");

            private T ReadValue<T>(string propertyName)
            {
                var property = rawResult.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(property, $"Missing property '{propertyName}' on generation result.");
                var value = property.GetValue(rawResult);
                Assert.IsNotNull(value, $"Property '{propertyName}' returned null.");
                return (T)value;
            }
        }

        private static void AssertContains(string source, string expected)
        {
            Assert.IsTrue(
                source.Contains(expected, StringComparison.Ordinal),
                $"Expected generated source to contain '{expected}', but it did not.\nGenerated source:\n{source}");
        }
    }
}
