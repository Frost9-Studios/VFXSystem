# VFX Layer 1 Sandbox

This folder is intentionally non-package test scaffolding.

## Purpose
- Quick manual validation for `Frost9.VFX` Layer 1 behavior.
- Uses runtime-generated catalog/config at scene startup.
- Binds `VFXRefs.Effects.VfxPrefab` to one inspector-assigned prefab.

## Setup
1. Create/open a scene under `Assets/_Project/`.
2. Add an empty GameObject named `VfxLayer1Sandbox`.
3. Add component: `Project.VfxSandbox.VfxLayer1SandboxController`.
4. Assign `Effect Prefab` from `Assets/Kyeoms_FX/Prefabs/...` (any effect prefab is fine for now).
5. Press Play.

## Controls
- `LMB`: Spawn at mouse position.
- `1`: Spawn at origin.
- `2`: Spawn radial burst for pooling stress.
- `O`: Spawn on moving attach target.
- `U`: TryUpdate on last handle (color/scale/intensity).
- `S`: Stop last handle.
- `G`: `StopAll()` default (Gameplay channel only).
- `Shift+G`: explicit global stop (`VfxStopFilter.Global`).
- `H`: stale-handle safety check (expected stale stop = false).
- `V`: deterministic pool-reuse verification (logs whether second burst reused pooled instances).

## Inspector Debug Options
- `Show On Screen Overlay`: runtime Canvas stats panel.
- `Show Pool Root In Scene Hierarchy`:
  - Off: pool lives in `DontDestroyOnLoad`.
  - On: pool stays in active scene hierarchy for easier inspection.
- `Enable Periodic Stats Log`: writes pool stats to Console on interval.

## What to verify
- Effects spawn and auto-release.
- Repeated burst spawn reuses instances (stats change smoothly).
- `StopAll()` default does not hard-kill non-gameplay channels.
- Stale handle stop returns `false`, fresh handle stop returns `true`.

Note: The sandbox maps the assigned `Effect Prefab` to `VFXRefs.Effects.VfxPrefab` internally so Layer 1 id-path behavior is still exercised.
