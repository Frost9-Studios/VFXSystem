# Frost9.VFX

Reusable Unity VFX package with catalog-driven playback, pooled instances, and service-first APIs.

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
