# Frost9.VFX

Reusable Unity VFX package with catalog-driven playback, pooled instances, and service-first APIs.

## Package Scope
- Namespace: `Frost9.VFX`
- Assemblies:
  - `Frost9.VFX` (runtime)
  - `Frost9.VFX.Editor` (editor tooling)
  - `Frost9.VFX.Tests` (tests)

## Dependencies
- Runtime hard dependencies: Unity only.
- No gameplay/project-specific dependencies in package runtime.

## Runtime API
- `IVfxService`
  - `PlayAt(...)`
  - `PlayOn(...)`
  - `Stop(VfxHandle handle)`
  - `StopAll(VfxStopFilter? filter = null)` (safe default: gameplay channel scope)
  - `TryUpdate(VfxHandle handle, in VfxParams parameters)`
  - `GetStats()`
- `VfxManager` static facade for non-DI usage.

## Layer 2 Runtime Behavior
- `PlayOn(...)` supports:
  - `AttachMode.FollowTransform`
  - `AttachMode.FollowPositionOnly`
  - `AttachMode.WorldLocked`
- `PlayOn(...)` returns invalid handle for null/destroyed targets (warn-once).
- Attached instances auto-release safely when target objects are destroyed.
- `VfxPlayOptions.WithIgnoreTargetScale(bool)` is available for attached playback behavior.
- `VfxParams.TargetPoint` is treated as world-space.
- Built-in straight-line preview runner: `LineArcVfxPlayable` (LineRenderer-based).

## Data Model
- `VfxId` identifiers.
- `VfxCatalog` / `VfxCatalogEntry` as source of truth.
- `VfxParams` typed parameter struct.
- `VFXRefs` generated-style ids under `Runtime/Generated/`.

## Editor Tooling
- Catalog validation:
  - inspector button on `VfxCatalog` assets
  - menu item: `Tools/Frost9/VFX/Validate All Catalogs`
  - throttled auto-validation on catalog asset changes
- Deterministic refs generation:
  - menu item: `Tools/Frost9/VFX/Generate VFXRefs`
  - generator sorts by `VfxId` string
  - deterministic identifier sanitization + collision suffixing
  - safe write behavior (temp replace and no-op when content unchanged)

## Pooling and Safety
- Per-id pooling via `VfxPoolManager`.
- Versioned handles (`VfxHandle`) prevent stale-handle control of recycled instances.
- Runner contract: `IVfxPlayable`.
- Default runner: `PrefabVfxPlayable`.
- Warn-once safety logging for missing ids/prefabs and runtime limits.

## Current Status
- Layer 0 complete: package scaffolding and assembly setup.
- Layer 1 complete: core runtime, pooling, handle safety, baseline tests.
- Layer 1 gate tests now include:
  - stale-handle safety after recycle
  - default `StopAll()` gameplay-only scope behavior
  - hard-cap enforcement when pool expansion is disabled
  - reset-on-reuse position correctness
  - fallback auto-release for non-completing runners
  - active-count cleanup when pooled instances are externally destroyed
- Layer 2 complete: attach lifecycle, handle update gates, and line runner.
- Layer 2 gate tests include:
  - null/destroyed target safe-failure for `PlayOn(...)`
  - `FollowPositionOnly` movement behavior
  - target-destroy auto-release for attached effects
  - valid/stale handle behavior for `TryUpdate(...)`
  - line-runner play/update/stop/pool-reuse coverage
- Layer 3A complete: editor validation foundation.
- Layer 3A includes:
  - `VfxCatalogValidator` with structured error/warning output
  - manual validation via catalog inspector button and `Tools/Frost9/VFX/Validate All Catalogs`
  - throttled automatic validation on catalog asset changes
  - validation coverage for duplicate ids, missing prefabs, missing `IVfxPlayable`, and pool/lifetime config sanity
  - editor tests for valid and deliberately broken catalog cases
- Layer 3B complete: deterministic `VFXRefs` generation tooling.
- Layer 3B includes:
  - deterministic source generation from catalog ids
  - stable sort by `VfxId` string (not asset path)
  - deterministic sanitization and collision handling (`_2`, `_3`, ...)
  - safe write with unchanged-content short-circuit
  - editor tests for ordering, sanitization/collision, and unchanged second-run behavior
- Layer 3C pending: diagnostics window.
