namespace Frost9.VFX.Editor
{
    /// <summary>
    /// Validation result for a single catalog with its source provenance.
    /// </summary>
    public sealed class VfxCatalogValidationReport
    {
        /// <summary>
        /// Initializes a validation report.
        /// </summary>
        /// <param name="catalog">Validated catalog asset.</param>
        /// <param name="assetPath">Catalog asset path (empty for in-memory catalogs).</param>
        /// <param name="result">Validation result.</param>
        public VfxCatalogValidationReport(VfxCatalog catalog, string assetPath, VfxCatalogValidationResult result)
        {
            Catalog = catalog;
            AssetPath = assetPath ?? string.Empty;
            Result = result;
        }

        /// <summary>
        /// Gets the validated catalog.
        /// </summary>
        public VfxCatalog Catalog { get; }

        /// <summary>
        /// Gets the catalog asset path.
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        /// Gets the validation result.
        /// </summary>
        public VfxCatalogValidationResult Result { get; }
    }
}
