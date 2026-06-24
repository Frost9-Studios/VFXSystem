using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Frost9.VFX.Editor;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// GUI-free tests for the VfxId drawer decision model.
    /// </summary>
    public class VfxIdDrawerModelEditorTests
    {
        /// <summary>
        /// Verifies known ids are listed and the empty value maps to the None state.
        /// </summary>
        [Test]
        public void Model_ListsKnownIds_AndNoneState()
        {
            var index = BuildIndex();

            var none = VfxIdDrawerModel.Build(string.Empty, index);
            Assert.AreEqual(VfxIdValueState.None, none.State);
            CollectionAssert.AreEquivalent(
                new[] { "Effects.A", "Effects.B", "Effects.Dup" },
                none.Options.Select(o => o.Value).ToArray());

            var known = VfxIdDrawerModel.Build("Effects.A", index);
            Assert.AreEqual(VfxIdValueState.Known, known.State);
            Assert.IsFalse(known.CurrentHasConflict);
        }

        /// <summary>
        /// Verifies a missing current id is flagged without being cleared.
        /// </summary>
        [Test]
        public void Model_FlagsMissingCurrentId_WithoutClearing()
        {
            var index = BuildIndex();

            var model = VfxIdDrawerModel.Build("Effects.Gone", index);

            Assert.AreEqual(VfxIdValueState.Missing, model.State);
            Assert.AreEqual("Effects.Gone", model.CurrentValue, "Missing value must be preserved.");
        }

        /// <summary>
        /// Verifies an arbitrary manual value is preserved (treated as missing but never discarded).
        /// </summary>
        [Test]
        public void Model_ManualEntry_PreservesArbitraryValue()
        {
            var index = BuildIndex();

            var model = VfxIdDrawerModel.Build("Debug.Custom_Value", index);

            Assert.AreEqual(VfxIdValueState.Missing, model.State);
            Assert.AreEqual("Debug.Custom_Value", model.CurrentValue);
        }

        /// <summary>
        /// Verifies conflicting ids are marked as conflicts, not presented as clean.
        /// </summary>
        [Test]
        public void Model_DoesNotPresentConflictingIdsAsClean()
        {
            var index = BuildIndex();

            var dupOption = VfxIdDrawerModel.Build(string.Empty, index).Options.First(o => o.Value == "Effects.Dup");
            var cleanOption = VfxIdDrawerModel.Build(string.Empty, index).Options.First(o => o.Value == "Effects.A");

            Assert.IsTrue(dupOption.HasConflict);
            Assert.IsFalse(cleanOption.HasConflict);

            var dupModel = VfxIdDrawerModel.Build("Effects.Dup", index);
            Assert.IsTrue(dupModel.CurrentHasConflict);
        }

        private static VfxCatalogProjectIndex BuildIndex()
        {
            var recordA = new VfxCatalogIdRecord("Effects.A", null, "Assets/A.asset", 0, null, true, "A", false, false);
            var recordB = new VfxCatalogIdRecord("Effects.B", null, "Assets/B.asset", 0, null, true, "B", false, false);
            var dup1 = new VfxCatalogIdRecord("Effects.Dup", null, "Assets/A.asset", 1, null, true, "Dup", true, false);
            var dup2 = new VfxCatalogIdRecord("Effects.Dup", null, "Assets/B.asset", 1, null, true, "Dup", true, false);

            var records = new List<VfxCatalogIdRecord> { recordA, recordB, dup1, dup2 };
            var distinct = new List<VfxId>
            {
                new VfxId("Effects.A"),
                new VfxId("Effects.B"),
                new VfxId("Effects.Dup")
            };
            var duplicateGroups = new List<IReadOnlyList<VfxCatalogIdRecord>>
            {
                new List<VfxCatalogIdRecord> { dup1, dup2 }
            };

            return new VfxCatalogProjectIndex(
                records,
                distinct,
                duplicateGroups,
                new List<IReadOnlyList<VfxCatalogIdRecord>>(),
                new List<VfxCatalogIdRecord>());
        }
    }
}
