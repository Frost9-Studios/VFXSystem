using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Deterministic generator for runtime <c>VFXRefs</c> identifier wrappers.
    /// </summary>
    public static class VfxRefsGenerator
    {
        /// <summary>
        /// Default generated file path under project resources (outside the package).
        /// </summary>
        public const string DefaultOutputPath = "Assets/Resources/VFX/VFXRefs.cs";

        /// <summary>
        /// Generates refs from all catalog assets and writes the runtime generated file.
        /// </summary>
        /// <param name="outputPath">Optional output path override.</param>
        /// <returns>Generation operation result.</returns>
        public static VfxRefsGenerationResult GenerateFromProject(string outputPath = DefaultOutputPath)
        {
            var rawIds = new List<string>();
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

                    rawIds.Add(entry.Id.Value);
                }
            }

            var analysis = VfxIdentifierAnalysis.Analyze(rawIds);
            var idCount = analysis.Identifiers.Count;
            var warnings = BuildWarnings(rawIds, analysis);
            var conflicts = analysis.CollisionGroups;

            if (idCount == 0 && File.Exists(outputPath))
            {
                return new VfxRefsGenerationResult(guids.Length, 0, outputPath, false, warnings, conflicts);
            }

            var source = GenerateSource(analysis);
            var changed = WriteIfChanged(outputPath, source);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            return new VfxRefsGenerationResult(guids.Length, idCount, outputPath, changed, warnings, conflicts);
        }

        /// <summary>
        /// Builds deterministic source text from a set of id strings.
        /// </summary>
        /// <param name="ids">Identifier values to emit.</param>
        /// <returns>Generated C# source text.</returns>
        public static string GenerateSource(IEnumerable<string> ids)
        {
            return GenerateSource(VfxIdentifierAnalysis.Analyze(ids));
        }

        private static string GenerateSource(VfxIdentifierAnalysis analysis)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("namespace Frost9.VFX");
            builder.AppendLine("{");
            builder.AppendLine("    /// <summary>");
            builder.AppendLine("    /// Generated VFX identifier references.");
            builder.AppendLine("    /// </summary>");
            builder.AppendLine("    public static class VFXRefs");
            builder.AppendLine("    {");
            EmitNodeChildren(builder, analysis.Root, 2);
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void EmitNodeChildren(StringBuilder builder, VfxIdentifierTrieNode node, int indentLevel)
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

        private static List<string> BuildWarnings(List<string> rawIds, VfxIdentifierAnalysis analysis)
        {
            var warnings = new List<string>();

            var counts = new Dictionary<string, int>(System.StringComparer.Ordinal);
            for (var i = 0; i < rawIds.Count; i++)
            {
                var raw = rawIds[i];
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var trimmed = raw.Trim();
                counts.TryGetValue(trimmed, out var count);
                counts[trimmed] = count + 1;
            }

            var duplicateKeys = new List<string>();
            foreach (var pair in counts)
            {
                if (pair.Value > 1)
                {
                    duplicateKeys.Add(pair.Key);
                }
            }

            duplicateKeys.Sort(System.StringComparer.Ordinal);
            for (var i = 0; i < duplicateKeys.Count; i++)
            {
                var key = duplicateKeys[i];
                warnings.Add(
                    $"Duplicate raw id '{key}' found in {counts[key]} catalog entries; only one is emitted.");
            }

            for (var i = 0; i < analysis.CollisionGroups.Count; i++)
            {
                var group = analysis.CollisionGroups[i];
                warnings.Add(
                    $"Ids [{string.Join(", ", group)}] sanitize to the same identifier and were disambiguated with numeric suffixes.");
            }

            return warnings;
        }

        private static string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
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
    }
}
