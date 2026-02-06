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
        {
            CatalogCount = catalogCount;
            IdCount = idCount;
            OutputPath = outputPath ?? string.Empty;
            Changed = changed;
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
    }
}
