# Changelog

All notable changes to this package are documented in this file.

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
