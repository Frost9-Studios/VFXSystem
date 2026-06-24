using System.Collections.Generic;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Classification of a serialized VFX id value relative to known catalog ids.
    /// </summary>
    public enum VfxIdValueState
    {
        /// <summary>The value is empty.</summary>
        None,

        /// <summary>The value matches a project-known id.</summary>
        Known,

        /// <summary>The value is non-empty but not present in any catalog.</summary>
        Missing
    }

    /// <summary>
    /// A selectable id option for a dropdown.
    /// </summary>
    public readonly struct VfxIdOption
    {
        /// <summary>
        /// Initializes an option.
        /// </summary>
        public VfxIdOption(string value, string displayLabel, bool hasConflict)
        {
            Value = value ?? string.Empty;
            DisplayLabel = displayLabel ?? string.Empty;
            HasConflict = hasConflict;
        }

        /// <summary>Gets the raw id value.</summary>
        public string Value { get; }

        /// <summary>Gets the display label (may include provenance/conflict annotation).</summary>
        public string DisplayLabel { get; }

        /// <summary>Gets whether this id has a duplicate or sanitized-collision conflict.</summary>
        public bool HasConflict { get; }
    }

    /// <summary>
    /// Reusable, GUI-free decision model for a searchable VFX id field. Shared by the package's
    /// <see cref="VfxIdPropertyDrawer"/> and available to project tooling that renders string id
    /// fields. Built from a discovery index so it never claims runtime-validity, only project-known.
    /// </summary>
    public sealed class VfxIdDrawerModel
    {
        private VfxIdDrawerModel(
            string currentValue,
            VfxIdValueState state,
            IReadOnlyList<VfxIdOption> options,
            bool currentHasConflict,
            string currentSourceCatalogPath)
        {
            CurrentValue = currentValue;
            State = state;
            Options = options;
            CurrentHasConflict = currentHasConflict;
            CurrentSourceCatalogPath = currentSourceCatalogPath;
        }

        /// <summary>Gets the current serialized value.</summary>
        public string CurrentValue { get; }

        /// <summary>Gets the classification of the current value.</summary>
        public VfxIdValueState State { get; }

        /// <summary>Gets selectable id options (excluding the explicit None entry).</summary>
        public IReadOnlyList<VfxIdOption> Options { get; }

        /// <summary>Gets whether the current value has a duplicate/collision conflict.</summary>
        public bool CurrentHasConflict { get; }

        /// <summary>Gets the source catalog path for the current value, or empty.</summary>
        public string CurrentSourceCatalogPath { get; }

        /// <summary>
        /// Builds a drawer model from the current value and a discovery index.
        /// </summary>
        /// <param name="currentValue">Current serialized id value.</param>
        /// <param name="index">Project discovery index (may be null).</param>
        /// <returns>Drawer model.</returns>
        public static VfxIdDrawerModel Build(string currentValue, VfxCatalogProjectIndex index)
        {
            var value = currentValue ?? string.Empty;

            var conflictIds = new HashSet<string>(System.StringComparer.Ordinal);
            var distinct = new HashSet<string>(System.StringComparer.Ordinal);
            var sourcePathById = new Dictionary<string, List<string>>(System.StringComparer.Ordinal);

            if (index != null)
            {
                CollectConflicts(index.DuplicateRawIdGroups, conflictIds);
                CollectConflicts(index.SanitizedCollisionGroups, conflictIds);

                for (var i = 0; i < index.Records.Count; i++)
                {
                    var record = index.Records[i];
                    if (!record.IsWellFormedId)
                    {
                        continue;
                    }

                    if (!sourcePathById.TryGetValue(record.RawId, out var paths))
                    {
                        paths = new List<string>();
                        sourcePathById.Add(record.RawId, paths);
                    }

                    if (!paths.Contains(record.CatalogAssetPath))
                    {
                        paths.Add(record.CatalogAssetPath);
                    }
                }

                for (var i = 0; i < index.DistinctProjectIds.Count; i++)
                {
                    distinct.Add(index.DistinctProjectIds[i].Value);
                }
            }

            var options = new List<VfxIdOption>();
            if (index != null)
            {
                for (var i = 0; i < index.DistinctProjectIds.Count; i++)
                {
                    var id = index.DistinctProjectIds[i].Value;
                    var hasConflict = conflictIds.Contains(id);
                    options.Add(new VfxIdOption(id, BuildLabel(id, hasConflict, sourcePathById), hasConflict));
                }
            }

            var state = string.IsNullOrEmpty(value)
                ? VfxIdValueState.None
                : distinct.Contains(value)
                    ? VfxIdValueState.Known
                    : VfxIdValueState.Missing;

            var currentHasConflict = !string.IsNullOrEmpty(value) && conflictIds.Contains(value);
            var currentSourcePath = sourcePathById.TryGetValue(value, out var currentPaths) && currentPaths.Count > 0
                ? currentPaths[0]
                : string.Empty;

            return new VfxIdDrawerModel(value, state, options, currentHasConflict, currentSourcePath);
        }

        private static void CollectConflicts(
            IReadOnlyList<IReadOnlyList<VfxCatalogIdRecord>> groups,
            HashSet<string> conflictIds)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                for (var j = 0; j < group.Count; j++)
                {
                    conflictIds.Add(group[j].RawId);
                }
            }
        }

        private static string BuildLabel(string id, bool hasConflict, Dictionary<string, List<string>> sourcePathById)
        {
            var label = id;
            if (sourcePathById.TryGetValue(id, out var paths) && paths.Count > 1)
            {
                label += $"  ({paths.Count} catalogs)";
            }

            if (hasConflict)
            {
                label += "  (conflict)";
            }

            return label;
        }
    }
}
