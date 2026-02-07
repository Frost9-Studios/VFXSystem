# Frost9.VFX

Reusable Unity VFX package with catalog-driven playback, pooled instances, and service-first APIs.

## Package Scope
- Namespace: `Frost9.VFX`
- Assemblies:
  - `Frost9.VFX` (runtime)
  - `Frost9.VFX.Editor` (editor tooling)
  - `Frost9.VFX.Tests` (tests)

## Dependencies
- Core runtime APIs (`IVfxService`, `VfxService`, `VfxManager`) depend on Unity APIs and package runtime assets only.
- No gameplay/project-specific dependencies in package runtime.
- Current package distribution includes VContainer integration in runtime assembly, so `jp.hadashikick.vcontainer` is required.

## Runtime API
- `IVfxService`
  - `PlayAt(...)`
  - `PlayOn(...)`
  - `Stop(VfxHandle handle)`
  - `StopAll(VfxStopFilter? filter = null)` (safe default: gameplay channel scope)
  - `TryUpdate(VfxHandle handle, in VfxParams parameters)`
  - `GetStats()`
- `VfxManager` static facade for non-DI usage.

## High-Level Usage
1. Author a `VfxCatalog` with ids and prefabs.
2. Bootstrap one `IVfxService` (manual, static facade, or DI).
3. Spawn effects from gameplay presentation with ids (not raw prefab references).
4. Keep handles only for effects you need to update/stop.
5. Use scoped stop filters (`Gameplay`, `Owner`, `Id`) instead of global stop by default.

```csharp
// one-shot
var handle = vfxService.PlayAt(VFXRefs.Effects.VfxPrefab, worldPoint);

// optional update
vfxService.TryUpdate(handle, VfxParams.Empty.WithScale(1.2f));

// explicit stop
vfxService.Stop(handle);
```

## Targeting Preview Pattern
Targeting line/arc visuals should be owned by your targeting system loop, not by core gameplay logic.

```csharp
// Begin aiming (spawn once)
previewHandle = vfxService.PlayOn(
    previewId, // e.g. Effects.LinePreview
    casterGameObject,
    AttachMode.FollowPositionOnly,
    VfxParams.Empty.WithTargetPoint(initialAimPoint),
    VfxPlayOptions.DefaultGameplay
        .WithAutoRelease(false)
        .WithOwner(casterGameObject));

// During aiming (every frame)
vfxService.TryUpdate(
    previewHandle,
    VfxParams.Empty.WithTargetPoint(currentAimPoint));

// Confirm or cancel
vfxService.Stop(previewHandle);
previewHandle = VfxHandle.Invalid;
```

## Owner-Scoped Cleanup Example
```csharp
var stopped = vfxService.StopAll(
    VfxStopFilter.GameplayDefault
        .WithOwner(casterGameObject));
```

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
- `VFXRefs` generated-style ids at `Assets/Resources/VFX/VFXRefs.cs` (outside package).
- `VfxCatalogEntry.Id` is a string id (for example `Effects.FireballImpact`), not a numeric id.

## Editor Tooling
- Catalog validation:
  - inspector button on `VfxCatalog` assets
  - menu item: `Tools/Frost9/VFX/Validate All Catalogs`
  - throttled auto-validation on catalog asset changes
- Deterministic refs generation:
  - menu item: `Tools/Frost9/VFX/Generate VFXRefs`
  - scans all project `VfxCatalog` assets via `AssetDatabase.FindAssets("t:VfxCatalog")`
  - fixed output path: `Assets/Resources/VFX/VFXRefs.cs`
  - output folder is auto-created when missing
  - generator sorts by `VfxId` string
  - deterministic identifier sanitization + collision suffixing
- safe write behavior (temp replace and no-op when content unchanged)

## VContainer Integration (Optional)
- Namespace: `Frost9.VFX.Integration.VContainer`
- Helper methods:
  - `RegisterVfx(...)`
  - `RegisterVfxWithExistingPool(...)`
- Behavior:
  - Audio-style DI wiring (`IVfxService` is container-constructed, not manually `new`'d in registration).
  - `dontDestroyOnLoad` is exposed and defaults to `true`.
  - Pool manager creation is `create-or-reuse` by GameObject name to prevent duplicates.

```csharp
using Frost9.VFX.Integration.VContainer;
using VContainer;

public override void Configure(IContainerBuilder builder)
{
    builder.RegisterVfx(
        catalog: gameplayCatalog,
        configuration: gameplayVfxConfig,
        poolManagerObjectName: "VFXSystem_PoolManager",
        dontDestroyOnLoad: true);
}
```

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
- Layer 3C complete: diagnostics window.
- Layer 3C includes:
  - read-only diagnostics window: `Tools/Frost9/VFX/Diagnostics`
  - play-mode aware status messaging when runtime stats are unavailable
  - per-pool-manager summary and per-id stats table
  - throttled polling to keep editor overhead low
  - editor tests for collector status and window open behavior
- Layer 3D complete: optional VContainer helper registration.
- Layer 3D includes:
  - guarded integration under `Runtime/Integration/VContainer/`
  - `RegisterVfx(...)` and `RegisterVfxWithExistingPool(...)`
  - default `DontDestroyOnLoad` behavior with pool-manager reuse by name
  - runtime integration tests for reuse/default/existing-pool registration paths
