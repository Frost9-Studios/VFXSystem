using System;
using System.Collections.Generic;
using System.Text;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Public access to the exact identifier sanitization rules used by <see cref="VfxRefsGenerator"/>.
    /// </summary>
    /// <remarks>
    /// Consumers that derive C# identifiers from raw VFX ids (for example a project that uses prefab
    /// file names as ids) must use these rules so their analysis matches generated output. Generated
    /// names are produced by <see cref="VfxIdentifierAnalysis"/>, which builds on this sanitizer.
    /// </remarks>
    public static class VfxIdentifierSanitizer
    {
        /// <summary>
        /// Fallback used for intermediate (group/class) segments.
        /// </summary>
        public const string GroupFallback = "Group";

        /// <summary>
        /// Fallback used for leaf (field) segments.
        /// </summary>
        public const string FieldFallback = "Id";

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
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
        /// Sanitizes a single raw segment into a valid C# identifier.
        /// </summary>
        /// <param name="rawValue">Raw segment text.</param>
        /// <param name="fallback">Fallback identifier when the segment sanitizes to nothing usable.</param>
        /// <returns>Sanitized identifier (without collision disambiguation).</returns>
        public static string Sanitize(string rawValue, string fallback = FieldFallback)
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

        /// <summary>
        /// Allocates a unique identifier within a single scope using deterministic <c>_N</c> suffixing.
        /// </summary>
        /// <param name="counters">Per-scope allocation counters.</param>
        /// <param name="sanitized">Already-sanitized base identifier.</param>
        /// <returns>Unique identifier for the scope.</returns>
        internal static string AllocateUnique(Dictionary<string, int> counters, string sanitized)
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
    }
}
