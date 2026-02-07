using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Property drawer for rendering <see cref="VfxId"/> as a single string field.
    /// </summary>
    [CustomPropertyDrawer(typeof(VfxId))]
    public sealed class VfxIdPropertyDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight = 34f;

        /// <summary>
        /// Gets required UI height for the property.
        /// </summary>
        /// <param name="property">Serialized property.</param>
        /// <param name="label">Inspector label.</param>
        /// <returns>Height in pixels.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            if (valueProperty == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, includeChildren: true);
            }

            if (!string.IsNullOrWhiteSpace(valueProperty.stringValue))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + HelpBoxHeight;
        }

        /// <summary>
        /// Draws the property field and inline guidance.
        /// </summary>
        /// <param name="position">Target inspector rect.</param>
        /// <param name="property">Serialized property.</param>
        /// <param name="label">Inspector label.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProperty = property.FindPropertyRelative("value");
            if (valueProperty == null)
            {
                EditorGUI.PropertyField(position, property, label, includeChildren: true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var fieldLabel = new GUIContent(
                string.IsNullOrWhiteSpace(label.text) ? "Id" : label.text,
                "Stable string id. Use a namespaced value, for example: Effects.FireballImpact");

            valueProperty.stringValue = EditorGUI.TextField(fieldRect, fieldLabel, valueProperty.stringValue);

            if (string.IsNullOrWhiteSpace(valueProperty.stringValue))
            {
                var helpRect = new Rect(
                    position.x,
                    fieldRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    HelpBoxHeight);

                EditorGUI.HelpBox(helpRect, "Id is a string. Example: Effects.FireballImpact", MessageType.Info);
            }

            EditorGUI.EndProperty();
        }
    }
}
