# RustLike.Presentation

Client-only. Excluded from server builds via the `!UNITY_SERVER` constraint
in the asmdef.

> **Status:** placeholder — first content lands in Phase 1 (player camera) and
> Phase 3 (inventory UI).

## What goes here

- `UI/` — HUD, inventory, build menu, map, vendor, server browser. **One
  Canvas per screen.** Use Sprite Atlas. Pool every list item.
- `Camera/` — first/third-person rigs, ADS state machine.
- `Audio/` — `AudioListener`, voice limit, distance attenuation.
- `VFX/` — particle systems via `PrefabPool`. No `ParticleSystem` instantiated
  ad-hoc.
- `Animation/` — Animator state machines for player/NPC; `Animation Jobs` for
  doors/items.

## Quality presets

| Preset | URP asset | LOD bias | Shadows |
|---|---|---|---|
| Low | `URP-Low.asset` | 0.5 | sun-only, 60 m |
| Mid | `URP-Mid.asset` | 1.0 | sun-only, 100 m |
| High | `URP-High.asset` | 1.5 | sun + 1 spot, 150 m |

URP assets and Quality settings live in `Assets/_Project/Settings/`.

## Don't

- Don't add HDRP. Period.
- Don't enable SSAO/SSR/Bloom by default.
- Don't write per-instance materials. Use `MaterialPropertyBlock`.
- Don't use Update for sim logic — only for camera/UI animation.
