using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Searchable property drawer for <see cref="VfxId"/> fields. Offers project-known ids, an explicit
    /// None choice, a manual-entry escape hatch, and warnings for missing or conflicting ids. It does
    /// not change <see cref="VfxId"/> serialization (it edits the underlying <c>value</c> string).
    /// </summary>
    [CustomPropertyDrawer(typeof(VfxId))]
    public sealed class VfxIdPropertyDrawer : PropertyDrawer
    {
        private const string ManualEntryLabel = "Manual entry…";
        private const float HelpBoxHeight = 34f;

        private readonly Dictionary<string, bool> manualModeByPath = new Dictionary<string, bool>();

        /// <summary>
        /// Gets required UI height for the property.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            if (valueProperty == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
            }

            var model = VfxIdDrawerModel.Build(valueProperty.stringValue, VfxCatalogDiscovery.DiscoverProject());
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var height = EditorGUIUtility.singleLineHeight;

            if (ShowManualField(property.propertyPath, model))
            {
                height += spacing + EditorGUIUtility.singleLineHeight;
            }

            if (model.State == VfxIdValueState.Missing)
            {
                height += spacing + HelpBoxHeight;
            }

            if (model.CurrentHasConflict)
            {
                height += spacing + HelpBoxHeight;
            }

            return height;
        }

        /// <summary>
        /// Draws the searchable id field.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            if (valueProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, includeChildren: true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var model = VfxIdDrawerModel.Build(valueProperty.stringValue, VfxCatalogDiscovery.DiscoverProject());
            var spacing = EditorGUIUtility.standardVerticalSpacing;
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var y = position.y;

            var displayed = BuildDisplayedOptions(model);
            var selected = ComputeSelectedIndex(model);
            var popupRect = new Rect(position.x, y, position.width, lineHeight);
            var fieldLabel = string.IsNullOrWhiteSpace(label.text) ? "Id" : label.text;
            var newSelected = EditorGUI.Popup(popupRect, fieldLabel, selected, displayed);
            if (newSelected != selected)
            {
                ApplySelection(newSelected, valueProperty, property.propertyPath, model);
            }

            y += lineHeight;

            if (ShowManualField(property.propertyPath, model))
            {
                y += spacing;
                var manualRect = new Rect(position.x, y, position.width, lineHeight);
                valueProperty.stringValue = EditorGUI.TextField(manualRect, "Manual id", valueProperty.stringValue);
                y += lineHeight;
            }

            if (model.State == VfxIdValueState.Missing)
            {
                y += spacing;
                var helpRect = new Rect(position.x, y, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(
                    helpRect,
                    $"Id '{model.CurrentValue}' is not in any catalog and will not resolve at runtime.",
                    MessageType.Warning);
                y += HelpBoxHeight;
            }

            if (model.CurrentHasConflict)
            {
                y += spacing;
                var helpRect = new Rect(position.x, y, position.width, HelpBoxHeight);
                EditorGUI.HelpBox(
                    helpRect,
                    $"Id '{model.CurrentValue}' has duplicate or colliding catalog entries. Resolve the conflict.",
                    MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        private bool ShowManualField(string propertyPath, VfxIdDrawerModel model)
        {
            if (model.State == VfxIdValueState.Missing)
            {
                return true;
            }

            return manualModeByPath.TryGetValue(propertyPath, out var manual) && manual;
        }

        private static string[] BuildDisplayedOptions(VfxIdDrawerModel model)
        {
            var displayed = new string[model.Options.Count + 2];
            displayed[0] = "None";
            for (var i = 0; i < model.Options.Count; i++)
            {
                displayed[i + 1] = model.Options[i].DisplayLabel;
            }

            displayed[displayed.Length - 1] = ManualEntryLabel;
            return displayed;
        }

        private static int ComputeSelectedIndex(VfxIdDrawerModel model)
        {
            switch (model.State)
            {
                case VfxIdValueState.None:
                    return 0;
                case VfxIdValueState.Known:
                    for (var i = 0; i < model.Options.Count; i++)
                    {
                        if (string.Equals(model.Options[i].Value, model.CurrentValue, System.StringComparison.Ordinal))
                        {
                            return i + 1;
                        }
                    }

                    return 0;
                default:
                    // Missing -> show the manual-entry slot as selected.
                    return model.Options.Count + 1;
            }
        }

        private void ApplySelection(int newSelected, SerializedProperty valueProperty, string propertyPath, VfxIdDrawerModel model)
        {
            if (newSelected == 0)
            {
                valueProperty.stringValue = string.Empty;
                manualModeByPath[propertyPath] = false;
                return;
            }

            if (newSelected >= 1 && newSelected <= model.Options.Count)
            {
                valueProperty.stringValue = model.Options[newSelected - 1].Value;
                manualModeByPath[propertyPath] = false;
                return;
            }

            // Manual entry chosen: keep the value, reveal the manual field.
            manualModeByPath[propertyPath] = true;
        }
    }
}
