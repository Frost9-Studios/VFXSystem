using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Deterministic generator for runtime <c>VFXRefs</c> identifier wrappers.
    /// </summary>
    public static class VfxRefsGenerator
    {
        /// <summary>
        /// Default generated file path under the runtime assembly.
        /// </summary>
        public const string DefaultOutputPath = "Assets/com.frost9.vfx/Runtime/Generated/VFXRefs.cs";

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(System.StringComparer.Ordinal)
        {
            "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue",
            "decimal","default","delegate","do","double","else","enum","event","explicit","extern","false","finally",
            "fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal","is","lock",
            "long","namespace","new","null","object","operator","out","override","params","private","protected",
            "public","readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc","static","string",
            "struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
            "using","virtual","void","volatile","while"
        };

        /// <summary>
        /// Generates refs from all catalog assets and writes the runtime generated file.
        /// </summary>
        /// <param name="outputPath">Optional output path override.</param>
        /// <returns>Generation operation result.</returns>
        public static VfxRefsGenerationResult GenerateFromProject(string outputPath = DefaultOutputPath)
        {
            var ids = new SortedSet<string>(System.StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:VfxCatalog");

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalog>(path);
                if (catalog == null)
                {
                    continue;
                }

                var entries = catalog.Entries;
                for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                {
                    var entry = entries[entryIndex];
                    if (entry == null || !entry.Id.IsValid)
                    {
                        continue;
                    }

                    ids.Add(entry.Id.Value);
                }
            }

            if (ids.Count == 0 && File.Exists(outputPath))
            {
                return new VfxRefsGenerationResult(guids.Length, 0, outputPath, false);
            }

            var source = GenerateSource(ids);
            var changed = WriteIfChanged(outputPath, source);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            return new VfxRefsGenerationResult(guids.Length, ids.Count, outputPath, changed);
        }

        /// <summary>
        /// Builds deterministic source text from a set of id strings.
        /// </summary>
        /// <param name="ids">Identifier values to emit.</param>
        /// <returns>Generated C# source text.</returns>
        public static string GenerateSource(IEnumerable<string> ids)
        {
            var sorted = new SortedSet<string>(System.StringComparer.Ordinal);
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        sorted.Add(id.Trim());
                    }
                }
            }

            var root = new Node(string.Empty);
            foreach (var id in sorted)
            {
                AddId(root, id);
            }

            var builder = new StringBuilder(4096);
            builder.AppendLine("namespace Frost9.VFX");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// Generated VFX identifier references.");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class VFXRefs");
            builder.AppendLine("    {");
            EmitNodeChildren(builder, root, 2);
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AddId(Node root, string id)
        {
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
                return;
            }

            var current = root;
            for (var i = 0; i < segments.Count - 1; i++)
            {
                var rawSegment = segments[i];
                string className;
                if (!current.RawSegmentToChildName.TryGetValue(rawSegment, out className))
                {
                    className = AllocateUniqueIdentifier(current.ChildClassCounters, SanitizeIdentifier(rawSegment, fallback: "Group"));
                    current.RawSegmentToChildName.Add(rawSegment, className);
                }

                if (!current.ChildrenByName.TryGetValue(className, out var child))
                {
                    child = new Node(className);
                    current.ChildrenByName.Add(className, child);
                    current.Children.Add(child);
                }

                current = child;
            }

            var fieldName = AllocateUniqueIdentifier(current.FieldCounters, SanitizeIdentifier(segments[segments.Count - 1], fallback: "Id"));
            current.Fields.Add(new Field(fieldName, id));
        }

        private static void EmitNodeChildren(StringBuilder builder, Node node, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);

            node.Children.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Name, b.Name));
            node.Fields.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.Value, b.Value));

            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                builder.Append(indent).AppendLine("/// <summary>");
                builder.Append(indent).Append("/// Identifier group: ").Append(child.Name).AppendLine(".");
                builder.Append(indent).AppendLine("/// </summary>");
                builder.Append(indent).Append("public static class ").Append(child.Name).AppendLine();
                builder.Append(indent).AppendLine("{");
                EmitNodeChildren(builder, child, indentLevel + 1);
                builder.Append(indent).AppendLine("}");
            }

            for (var i = 0; i < node.Fields.Count; i++)
            {
                var field = node.Fields[i];
                builder.Append(indent).AppendLine("/// <summary>");
                builder.Append(indent).Append("/// VFX id: ").Append(field.Value).AppendLine(".");
                builder.Append(indent).AppendLine("/// </summary>");
                builder.Append(indent).Append("public static readonly VfxId ").Append(field.Name)
                    .Append(" = new VfxId(\"").Append(EscapeString(field.Value)).AppendLine("\");");
            }
        }

        private static string SanitizeIdentifier(string rawValue, string fallback)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return fallback;
            }

            var builder = new StringBuilder(rawValue.Length + 4);
            for (var i = 0; i < rawValue.Length; i++)
            {
                var character = rawValue[i];
                var isLetter = char.IsLetter(character);
                var isDigit = char.IsDigit(character);
                var isUnderscore = character == '_';

                if (i == 0)
                {
                    if (isLetter || isUnderscore)
                    {
                        builder.Append(character);
                    }
                    else if (isDigit)
                    {
                        builder.Append('_').Append(character);
                    }
                    else
                    {
                        builder.Append('_');
                    }
                }
                else
                {
                    builder.Append(isLetter || isDigit || isUnderscore ? character : '_');
                }
            }

            var value = builder.ToString();
            if (string.IsNullOrWhiteSpace(value) || IsAllUnderscores(value))
            {
                value = fallback;
            }

            if (CSharpKeywords.Contains(value))
            {
                value = "_" + value;
            }

            return value;
        }

        private static string AllocateUniqueIdentifier(Dictionary<string, int> counters, string sanitized)
        {
            if (!counters.TryGetValue(sanitized, out var count))
            {
                counters[sanitized] = 1;
                return sanitized;
            }

            var next = count + 1;
            counters[sanitized] = next;
            return sanitized + "_" + next.ToString();
        }

        private static string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static bool IsAllUnderscores(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != '_')
                {
                    return false;
                }
            }

            return value.Length > 0;
        }

        private static bool WriteIfChanged(string outputPath, string content)
        {
            var normalizedOutputPath = outputPath.Replace('\\', '/');
            var directory = Path.GetDirectoryName(normalizedOutputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var existing = File.Exists(normalizedOutputPath) ? File.ReadAllText(normalizedOutputPath) : null;
            if (string.Equals(existing, content, System.StringComparison.Ordinal))
            {
                return false;
            }

            var tempPath = normalizedOutputPath + ".tmp";
            File.WriteAllText(tempPath, content, new UTF8Encoding(false));

            if (File.Exists(normalizedOutputPath))
            {
                File.Replace(tempPath, normalizedOutputPath, null);
            }
            else
            {
                File.Move(tempPath, normalizedOutputPath);
            }

            return true;
        }

        private sealed class Node
        {
            public Node(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public List<Node> Children { get; } = new List<Node>();

            public Dictionary<string, Node> ChildrenByName { get; } = new Dictionary<string, Node>(System.StringComparer.Ordinal);

            public List<Field> Fields { get; } = new List<Field>();

            public Dictionary<string, int> ChildClassCounters { get; } = new Dictionary<string, int>(System.StringComparer.Ordinal);

            public Dictionary<string, string> RawSegmentToChildName { get; } = new Dictionary<string, string>(System.StringComparer.Ordinal);

            public Dictionary<string, int> FieldCounters { get; } = new Dictionary<string, int>(System.StringComparer.Ordinal);
        }

        private readonly struct Field
        {
            public Field(string name, string value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; }

            public string Value { get; }
        }
    }
}
