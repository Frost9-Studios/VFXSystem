# Changelog

All notable changes to this package are documented in this file.

## [0.2.0] - 2026-06-24
### Added
- Read-only catalog API on `VfxCatalog`: `Count`, `Ids`, `Contains(VfxId)`, plus an `InvalidateLookup()` coherence seam (all `UnityEditor`-free).
- Editor catalog mutation API `VfxCatalogEditing` (`AddOrUpdate`, `Remove`, `Sync`, `BeginBatch`) built on `SerializedObject`: preflight validation, single-apply batches, prefab-only updates that preserve every tuned entry setting, and opt-in stale pruning. Structured `VfxCatalogEditResult` / `VfxCatalogSyncResult`.
- Project and catalog-scoped id discovery `VfxCatalogDiscovery` (`DiscoverProject`, `DiscoverCatalog`) with full provenance, duplicate raw-id and sanitized-collision detection, deterministic ordering, and cache invalidation via `AssetPostprocessor` and Undo/Redo.
- Public identifier sanitization and collision analysis (`VfxIdentifierSanitizer`, `VfxIdentifierAnalysis`) shared with `VFXRefs` generation, so consumers can mirror generated names exactly.
- Programmatic validation entry points `VfxCatalogValidation` (`ValidateCatalog`, `ValidateAllProjectCatalogs`) returning per-catalog provenance.
- Searchable `VfxId` property drawer (project-known id dropdown, explicit None, manual-entry escape hatch, missing/conflict warnings) backed by a reusable `VfxIdDrawerModel`.
- `VfxPrefabAuthoring.EnsurePlayable` — idempotent runner injection into regular/variant prefabs with actionable errors for unsupported (model/immutable) prefabs.

### Fixed
- `MissingReferenceException` during Play Mode exit teardown after pooled VFX have been used. Pooled instances are classified (live / GameObject-destroyed / runner-destroyed) and dead instances are retired with exactly-once bookkeeping; a destroyed runner whose GameObject is still alive is retired without leaking pool capacity. Repeated `ClearAll`/`Dispose`/`OnDestroy` are harmless.

### Changed
- Raised the minimum supported editor to Unity 6.3 (`6000.3`); the project runs on `6000.3.9f1`.
- `VFXRefs` generation refactored onto the shared sanitizer/trie (generated names are byte-identical) and now reports structured warnings and conflicts (`Warnings`, `Conflicts`, `HasConflicts` on `VfxRefsGenerationResult`).
- Catalog validation menu and inspector are now thin wrappers over the programmatic validation API.

## [0.1.0] - 2026-02-07
### Added
- Runtime service API (`IVfxService`, `VfxService`, `VfxManager`) for catalog-driven playback.
- Per-id pooled runtime with generation-safe handles and scoped stop filters.
- Built-in runners: `PrefabVfxPlayable` and `LineArcVfxPlayable`.
- Attach semantics (`WorldLocked`, `FollowTransform`, `FollowPositionOnly`) with target-loss safety.
- Runtime diagnostics snapshot API and editor diagnostics window.
- Catalog validation tooling (manual, menu, and throttled auto-validation).
- Deterministic `VFXRefs` source generation.
- Optional VContainer registration helpers (`RegisterVfx`, `RegisterVfxWithExistingPool`).
- Runtime and editor test coverage for Layer 1/2 behavior and tooling.

### Changed
- Package metadata updated for Unity 6000.3 packaging workflow.
- Documentation expanded with canonical targeting-preview and owner-scoped cleanup usage.
