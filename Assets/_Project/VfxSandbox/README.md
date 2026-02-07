# VFX Runtime Sandbox

This folder is intentionally non-package test scaffolding.

## Purpose
- Quick manual validation for implemented Frost9.VFX runtime features.
- Uses runtime-generated catalog/config at scene startup.
- Binds:
  - `Effects.VfxPrefab` to one inspector-assigned prefab.
  - `Effects.LinePreview` to a runtime-created `LineArcVfxPlayable` prefab.

## Setup
1. Create/open a scene under `Assets/_Project/`.
2. Add an empty GameObject named `VfxLayer1Sandbox`.
3. Add component: `Project.VfxSandbox.VfxLayer1SandboxController`.
4. Assign `Effect Prefab` from `Assets/Kyeoms_FX/Prefabs/...` (any effect prefab is fine for now).
5. Press Play.

## Controls
- Spawn / Attach
  - `LMB`: `PlayAt(mouse)`
  - `1`: `PlayAt(origin)`
  - `2`: radial burst stress spawn
  - `O`: `PlayOn(target)` with current attach mode
  - `P`: cycle attach mode (`FollowTransform` -> `FollowPositionOnly` -> `WorldLocked`)
  - `I`: toggle `IgnoreTargetScale`
- Line runner
  - `L`: spawn line preview
  - `T`: `TryUpdate` line endpoint to mouse
  - `K`: stop line handle
- Handle / scope
  - `U`: `TryUpdate(last prefab handle)`
  - `S`: `Stop(last prefab handle)`
  - `C`: spawn persistent UI-channel effect
  - `G`: `StopAll()` default (Gameplay only)
  - `Shift+G`: `StopAll(VfxStopFilter.Global)`
- Verification / tooling
  - `H`: stale-handle safety check
  - `V`: deterministic pool-reuse check
  - `R`: reinitialize service
  - `B`: toggle bootstrap mode (`DirectService` / `VContainerHelper`) and reinitialize
  - `M`: toggle overlay visibility

## Inspector Debug Options
- `Show On Screen Overlay`: runtime Canvas stats panel.
- `Show Pool Root In Scene Hierarchy`:
  - Off: pool lives in `DontDestroyOnLoad`.
  - On: pool stays in active scene hierarchy for easier inspection.
- `Enable Periodic Stats Log`: writes pool stats to Console on interval.
- `Bootstrap Mode`: choose direct service startup or VContainer helper startup.

## What to verify quickly
- Effects spawn and auto-release.
- Repeated burst spawn reuses instances.
  - Press `V` and confirm console/overlay reports `CreatedDeltaSecondWave=0`.
- Default stop scope is safe.
  - Press `C`, then `G`.
  - UI handle should remain active (`UI alive ... expected true`).
  - Press `Shift+G` to stop globally.
- Stale handle protection works.
  - Press `H` and confirm stale stop is `false`, fresh stop is `true`.
- Attach behavior changes with mode toggles.
  - Press `P` then `O` repeatedly and observe different follow behavior.
- Line runner updates in place.
  - Press `L`, move mouse, press `T`, endpoint should move.
- Both bootstrap paths work.
  - Press `B` to switch between Direct and VContainer startup; sandbox reinitializes.

Note: The sandbox maps the assigned prefab to the stable id string `Effects.VfxPrefab`, so generated refs changes cannot break sandbox compilation.
