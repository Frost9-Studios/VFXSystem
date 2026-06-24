using System;
using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// A batch of catalog edits applied with a single <c>ApplyModifiedProperties</c> on dispose.
    /// </summary>
    /// <remarks>
    /// Changes are only committed when the batch disposes successfully. If the serialized layout is
    /// missing or an operation aborts the batch, no changes are applied (the <c>SerializedObject</c>
    /// modifications are discarded), so the catalog is never left partially mutated.
    /// </remarks>
    public sealed class VfxCatalogEditBatch : IDisposable
    {
        private readonly VfxCatalog catalog;
        private readonly SerializedObject serializedObject;
        private readonly SerializedProperty entriesProperty;
        private bool failed;
        private bool applied;

        internal VfxCatalogEditBatch(VfxCatalog catalog)
        {
            this.catalog = catalog;
            serializedObject = new SerializedObject(catalog);
            entriesProperty = serializedObject.FindProperty(VfxCatalogSerializedNames.Entries);
        }

        /// <summary>
        /// Gets whether the catalog's serialized entries layout was found.
        /// </summary>
        public bool LayoutValid => entriesProperty != null && entriesProperty.isArray;

        /// <summary>
        /// Gets whether the batch has aborted and will not apply.
        /// </summary>
        public bool Failed => failed;

        /// <summary>
        /// Adds a new entry or updates only the prefab reference of an existing one.
        /// </summary>
        public VfxCatalogEditResult AddOrUpdate(VfxId id, GameObject prefab)
        {
            if (failed)
            {
                return Error(id, "Batch already aborted.");
            }

            if (!LayoutValid)
            {
                return Abort(id, "Catalog serialized layout not found.");
            }

            if (!id.IsValid)
            {
                return Error(id, "Id is empty or invalid.");
            }

            if (!VfxCatalogEditing.IsValidPrefabAsset(prefab))
            {
                return Error(id, $"Prefab for '{id.Value}' is null or not a prefab asset.");
            }

            var index = FindIndex(id);
            if (index < 0)
            {
                return AddEntry(id, prefab);
            }

            return UpdatePrefab(index, id, prefab);
        }

        /// <summary>
        /// Removes an entry by id.
        /// </summary>
        public VfxCatalogEditResult Remove(VfxId id)
        {
            if (failed)
            {
                return Error(id, "Batch already aborted.");
            }

            if (!LayoutValid)
            {
                return Abort(id, "Catalog serialized layout not found.");
            }

            if (!id.IsValid)
            {
                return Error(id, "Id is empty or invalid.");
            }

            var index = FindIndex(id);
            if (index < 0)
            {
                return new VfxCatalogEditResult(VfxCatalogEditOutcome.Unchanged, id, $"'{id.Value}' is not present.");
            }

            entriesProperty.DeleteArrayElementAtIndex(index);

            if (FindIndex(id) >= 0)
            {
                return Abort(id, $"Failed to remove '{id.Value}'.");
            }

            return new VfxCatalogEditResult(VfxCatalogEditOutcome.Removed, id, $"Removed '{id.Value}'.");
        }

        /// <summary>
        /// Aborts the batch so dispose applies nothing.
        /// </summary>
        public void Abort()
        {
            failed = true;
        }

        /// <summary>
        /// Applies pending changes once (unless aborted) and invalidates dependent caches.
        /// </summary>
        public void Apply()
        {
            if (applied)
            {
                return;
            }

            applied = true;

            if (failed)
            {
                return;
            }

            var changed = serializedObject.ApplyModifiedProperties();
            if (changed)
            {
                // Keep the runtime lookup and editor discovery cache coherent without a save/import
                // or domain reload.
                catalog.InvalidateLookup();
                VfxCatalogDiscovery.InvalidateCache();
            }
        }

        /// <summary>
        /// Applies the batch on dispose.
        /// </summary>
        public void Dispose()
        {
            Apply();
        }

        private VfxCatalogEditResult AddEntry(VfxId id, GameObject prefab)
        {
            var newIndex = entriesProperty.arraySize;
            entriesProperty.arraySize = newIndex + 1;
            var element = entriesProperty.GetArrayElementAtIndex(newIndex);

            if (!TryInitializeNewEntry(element, id, prefab, out var failureMessage))
            {
                return Abort(id, failureMessage);
            }

            return new VfxCatalogEditResult(VfxCatalogEditOutcome.Added, id, $"Added '{id.Value}'.");
        }

        private VfxCatalogEditResult UpdatePrefab(int index, VfxId id, GameObject prefab)
        {
            var element = entriesProperty.GetArrayElementAtIndex(index);
            var prefabProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.Prefab);
            if (prefabProperty == null)
            {
                return Abort(id, "Entry prefab property not found.");
            }

            if (prefabProperty.objectReferenceValue == prefab)
            {
                return new VfxCatalogEditResult(VfxCatalogEditOutcome.Unchanged, id, $"'{id.Value}' is unchanged.");
            }

            // Only the prefab sub-property is touched, so every tuned setting is preserved.
            prefabProperty.objectReferenceValue = prefab;
            return new VfxCatalogEditResult(VfxCatalogEditOutcome.Updated, id, $"Updated prefab for '{id.Value}'.");
        }

        private bool TryInitializeNewEntry(SerializedProperty element, VfxId id, GameObject prefab, out string failureMessage)
        {
            failureMessage = string.Empty;

            var idProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.Id);
            var idValueProperty = idProperty != null
                ? idProperty.FindPropertyRelative(VfxCatalogSerializedNames.IdValue)
                : null;
            var prefabProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.Prefab);
            var initialProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.InitialPoolSize);
            var maxProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.MaxPoolSize);
            var expansionProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.AllowPoolExpansion);
            var channelProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.DefaultChannel);
            var autoReleaseProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.AutoReleaseByDefault);
            var lifetimeProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.FallbackLifetimeSeconds);
            var parametersProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.DefaultParameters);

            if (idValueProperty == null || prefabProperty == null || initialProperty == null ||
                maxProperty == null || expansionProperty == null || channelProperty == null ||
                autoReleaseProperty == null || lifetimeProperty == null || parametersProperty == null)
            {
                failureMessage = "Catalog entry serialized layout not found.";
                return false;
            }

            // A new array element copies the previous element's values; reset the nested parameters
            // and set every field deliberately to the documented defaults.
            ResetToDefault(parametersProperty);

            idValueProperty.stringValue = id.Value;
            prefabProperty.objectReferenceValue = prefab;
            initialProperty.intValue = VfxCatalogSerializedNames.DefaultInitialPoolSize;
            maxProperty.intValue = VfxCatalogSerializedNames.DefaultMaxPoolSize;
            expansionProperty.boolValue = VfxCatalogSerializedNames.DefaultAllowPoolExpansion;
            channelProperty.enumValueIndex = VfxCatalogSerializedNames.DefaultChannelValue;
            autoReleaseProperty.boolValue = VfxCatalogSerializedNames.DefaultAutoReleaseByDefault;
            lifetimeProperty.floatValue = VfxCatalogSerializedNames.DefaultFallbackLifetimeSeconds;
            return true;
        }

        private int FindIndex(VfxId id)
        {
            for (var i = 0; i < entriesProperty.arraySize; i++)
            {
                var element = entriesProperty.GetArrayElementAtIndex(i);
                var idProperty = element.FindPropertyRelative(VfxCatalogSerializedNames.Id);
                var idValueProperty = idProperty != null
                    ? idProperty.FindPropertyRelative(VfxCatalogSerializedNames.IdValue)
                    : null;
                if (idValueProperty != null && string.Equals(idValueProperty.stringValue, id.Value, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        private VfxCatalogEditResult Abort(VfxId id, string message)
        {
            failed = true;
            return Error(id, message);
        }

        private static VfxCatalogEditResult Error(VfxId id, string message)
        {
            return new VfxCatalogEditResult(VfxCatalogEditOutcome.Error, id, message);
        }

        private static void ResetToDefault(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    return;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    return;
                case SerializedPropertyType.Float:
                    property.floatValue = 0f;
                    return;
                case SerializedPropertyType.String:
                    property.stringValue = string.Empty;
                    return;
                case SerializedPropertyType.Color:
                    property.colorValue = new Color(0f, 0f, 0f, 0f);
                    return;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    return;
                case SerializedPropertyType.LayerMask:
                    property.intValue = 0;
                    return;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = 0;
                    return;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    return;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = Vector3.zero;
                    return;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = Vector4.zero;
                    return;
                case SerializedPropertyType.Rect:
                    property.rectValue = Rect.zero;
                    return;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = new Bounds(Vector3.zero, Vector3.zero);
                    return;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    return;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = Vector2Int.zero;
                    return;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = Vector3Int.zero;
                    return;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = new AnimationCurve();
                    return;
            }

            if (property.isArray)
            {
                property.arraySize = 0;
                return;
            }

            if (property.hasChildren)
            {
                var child = property.Copy();
                var end = property.GetEndProperty();
                if (child.NextVisible(true))
                {
                    while (!SerializedProperty.EqualContents(child, end))
                    {
                        ResetToDefault(child);
                        if (!child.NextVisible(false))
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
