# How to run RustLike (Phase 0 playable demo)

The current build is a **playable Phase 0 demo**: walk around a small playground
with a survival HUD bound to the real `VitalsService`. There is no networking,
inventory UI, or building yet — those land in Phases 3–5.

## Requirements

- **Unity 2022.3.40f1 LTS** (older 2022.3.x versions also work).
- ~3 GB free disk for first import (URP + Burst + Addressables).

## Steps

1. **Clone** the repo and open Unity Hub.
2. Click **Add → Add project from disk**, point at the `unity/` subfolder.
3. Open the project. First import takes ~3–5 minutes (Burst compiles, URP
   imports). Lots of yellow warnings are normal — they go away after import.
4. Once the Editor is open, just press **Play**.
   - You don't need to open any scene file. The default empty scene is enough;
     `RuntimeInitializeOnLoadMethod` builds the playground at startup.
5. Click on the Game view to capture the cursor and start playing.

## Controls

| Key | Action |
|---|---|
| **WASD** | Walk |
| **Shift** | Sprint (drains hunger) |
| **Space** | Jump |
| **LeftCtrl** | Crouch |
| **Mouse** | Look |
| **Esc** | Release cursor (then click to recapture) |

## What you should see

- Grey ground plane, blue sky, soft fog.
- A red pillar near origin, ring of cubes/cylinders around the spawn.
- Bottom-left HUD: `HP / Hunger / Thirst / Temp` bars.
- Top-left: controls hint. Top-right: build mode tag (`mode=Client`).
- Hunger and thirst slowly tick down at 1 Hz; sprint nibbles HP.
  Stand around long enough → HP hits 0 → "YOU DIED" overlay.

## Building a standalone player

`File → Build Settings → Add Open Scenes` (the empty default scene is fine,
the bootstrap doesn't need anything in it).
Pick your platform, **Build**.

For Linux/headless server builds (Phase 0 close-out), see `docs/ROADMAP.md`.

## Troubleshooting

- **"Multiple AudioListeners"** warning on first Play — refresh, the
  `ClientBootstrap` removes the default scene's listener at startup.
- **No URP material** on cubes — that's expected; `MaterialPropertyBlock` sets
  both `_BaseColor` (URP) and `_Color` (built-in) so the demo looks the same in
  either pipeline.
- **Console: "ServiceLocator service not registered: VitalsService"** — means
  the script execution order is off (very rare). Just hit Play again.
