using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Frost9.VFX.Editor;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Editor tests for the public identifier sanitizer and collision analysis, ensuring parity with
    /// the deterministic generator.
    /// </summary>
    public class VfxIdentifierSanitizerEditorTests
    {
        /// <summary>
        /// Verifies per-segment sanitization rules (letters, leading digits, keywords, separators).
        /// </summary>
        [Test]
        public void Sanitize_AppliesGenerationRules()
        {
            Assert.AreEqual("Fire_Ball", VfxIdentifierSanitizer.Sanitize("Fire Ball"));
            Assert.AreEqual("Fire_Ball", VfxIdentifierSanitizer.Sanitize("Fire-Ball"));
            Assert.AreEqual("_1Start", VfxIdentifierSanitizer.Sanitize("1Start"));
            Assert.AreEqual("_class", VfxIdentifierSanitizer.Sanitize("class"));
            Assert.AreEqual("_switch", VfxIdentifierSanitizer.Sanitize("switch"));
            Assert.AreEqual("Id", VfxIdentifierSanitizer.Sanitize(""));
            Assert.AreEqual("Group", VfxIdentifierSanitizer.Sanitize("   ", VfxIdentifierSanitizer.GroupFallback));
        }

        /// <summary>
        /// Verifies analysis produces the same disambiguated names the generator emits and surfaces
        /// the collision group deterministically.
        /// </summary>
        [Test]
        public void Analyze_DisambiguatesAndGroupsCollisions()
        {
            var analysis = VfxIdentifierAnalysis.Analyze(new[]
            {
                "Effects.Fire Ball",
                "Effects.Fire-Ball",
                "Effects.Unique"
            });

            Assert.AreEqual("Fire_Ball", GeneratedName(analysis, "Effects.Fire Ball"));
            Assert.AreEqual("Fire_Ball_2", GeneratedName(analysis, "Effects.Fire-Ball"));
            Assert.AreEqual("Unique", GeneratedName(analysis, "Effects.Unique"));

            Assert.AreEqual("Effects.Fire_Ball", GeneratedPath(analysis, "Effects.Fire Ball"));
            Assert.AreEqual("Effects.Fire_Ball_2", GeneratedPath(analysis, "Effects.Fire-Ball"));

            Assert.IsTrue(analysis.HasCollisions);
            Assert.AreEqual(1, analysis.CollisionGroups.Count);
            CollectionAssert.AreEqual(
                new[] { "Effects.Fire Ball", "Effects.Fire-Ball" },
                analysis.CollisionGroups[0].ToArray());
        }

        /// <summary>
        /// Verifies the flat filename-as-id case (no namespace) collides at the leaf identifier.
        /// </summary>
        [Test]
        public void Analyze_FlatFilenameCollision()
        {
            var analysis = VfxIdentifierAnalysis.Analyze(new[] { "Fire Ball", "Fire-Ball", "Lightning" });

            Assert.AreEqual("Fire_Ball", GeneratedName(analysis, "Fire Ball"));
            Assert.AreEqual("Fire_Ball_2", GeneratedName(analysis, "Fire-Ball"));
            Assert.AreEqual("Lightning", GeneratedName(analysis, "Lightning"));

            Assert.AreEqual(1, analysis.CollisionGroups.Count);
            CollectionAssert.AreEqual(
                new[] { "Fire Ball", "Fire-Ball" },
                analysis.CollisionGroups[0].ToArray());
        }

        /// <summary>
        /// Verifies distinct ids that do not collide report no collisions and that exact duplicates
        /// are not treated as collisions (deduplicated like generation).
        /// </summary>
        [Test]
        public void Analyze_NoFalseCollisions()
        {
            var analysis = VfxIdentifierAnalysis.Analyze(new[]
            {
                "Effects.Alpha",
                "Effects.Beta",
                "Effects.Alpha"
            });

            Assert.AreEqual(2, analysis.Identifiers.Count, "Exact duplicate should be deduplicated.");
            Assert.IsFalse(analysis.HasCollisions);
        }

        /// <summary>
        /// Verifies the generator emits identical output to the analysis-driven path (regression guard
        /// against name drift after the sanitizer extraction).
        /// </summary>
        [Test]
        public void GenerateSource_MatchesExpectedDisambiguation()
        {
            var source = VfxRefsGenerator.GenerateSource(new[]
            {
                "Effects.Fire Ball",
                "Effects.Fire-Ball",
                "Effects.1Start",
                "class.switch"
            });

            StringAssert.Contains("public static readonly VfxId Fire_Ball = new VfxId(\"Effects.Fire Ball\")", source);
            StringAssert.Contains("public static readonly VfxId Fire_Ball_2 = new VfxId(\"Effects.Fire-Ball\")", source);
            StringAssert.Contains("public static readonly VfxId _1Start", source);
            StringAssert.Contains("public static class _class", source);
            StringAssert.Contains("public static readonly VfxId _switch", source);
        }

        private static string GeneratedName(VfxIdentifierAnalysis analysis, string rawId)
        {
            return analysis.Identifiers.First(identifier => identifier.RawId == rawId).GeneratedName;
        }

        private static string GeneratedPath(VfxIdentifierAnalysis analysis, string rawId)
        {
            return analysis.Identifiers.First(identifier => identifier.RawId == rawId).GeneratedPath;
        }
    }
}
