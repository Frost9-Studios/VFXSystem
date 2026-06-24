# Frost9.VFX

Reusable Unity VFX package with catalog-driven playback, pooled instances, and service-first APIs.

## Requirements
- Unity 6.3 (`6000.3`) or newer. Developed and tested on `6000.3.9f1`.
- VContainer `1.17.0` (optional integration helpers are compiled only when VContainer is present).

## Runtime Surface
- `IVfxService`:
  - `PlayAt(...)`
  - `PlayOn(...)`
  - `TryUpdate(...)`
  - `Stop(...)`
  - `StopAll(...)`
  - `GetStats()`
- `VfxManager`: static fallback facade.
- Catalog-driven ids: `VfxId`, `VfxCatalog`, generated `VFXRefs`.
- `VfxCatalog` read-only API (no `UnityEditor`): `Count`, `Ids`, `Contains(VfxId)`, `TryGetEntry(...)`, `Entries`.

## Generated Refs Output
- Generate ids from catalogs: `Tools/Frost9/VFX/Generate VFXRefs`.
- Catalog scan scope: all project assets matching `t:VfxCatalog`.
- Output path is fixed: `Assets/Resources/VFX/VFXRefs.cs`.
- Output folders are created automatically if missing.
- `Assets/Resources/VFX/Frost9.VFX.Generated.asmref` routes the generated file into the `Frost9.VFX` assembly.

## Catalog Entry Authoring
- `Id`: string identifier (`VfxId`). Use a stable namespaced format like `Effects.FireballImpact`.
- `Prefab`: effect prefab that can be played by the service (typically includes `PrefabVfxPlayable` or another `IVfxPlayable` runner).
- `Initial Pool Size`: prewarm count.
- `Max Pool Size`: hard cap.
- `Allow Pool Expansion`: whether runtime can allocate above prewarm count (up to max).
- `Default Channel`: fallback channel when call-site play options do not override it.
- `Auto Release By Default`: default release behavior for spawned instances.
- `Fallback Lifetime Seconds`: safety auto-release timeout for runners that do not signal completion.
- `Default Parameters`: typed parameter defaults merged with call-site overrides.

## Editor Authoring APIs (`Frost9.VFX.Editor`)
Reusable editor plumbing for building project-side authoring tools without reflection, mutating private serialized lists, parsing generated C#, or simulating menu clicks. Editor-only; the runtime assembly stays free of `UnityEditor`.

```csharp
using Frost9.VFX.Editor;

// 1) Mutate a catalog (SerializedObject-based; Undo + dirty handled).
//    Prefab-only updates preserve every tuned entry setting.
VfxCatalogEditing.AddOrUpdate(catalog, new VfxId("Effects.Fireball"), fireballPrefab);
VfxCatalogEditing.Remove(catalog, new VfxId("Effects.Old"));

// Batch many edits, applied once on dispose:
using (var batch = VfxCatalogEditing.BeginBatch(catalog))
{
    batch.AddOrUpdate(idA, prefabA);
    batch.AddOrUpdate(idB, prefabB);
}

// Synchronize toward a desired set. Stale entries are reported, and only
// removed when removeMissing is true. Conflicts/errors apply no changes.
var sync = VfxCatalogEditing.Sync(
    catalog,
    desired: new[] { (idA, prefabA), (idB, prefabB) },
    removeMissing: false);
// sync.Added / Updated / Unchanged / Stale / Removed / Conflicts / Errors

// 2) Discover ids across the project (or scoped to one catalog).
var project = VfxCatalogDiscovery.DiscoverProject();   // project-known ids + provenance + conflicts
var scoped  = VfxCatalogDiscovery.DiscoverCatalog(catalog);

// 3) Validate programmatically (no dialogs/logging contract).
var report    = VfxCatalogValidation.ValidateCatalog(catalog);
var aggregate = VfxCatalogValidation.ValidateAllProjectCatalogs();

// 4) Generate VFXRefs directly (returns Changed + structured conflicts/warnings).
var gen = VfxRefsGenerator.GenerateFromProject();

// 5) Share the exact generator sanitization / collision rules.
var identifier = VfxIdentifierSanitizer.Sanitize("Fire Ball");   // "Fire_Ball"
var analysis   = VfxIdentifierAnalysis.Analyze(rawIds);          // collision groups + generated names

// 6) Ensure a prefab has a valid IVfxPlayable runner (idempotent).
var ensure = VfxPrefabAuthoring.EnsurePlayable(prefabAsset);     // Changed / Unchanged / Error
```

