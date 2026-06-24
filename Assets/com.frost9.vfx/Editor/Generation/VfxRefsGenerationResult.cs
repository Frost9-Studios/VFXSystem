using System;
using System.Collections.Generic;

namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Outcome information for a VFX refs generation operation.
    /// </summary>
    public readonly struct VfxRefsGenerationResult
    {
        /// <summary>
        /// Initializes the generation result.
        /// </summary>
        /// <param name="catalogCount">Number of catalogs scanned.</param>
        /// <param name="idCount">Number of ids emitted.</param>
        /// <param name="outputPath">Generated file path.</param>
        /// <param name="changed">Whether output file content changed.</param>
        public VfxRefsGenerationResult(int catalogCount, int idCount, string outputPath, bool changed)
            : this(catalogCount, idCount, outputPath, changed, null, null)
        {
        }

        /// <summary>
        /// Initializes the generation result with structured warnings and conflicts.
        /// </summary>
        /// <param name="catalogCount">Number of catalogs scanned.</param>
        /// <param name="idCount">Number of ids emitted.</param>
        /// <param name="outputPath">Generated file path.</param>
        /// <param name="changed">Whether output file content changed.</param>
        /// <param name="warnings">Human-readable warnings (duplicates, collisions).</param>
        /// <param name="conflicts">Groups of raw ids that collide after sanitization.</param>
        public VfxRefsGenerationResult(
            int catalogCount,
            int idCount,
            string outputPath,
            bool changed,
            IReadOnlyList<string> warnings,
            IReadOnlyList<IReadOnlyList<string>> conflicts)
        {
            CatalogCount = catalogCount;
            IdCount = idCount;
            OutputPath = outputPath ?? string.Empty;
            Changed = changed;
            Warnings = warnings ?? Array.Empty<string>();
            Conflicts = conflicts ?? Array.Empty<IReadOnlyList<string>>();
        }

        /// <summary>
        /// Gets number of scanned catalogs.
        /// </summary>
        public int CatalogCount { get; }

        /// <summary>
        /// Gets number of emitted ids.
        /// </summary>
        public int IdCount { get; }

        /// <summary>
        /// Gets output file path.
        /// </summary>
        public string OutputPath { get; }

        /// <summary>
        /// Gets whether output file content changed.
        /// </summary>
        public bool Changed { get; }

        /// <summary>
        /// Gets human-readable warnings discovered during generation.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; }

        /// <summary>
        /// Gets groups of raw ids that collide to the same sanitized identifier.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<string>> Conflicts { get; }

        /// <summary>
        /// Gets whether any sanitized-identifier conflicts were detected.
        /// </summary>
        public bool HasConflicts => Conflicts.Count > 0;
    }
}
