# Project Structure

Каждый модуль = отдельный Assembly Definition (`.asmdef`). Это:
- ускоряет инкрементальную компиляцию,
- запрещает циклические зависимости (компилятор сам ругается),
- позволяет stripping серверной сборки через `defines`/`platforms`.

```
rust/
├── docs/                                # архитектура, roadmap, ADR
├── tools/                               # вспомогательные скрипты (Python/Bash)
└── unity/                               # Unity-проект
    ├── ProjectSettings/
    ├── Packages/                        # com.unity.render-pipelines.universal, FishNet, ...
    └── Assets/
        ├── _Project/
        │   ├── Settings/                # URP assets, Quality, Input, Tags
        │   ├── Scenes/
        │   │   ├── Bootstrap.unity      # стартовая
        │   │   ├── MainMenu.unity
        │   │   └── Game.unity
        │   ├── Prefabs/
        │   │   ├── Player/
        │   │   ├── World/
        │   │   ├── Building/
        │   │   ├── Items/
        │   │   ├── NPC/
        │   │   ├── Vehicles/
        │   │   ├── VFX/
        │   │   └── UI/
        │   ├── Art/
        │   │   ├── Models/{Player,Buildings,Items,NPC,Vehicles,Props}/
        │   │   ├── Materials/
        │   │   ├── Textures/
        │   │   └── Shaders/             # 5–7 shared URP shaders
        │   ├── Audio/{SFX,Music,Voice}/
        │   ├── Animations/
        │   └── Data/                    # ScriptableObjects (item defs, recipes, biomes)
        │
        └── Scripts/
            ├── Core/                    # уровень 0
            │   ├── Bootstrap/           # GameLoop, EntryPoint, ServiceLocator
            │   ├── Events/              # EventBus
            │   ├── Memory/              # Pools, NativeCollections helpers
            │   ├── Time/                # Tick, FixedSim
            │   ├── Logging/             # Log
            │   ├── Math/                # FastMath, Hash, RNG (xorshift)
            │   ├── Streaming/           # AddressablesService
            │   └── RustLike.Core.asmdef
            │
            ├── Net/                     # уровень 1 (FishNet wrapper)
            │   ├── Transport/
            │   ├── Snapshots/           # delta, AOI integration
            │   ├── Prediction/
            │   ├── AOI/                 # area of interest grid
            │   ├── Replication/         # interfaces, base behaviours
            │   └── RustLike.Net.asmdef
            │
            ├── World/                   # уровень 2
            │   ├── Chunks/              # ChunkCoord, ChunkManager, ChunkLoader
            │   ├── Terrain/             # heightmap, mesh gen (Burst)
            │   ├── Biomes/
            │   ├── TimeOfDay/
            │   ├── Weather/
            │   ├── Monuments/
            │   └── RustLike.World.asmdef
            │
            ├── Gameplay/                # уровень 3
            │   ├── Survival/            # Vitals, Damage, Buffs
            │   ├── Inventory/           # ItemDef, Container, Hotbar
            │   ├── Building/            # Blocks, Stability, Privilege
            │   ├── Combat/              # Weapons, Projectiles, Recoil
            │   ├── Crafting/            # Queue, Workbench, Furnace, Recycler
            │   ├── AI/                  # BT, Senses, Tiers
            │   ├── Vehicles/
            │   ├── Electricity/
            │   ├── Farming/
            │   ├── Fishing/
            │   ├── Loot/
            │   ├── Emotes/
            │   ├── Economy/             # vending, trading
            │   ├── Social/               # teams, chat, markers
            │   ├── Progression/         # XP, unlocks, achievements
            │   └── RustLike.Gameplay.asmdef
            │
            ├── Presentation/            # уровень 4 (только client)
            │   ├── UI/                  # HUD, inventory UI, build menu
            │   ├── Camera/
            │   ├── Audio/
            │   ├── VFX/
            │   ├── Animation/
            │   └── RustLike.Presentation.asmdef
            │
            └── Server/                  # серверный entry point
                ├── ServerBootstrap.cs
                ├── Persistence/         # SQLite, save/load
                ├── Admin/               # RCON, console commands
                └── RustLike.Server.asmdef
```

## Assembly Dependencies

```
Core           → (none)
Net            → Core
World          → Core, Net
Gameplay       → Core, Net, World
Presentation   → Core, Net, World, Gameplay
Server         → Core, Net, World, Gameplay
```

## Server stripping

Серверная сборка собирается с define `UNITY_SERVER`. В этой сборке исключаются:
- `RustLike.Presentation` (платформа: только Standalone client),
- тяжёлые Unity модули: `Audio`, `Animation`, `Particles`, `TextMeshPro`,
  `XR`, `VR`, `UI` (через `Project Settings → Player → Strip Engine Code`).

Результат: серверный билд ≈ 80 МБ exe + 200 МБ contents без арта (Addressables
remote bundles).
