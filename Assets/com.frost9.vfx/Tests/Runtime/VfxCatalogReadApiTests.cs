using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Tests for the read-only catalog API and the lookup-invalidation seam.
    /// </summary>
    public class VfxCatalogReadApiTests
    {
        private readonly List<Object> created = new List<Object>();

        private static readonly VfxId IdA = new VfxId("Effects.A");
        private static readonly VfxId IdB = new VfxId("Effects.B");

        /// <summary>
        /// Cleans up created objects.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }

        /// <summary>
        /// Verifies Count, Ids, Contains and TryGetEntry over a known catalog.
        /// </summary>
        [Test]
        public void ReadApi_ReflectsCatalogContents()
        {
            var catalog = CreateCatalog(
                new VfxCatalogEntry(IdA, null),
                new VfxCatalogEntry(IdB, null));

            Assert.AreEqual(2, catalog.Count);
            Assert.IsTrue(catalog.Contains(IdA));
            Assert.IsTrue(catalog.Contains(IdB));
            Assert.IsFalse(catalog.Contains(new VfxId("Effects.Missing")));

            var ids = catalog.Ids.ToList();
            CollectionAssert.Contains(ids, IdA);
            CollectionAssert.Contains(ids, IdB);
            Assert.AreEqual(2, ids.Count);

            Assert.IsTrue(catalog.TryGetEntry(IdA, out var entry));
            Assert.AreEqual(IdA, entry.Id);
        }

        /// <summary>
        /// Verifies InvalidateLookup forces a rebuild that reflects direct serialized-list changes
        /// (simulating editor SerializedObject mutation) without any reload.
        /// </summary>
        [Test]
        public void InvalidateLookup_RebuildsOnNextAccess()
        {
            var catalog = CreateCatalog(new VfxCatalogEntry(IdA, null));

            // Build and cache the lookup.
            Assert.IsTrue(catalog.Contains(IdA));
            Assert.IsFalse(catalog.Contains(IdB));

            // Mutate the serialized entries list directly (as a SerializedObject edit would).
            AppendEntryViaReflection(catalog, new VfxCatalogEntry(IdB, null));

            // The cached lookup is intentionally stale until invalidated.
            Assert.IsFalse(catalog.Contains(IdB), "Lookup should be stale before invalidation.");

            catalog.InvalidateLookup();
            Assert.IsTrue(catalog.Contains(IdB), "Lookup should reflect the new entry after invalidation.");
        }

        private VfxCatalog CreateCatalog(params VfxCatalogEntry[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<VfxCatalog>();
            catalog.SetEntries(entries);
            created.Add(catalog);
            return catalog;
        }

        private static void AppendEntryViaReflection(VfxCatalog catalog, VfxCatalogEntry entry)
        {
            var field = typeof(VfxCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing private 'entries' field on VfxCatalog.");
            var list = (List<VfxCatalogEntry>)field.GetValue(catalog);
            list.Add(entry);
        }
    }
}
