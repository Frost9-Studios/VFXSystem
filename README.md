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

## Data Model
- `VfxId` identifiers.
- `VfxCatalog` / `VfxCatalogEntry` as source of truth.
- `VfxParams` typed parameter struct.
- `VFXRefs` generated-style ids under `Runtime/Generated/`.

## Pooling and Safety
- Per-id pooling via `VfxPoolManager`.
- Versioned handles (`VfxHandle`) prevent stale-handle control of recycled instances.
- Runner contract: `IVfxPlayable`.
- Default runner: `PrefabVfxPlayable`.
- Warn-once safety logging for missing ids/prefabs and runtime limits.

## Current Status
- Layer 0 complete: package scaffolding and assembly setup.
- Layer 1 complete: core runtime, pooling, handle safety, baseline tests.
- Layer 2 pending: attach/update specialization and line/arc runner.
- Layer 3 pending: editor validation, ref-generation tooling, diagnostics.
