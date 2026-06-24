using System.Collections.Generic;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// The generated identifier produced for a single raw VFX id.
    /// </summary>
    public readonly struct VfxGeneratedIdentifier
    {
        /// <summary>
        /// Initializes a generated identifier mapping.
        /// </summary>
        /// <param name="rawId">Trimmed raw id.</param>
        /// <param name="generatedName">Leaf C# identifier (disambiguated).</param>
        /// <param name="generatedPath">Full access path under VFXRefs (disambiguated).</param>
        public VfxGeneratedIdentifier(string rawId, string generatedName, string generatedPath)
        {
            RawId = rawId ?? string.Empty;
            GeneratedName = generatedName ?? string.Empty;
            GeneratedPath = generatedPath ?? string.Empty;
        }

        /// <summary>
        /// Gets the trimmed raw id.
        /// </summary>
        public string RawId { get; }

        /// <summary>
        /// Gets the leaf C# identifier, including any collision <c>_N</c> suffix.
        /// </summary>
        public string GeneratedName { get; }

        /// <summary>
        /// Gets the full dotted access path under <c>VFXRefs</c>, including any collision suffixes.
        /// </summary>
        public string GeneratedPath { get; }
    }

    /// <summary>
    /// Deterministic analysis of how a set of raw VFX ids map to generated C# identifiers, including
    /// collision groups. Built on the exact rules used by <see cref="VfxRefsGenerator"/> so generated
    /// names never diverge from generation.
    /// </summary>
    public sealed class VfxIdentifierAnalysis
    {
        private VfxIdentifierAnalysis(
            VfxIdentifierTrieNode root,
            IReadOnlyList<VfxGeneratedIdentifier> identifiers,
            IReadOnlyList<IReadOnlyList<string>> collisionGroups)
        {
            Root = root;
            Identifiers = identifiers;
            CollisionGroups = collisionGroups;
        }

        /// <summary>
        /// Gets the generated identifier mapping for each distinct raw id, ordered by raw id.
        /// </summary>
        public IReadOnlyList<VfxGeneratedIdentifier> Identifiers { get; }

        /// <summary>
        /// Gets groups of raw ids that sanitize to the same base identifier within the same scope
        /// (and therefore required <c>_N</c> disambiguation). Deterministically ordered.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> CollisionGroups { get; }

        /// <summary>
        /// Gets whether any sanitized-identifier collisions were detected.
        /// </summary>
        public bool HasCollisions => CollisionGroups.Count > 0;

        /// <summary>
        /// Gets the root trie node used by generation to emit source.
        /// </summary>
        internal VfxIdentifierTrieNode Root { get; }

        /// <summary>
        /// Analyzes a set of raw ids deterministically.
        /// </summary>
        /// <param name="rawIds">Raw id strings (whitespace/empty entries are ignored).</param>
        /// <returns>Identifier analysis.</returns>
        public static VfxIdentifierAnalysis Analyze(IEnumerable<string> rawIds)
        {
            // Match generation exactly: trim, drop empties, sort ordinal, then build the trie in that
            // order so collision suffixes are assigned identically.
            var sorted = new SortedSet<string>(System.StringComparer.Ordinal);
            if (rawIds != null)
            {
                foreach (var id in rawIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        sorted.Add(id.Trim());
                    }
                }
            }

            var root = new VfxIdentifierTrieNode(string.Empty);
            var identifiers = new List<VfxGeneratedIdentifier>(sorted.Count);

            // baseKey -> raw ids that share the pre-disambiguation generated path.
            var baseGroups = new Dictionary<string, List<string>>(System.StringComparer.Ordinal);
            var baseKeyOrder = new List<string>();

            foreach (var id in sorted)
            {
                if (!TryAddId(root, id, out var generatedName, out var generatedPath, out var baseKey))
                {
                    continue;
                }

                identifiers.Add(new VfxGeneratedIdentifier(id, generatedName, generatedPath));

                if (!baseGroups.TryGetValue(baseKey, out var group))
                {
                    group = new List<string>();
                    baseGroups.Add(baseKey, group);
                    baseKeyOrder.Add(baseKey);
                }

                group.Add(id);
            }

            baseKeyOrder.Sort(System.StringComparer.Ordinal);
            var collisionGroups = new List<IReadOnlyList<string>>();
            for (var i = 0; i < baseKeyOrder.Count; i++)
            {
                var group = baseGroups[baseKeyOrder[i]];
                if (group.Count >= 2)
                {
                    group.Sort(System.StringComparer.Ordinal);
                    collisionGroups.Add(group);
                }
            }

            return new VfxIdentifierAnalysis(root, identifiers, collisionGroups);
        }

        /// <summary>
        /// Adds a raw id to the trie, returning its generated name/path and pre-disambiguation base key.
        /// Mirrors <see cref="VfxRefsGenerator"/>'s original AddId allocation exactly.
        /// </summary>
        private static bool TryAddId(
            VfxIdentifierTrieNode root,
            string id,
            out string generatedName,
            out string generatedPath,
            out string baseKey)
        {
            generatedName = null;
            generatedPath = null;
            baseKey = null;

            var rawSegments = id.Split('.');
            var segments = new List<string>(rawSegments.Length);
            for (var i = 0; i < rawSegments.Length; i++)
            {
                var segment = rawSegments[i].Trim();
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    segments.Add(segment);
                }
            }

            if (segments.Count == 0)
            {
                return false;
            }

            var baseSegments = new string[segments.Count];
            var classNames = new List<string>(segments.Count - 1);
            var current = root;
            for (var i = 0; i < segments.Count - 1; i++)
            {
                var rawSegment = segments[i];
                baseSegments[i] = VfxIdentifierSanitizer.Sanitize(rawSegment, VfxIdentifierSanitizer.GroupFallback);

                if (!current.RawSegmentToChildName.TryGetValue(rawSegment, out var className))
                {
                    className = VfxIdentifierSanitizer.AllocateUnique(current.ChildClassCounters, baseSegments[i]);
                    current.RawSegmentToChildName.Add(rawSegment, className);
                }

                if (!current.ChildrenByName.TryGetValue(className, out var child))
                {
                    child = new VfxIdentifierTrieNode(className);
                    current.ChildrenByName.Add(className, child);
                    current.Children.Add(child);
                }

                classNames.Add(className);
                current = child;
            }

            var leafBase = VfxIdentifierSanitizer.Sanitize(segments[segments.Count - 1], VfxIdentifierSanitizer.FieldFallback);
            baseSegments[segments.Count - 1] = leafBase;

            var fieldName = VfxIdentifierSanitizer.AllocateUnique(current.FieldCounters, leafBase);
            current.Fields.Add(new VfxIdentifierField(fieldName, id));

            generatedName = fieldName;
            generatedPath = classNames.Count > 0 ? string.Join(".", classNames) + "." + fieldName : fieldName;
            baseKey = string.Join(".", baseSegments);
            return true;
        }
    }

    /// <summary>
    /// Trie node used to build the deterministic generated identifier hierarchy.
    /// </summary>
    internal sealed class VfxIdentifierTrieNode
    {
        public VfxIdentifierTrieNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public List<VfxIdentifierTrieNode> Children { get; } = new List<VfxIdentifierTrieNode>();

        public Dictionary<string, VfxIdentifierTrieNode> ChildrenByName { get; } =
            new Dictionary<string, VfxIdentifierTrieNode>(System.StringComparer.Ordinal);

        public List<VfxIdentifierField> Fields { get; } = new List<VfxIdentifierField>();

        public Dictionary<string, int> ChildClassCounters { get; } =
            new Dictionary<string, int>(System.StringComparer.Ordinal);

        public Dictionary<string, string> RawSegmentToChildName { get; } =
            new Dictionary<string, string>(System.StringComparer.Ordinal);

        public Dictionary<string, int> FieldCounters { get; } =
            new Dictionary<string, int>(System.StringComparer.Ordinal);
    }

    /// <summary>
    /// A generated field within a trie node.
    /// </summary>
    internal readonly struct VfxIdentifierField
    {
        public VfxIdentifierField(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}
