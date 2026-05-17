# RustLike.Gameplay

All authoritative game systems. Server-side simulation runs entirely from
this module + `RustLike.World` + `RustLike.Net`.

## Sub-modules

| Folder | Status | Notes |
|---|---|---|
| `Entities/` | ✅ skeleton | sleep/wake tier system |
| `Building/` | ✅ skeleton | block types, privilege, stability graph |
| `Inventory/` | ✅ skeleton | item defs, stacks, containers |
| `Survival/` | ✅ skeleton | vitals, damage |
| `Combat/` | ⏳ Phase 5 | weapons, projectiles, recoil, lag comp |
| `Crafting/` | ⏳ Phase 6 | queue, workbenches, furnace, recycler |
| `AI/` | ⏳ Phase 7 | BT, senses, animals, scientists |
| `Vehicles/` | ⏳ Phase 9 | modular vehicles, fuel |
| `Electricity/` | ⏳ Phase 9 | lazy graph: batteries, switches, turrets |
| `Farming/` | ⏳ Phase 10 | planting, water |
| `Fishing/` | ⏳ Phase 10 | rods, mini-game |
| `Loot/` | ⏳ Phase 8 | loot tables |
| `Emotes/` | ⏳ Phase 10 | wave/clap/dance |
| `Economy/` | ⏳ Phase 10 | vending, trades |
| `Social/` | ⏳ Phase 11 | teams, chat |
| `Progression/` | ⏳ Phase 11 | XP, achievements |

## Conventions

- **Server-authoritative.** Anything that affects health, inventory, position,
  or world state goes through a *Service* class on the server. Clients send
  *intents* via `Reliable` channel; servers validate and broadcast.
- **No singletons.** All services live in the `ServiceLocator`. You can replace
  them in tests via `ServiceLocator.Replace<T>`.
- **Data-driven.** Items, recipes, biomes, loot tables = ScriptableObjects.
- **No GC on hot paths.** Use pools; iterate by `for(int i=0;...)`, not
  `foreach`/`LINQ`.

## Adding a new gameplay system

1. Create `Gameplay/<System>/<System>Service.cs`.
2. Implement `ITickable` if it needs per-tick work.
3. Register in `ServerBootstrap` (or `ClientBootstrap` for client-only).
4. Publish events via `EventBus<T>` for loose coupling.
5. Add a per-folder `README.md` with the same shape as this one.
