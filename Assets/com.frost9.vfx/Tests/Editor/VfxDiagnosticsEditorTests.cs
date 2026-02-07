using NUnit.Framework;
using UnityEditor;

namespace Frost9.VFX.Tests
{
    /// <summary>
    /// Editor tests for diagnostics tooling behavior.
    /// </summary>
    public class VfxDiagnosticsEditorTests
    {
        /// <summary>
        /// Verifies collector reports runtime-unavailable state when not in play mode.
        /// </summary>
        [Test]
        public void Collect_NotInPlayMode_ReturnsUnavailableStatus()
        {
            Assert.IsFalse(EditorApplication.isPlaying, "This test must run in EditMode.");

            var snapshot = Frost9.VFX.Editor.VfxDiagnosticsCollector.Collect();
            Assert.AreEqual(Frost9.VFX.Editor.VfxDiagnosticsCollectionStatus.NotInPlayMode, snapshot.Status);
            Assert.Greater(snapshot.Message.Length, 0);
            Assert.IsNotNull(snapshot.Managers);
            Assert.AreEqual(0, snapshot.Managers.Length);
        }

        /// <summary>
        /// Verifies diagnostics window can be created in editor context.
        /// </summary>
        [Test]
        public void DiagnosticsWindow_CanOpen()
        {
            var window = EditorWindow.GetWindow<Frost9.VFX.Editor.VfxDiagnosticsWindow>();
            Assert.IsNotNull(window);
            window.Close();
        }
    }
}
