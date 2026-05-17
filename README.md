# RustLike

Multiplayer survival sandbox inspired by Rust. Unity 2022.3 LTS + URP.

**Optimization priority:** FPS → RAM → Network → Gameplay → Graphics.
**Targets:** 60 FPS / 8 GB RAM on a GTX 1050‑class PC, dedicated server with 100 players on a 4 vCPU VPS.

> Status: **Phase 0 — foundation skeleton.** Architecture, project structure
> and core systems are in place. Most gameplay modules are interfaces + minimal
> data structures, ready to be filled in following the roadmap.

## Repository layout

```
rust/
├── docs/                   ARCHITECTURE.md, ROADMAP.md, PROJECT_STRUCTURE.md
├── tools/                  helper scripts (placeholder)
└── unity/                  Unity project root
    ├── Assets/Scripts/     Core, Net, World, Gameplay, Presentation, Server
    ├── Packages/           manifest.json with FishNet, URP, Addressables
    └── ProjectSettings/
```

Read the docs in this order:
1. [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — full design + answers to the
   10 design questions (RAM/FPS/networking/AI/...).
2. [docs/ROADMAP.md](docs/ROADMAP.md) — phased plan (50 dev‑weeks to beta).
3. [docs/PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) — folders and asmdefs.

## Module overview

| Module | Status | Purpose |
|---|---|---|
| `RustLike.Core` | ✅ Done | Bootstrap, GameLoop, ServiceLocator, EventBus, SimClock, Pools, Streaming, FastRng |
| `RustLike.Net` | ✅ Skeleton | Channels, INetTransport, BitWriter/Reader, AOI grid, Snapshot builder |
| `RustLike.World` | ✅ Skeleton | ChunkCoord, ChunkManager, ChunkProvider, PlayerAnchor |
| `RustLike.Gameplay` (Entities) | ✅ Skeleton | SimTier, ISimEntity, SimEntityRegistry |
| `RustLike.Gameplay` (Building) | ✅ Skeleton | Block types, Privilege, Stability graph, BuildingService |
| `RustLike.Gameplay` (Inventory) | ✅ Skeleton | ItemDef SO, ItemStack, Container, Registry |
| `RustLike.Gameplay` (Survival) | ✅ Skeleton | Vitals struct, DamageInfo, VitalsService |
| `RustLike.Gameplay` (Combat, Crafting, AI, Vehicles, Electricity, ...) | ⏳ Phases 5+ | Roadmapped |
| `RustLike.Presentation` | ⏳ Phases 1+ | UI, camera, audio, VFX, animation |
| `RustLike.Server` | ⏳ Phase 0 final | ServerBootstrap, persistence, RCON |

Each module has its own `README.md` next to the asmdef.

## Building

You'll need:
- **Unity 2022.3.40f1** (LTS).
- A GitHub account to pull FishNet via the manifest.

To open: clone the repo, then point Unity Hub at the `unity/` directory.

For a server build:
- Player Settings → Server Build = ON.
- Define `UNITY_SERVER`.
- Strip Engine Code = High.
- Use `-batchmode -nographics -server` at runtime.

## Performance budgets (Low quality, 1080p)

| Metric | Client | Server (100 players) |
|---|---|---|
| RAM | ≤ 5.5 GB | ≤ 5.5 GB |
| Main‑thread frame | ≤ 12 ms | tick ≤ 16 ms |
| GPU frame | ≤ 14 ms | — |
| Draw calls | ≤ 1500 | — |
| Bandwidth | — | ≤ 25 Mbit/s total |

## License

MIT (see `LICENSE` once added).