- **Searchable `VfxId` drawer:** fields typed as `VfxId` render as a searchable dropdown of project-known ids (with explicit None, a manual-entry escape hatch, and warnings for missing/conflicting ids). The decision logic is exposed as the reusable `VfxIdDrawerModel`.
- **Project-known vs runtime-valid:** `DiscoverProject` returns ids authored anywhere in the project, not ids guaranteed playable by a specific `IVfxService` (a service binds one catalog). Use `DiscoverCatalog` to scope to a registered catalog.

## Quick Start (Direct Service)
```csharp
using Frost9.VFX;
using UnityEngine;

public sealed class GameVfxBootstrap : MonoBehaviour
{
    [SerializeField] private VfxCatalog catalog;
    [SerializeField] private VfxSystemConfiguration configuration;

    private IVfxService vfx;

    private void Awake()
    {
        var poolRoot = new GameObject("Game_VFXPoolManager");
        var poolManager = poolRoot.AddComponent<VfxPoolManager>();
        vfx = new VfxService(poolManager, catalog, configuration);
    }

    private void OnDestroy()
    {
        vfx?.Dispose();
    }
}
```

## Canonical Usage Pattern
```csharp
// 1) One-shot at position
var hit = vfx.PlayAt(VFXRefs.Effects.VfxPrefab, hitPoint);

// 2) Attached effect
var aura = vfx.PlayOn(
    VFXRefs.Effects.VfxPrefab,
    target,
    AttachMode.FollowTransform,
    VfxParams.Empty.WithLifetimeOverride(2f));

// 3) Optional runtime update
vfx.TryUpdate(aura, VfxParams.Empty.WithScale(1.2f));

// 4) Explicit stop
vfx.Stop(aura);
```

## Targeting Preview Pattern (Opinionated)
Use your game's targeting system to own the loop. VFX only renders the preview.

```csharp
// Start aiming: spawn once, keep handle alive
previewHandle = vfx.PlayOn(
    previewId,                        // e.g. Effects.LinePreview in your catalog
    casterGameObject,
    AttachMode.FollowPositionOnly,
    VfxParams.Empty.WithTargetPoint(initialWorldAimPoint),
    VfxPlayOptions.DefaultGameplay
        .WithAutoRelease(false)
        .WithOwner(casterGameObject));

// While aiming (every frame)
vfx.TryUpdate(previewHandle, VfxParams.Empty.WithTargetPoint(currentWorldAimPoint));

// Confirm/cancel
vfx.Stop(previewHandle);
previewHandle = VfxHandle.Invalid;
```

## Owner-Scoped Cleanup
```csharp
// Stop only effects owned by one gameplay object
var stopped = vfx.StopAll(
    VfxStopFilter.GameplayDefault
        .WithOwner(casterGameObject));
```

## VContainer Integration
`com.frost9.vfx` includes optional registration helpers in `Frost9.VFX.Integration.VContainer`.

```csharp
using Frost9.VFX.Integration.VContainer;
using VContainer;

public override void Configure(IContainerBuilder builder)
{
    builder.RegisterVfx(
        catalog: gameplayCatalog,
        configuration: gameplayVfxConfig,
        poolManagerObjectName: "Game_VFXPoolManager",
        dontDestroyOnLoad: true);
}
```

## Notes
- `StopAll()` default scope is **Gameplay** channel only.
- Global stop is explicit: `StopAll(VfxStopFilter.Global)`.
- If `PlayOn(...)` target is null/destroyed, it fails safely and returns `VfxHandle.Invalid`.
