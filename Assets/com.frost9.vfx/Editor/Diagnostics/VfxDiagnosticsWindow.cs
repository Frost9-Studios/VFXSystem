using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Read-only diagnostics window for runtime VFX pool statistics.
    /// </summary>
    public sealed class VfxDiagnosticsWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Frost9/VFX/Diagnostics";
        private const double RefreshIntervalSeconds = 0.25d;

        private double nextRefreshAt;
        private int selectedManagerInstanceId;
        private Vector2 scrollPosition;
        private VfxDiagnosticsSnapshot snapshot;

        /// <summary>
        /// Opens the diagnostics window.
        /// </summary>
        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<VfxDiagnosticsWindow>("Frost9 VFX Diagnostics");
            window.minSize = new Vector2(600f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            nextRefreshAt = 0d;
            RefreshSnapshot(force: true);
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnInspectorUpdate()
        {
            RefreshSnapshot(force: false);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (snapshot.Status != VfxDiagnosticsCollectionStatus.Success || snapshot.Managers.Length == 0)
            {
                var messageType = snapshot.Status == VfxDiagnosticsCollectionStatus.Success ? MessageType.Info : MessageType.Warning;
                EditorGUILayout.HelpBox(string.IsNullOrWhiteSpace(snapshot.Message)
                    ? "No diagnostics data available."
                    : snapshot.Message, messageType);
                return;
            }

            var selectedIndex = ResolveSelectedIndex();
            var selectedManager = snapshot.Managers[selectedIndex];
            selectedManagerInstanceId = selectedManager.InstanceId;

            DrawManagerPicker(selectedIndex);
            DrawSummary(selectedManager);
            DrawPerIdTable(selectedManager);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Runtime VFX Stats", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                {
                    RefreshSnapshot(force: true);
                }
            }
        }

        private void DrawManagerPicker(int selectedIndex)
        {
            if (snapshot.Managers.Length <= 1)
            {
                var only = snapshot.Managers[selectedIndex];
                EditorGUILayout.LabelField("Pool Manager", $"{only.Name} ({only.InstanceId})");
                EditorGUILayout.LabelField("Hierarchy Path", only.HierarchyPath);
                return;
            }

            var labels = new string[snapshot.Managers.Length];
            for (var i = 0; i < snapshot.Managers.Length; i++)
            {
                var manager = snapshot.Managers[i];
                labels[i] = $"{manager.Name} ({manager.InstanceId})";
            }

            var nextIndex = EditorGUILayout.Popup("Pool Manager", selectedIndex, labels);
            if (nextIndex != selectedIndex)
            {
                selectedManagerInstanceId = snapshot.Managers[nextIndex].InstanceId;
                RefreshSnapshot(force: true);
            }

            EditorGUILayout.LabelField("Hierarchy Path", snapshot.Managers[nextIndex].HierarchyPath);
        }

        private static void DrawSummary(VfxDiagnosticsManagerSnapshot manager)
        {
            var stats = manager.Stats;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Active", stats.TotalActiveInstances.ToString());
            EditorGUILayout.LabelField("Pooled", stats.TotalPooledInstances.ToString());
            EditorGUILayout.LabelField("Created", stats.TotalCreatedInstances.ToString());
            EditorGUILayout.LabelField("Recycled", stats.TotalRecycleCount.ToString());
            EditorGUILayout.EndVertical();
        }

        private void DrawPerIdTable(VfxDiagnosticsManagerSnapshot manager)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Per Id", EditorStyles.boldLabel);

            var byId = manager.Stats.ById;
            if (byId == null || byId.Length == 0)
            {
                EditorGUILayout.HelpBox("No per-id stats yet.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Id", EditorStyles.miniBoldLabel, GUILayout.Width(240f));
                GUILayout.Label("Active", EditorStyles.miniBoldLabel, GUILayout.Width(56f));
                GUILayout.Label("Pooled", EditorStyles.miniBoldLabel, GUILayout.Width(56f));
                GUILayout.Label("Created", EditorStyles.miniBoldLabel, GUILayout.Width(56f));
                GUILayout.Label("Recycled", EditorStyles.miniBoldLabel, GUILayout.Width(64f));
            }

            for (var i = 0; i < byId.Length; i++)
            {
                var row = byId[i];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(row.Id.Value, GUILayout.Width(240f));
                    GUILayout.Label(row.ActiveInstances.ToString(), GUILayout.Width(56f));
                    GUILayout.Label(row.PooledInstances.ToString(), GUILayout.Width(56f));
                    GUILayout.Label(row.CreatedInstances.ToString(), GUILayout.Width(56f));
                    GUILayout.Label(row.RecycleCount.ToString(), GUILayout.Width(64f));
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private int ResolveSelectedIndex()
        {
            if (snapshot.Managers.Length == 0)
            {
                return -1;
            }

            if (selectedManagerInstanceId != 0)
            {
                for (var i = 0; i < snapshot.Managers.Length; i++)
                {
                    if (snapshot.Managers[i].InstanceId == selectedManagerInstanceId)
                    {
                        return i;
                    }
                }
            }

            if (snapshot.SelectedIndex >= 0 && snapshot.SelectedIndex < snapshot.Managers.Length)
            {
                return snapshot.SelectedIndex;
            }

            return 0;
        }

        private void RefreshSnapshot(bool force)
        {
            var now = EditorApplication.timeSinceStartup;
            if (!force && now < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = now + RefreshIntervalSeconds;
            snapshot = VfxDiagnosticsCollector.Collect(selectedManagerInstanceId);
            Repaint();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange _)
        {
            nextRefreshAt = 0d;
            RefreshSnapshot(force: true);
        }
    }
}
