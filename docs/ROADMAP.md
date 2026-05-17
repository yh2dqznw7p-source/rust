# RustLike — Roadmap

Roadmap построен по принципу **"core first, gameplay second, content last"**.
Каждая фаза заканчивается **играбельным билдом**, который можно тестировать.

Каждая задача оценена в человеко‑неделях (`hw`) для одного middle Unity‑программиста.
Для команды 3–4 человека реальные сроки ÷2.

---

## Phase 0 — Foundation (≈ 4 hw)

**Цель: проект собирается, есть headless server, клиент подключается, видит куб.**

- [x] Architecture document.
- [ ] Unity 2022.3 LTS project, URP template, IL2CPP, .NET Standard 2.1.
- [ ] Assembly Definitions per module.
- [ ] FishNet integration, Tugboat transport.
- [ ] CI: GitHub Actions — build client + server для Linux/Windows.
- [ ] `Core` module: Bootstrap, ServiceLocator, EventBus, fixed tick, logging.
- [ ] `Core.Memory` module: pools.
- [ ] `Net` module: transport wrapper, channel constants.
- [ ] Headless server boots, client connects, "hello world" cube replicated.

**Exit criteria**: dedicated server держит 50 пустых клиентов на тике 16 ms.

---

## Phase 1 — World streaming (≈ 5 hw)

**Цель: бесконечный мир из чанков, грузится плавно.**

- [ ] `World.Chunks`: ChunkCoord, ChunkManager, AOI grid.
- [ ] Procedural heightmap (Perlin + Worley + erosion preview).
- [ ] Chunk mesh generation (Burst job).
- [ ] Chunk streaming client/server (Addressables + diff‑state).
- [ ] LRU cache, async loading, pre‑warm по направлению движения.
- [ ] Biome system (3 биома: forest, desert, snow).
- [ ] Time of day + skybox.
- [ ] Player controller (CharacterController, no physics).

**Exit criteria**: ходим по миру 4×4 км, FPS ≥60 на GTX 1050, RAM клиент ≤3 GB.

---

## Phase 2 — Survival core (≈ 3 hw)

- [ ] `Survival.Vitals`: health, hunger, thirst, temperature, radiation, bleeding.
- [ ] `Survival.DamageBus` + headshot, body part hitboxes.
- [ ] Respawn UI, sleeping bag spawn, beach respawn.
- [ ] Day/night temperature curves, biome temperature modifiers.
- [ ] Healing items, bandages, syringes.

**Exit criteria**: игрок умирает от голода / жажды / урона, респаунится.

---

## Phase 3 — Inventory & Items (≈ 3 hw)

- [ ] `Inventory.ItemDef` ScriptableObjects.
- [ ] Container (slots, stacking, split, swap).
- [ ] Hotbar, equipment slots, drag&drop UI (pooled).
- [ ] Loot containers, corpses, drop bags.
- [ ] Quick loot (Shift‑click).
- [ ] World drop entity (item on ground).
- [ ] Persistent inventory (save/load).

**Exit criteria**: 50 типов предметов, drag&drop без GC spikes.

---

## Phase 4 — Building (≈ 6 hw)

- [ ] Build planner ghost preview, snap points.
- [ ] Foundation, wall, floor, doorway, roof, stairs (6 типов).
- [ ] Stability graph (Burst, dirty‑flag).
- [ ] Upgrade tier (wood → stone → metal → HQM).
- [ ] Damage states + repair.
- [ ] Privilege cupboard, authorization list, building blocked zones.
- [ ] Doors, code locks, key locks.
- [ ] Decay timer + persistence.

**Exit criteria**: построить 2‑этажный дом, заапгрейдить, сломать стену С4‑заглушкой.

---

## Phase 5 — Combat (≈ 5 hw)

- [ ] Hitscan + projectile weapons (`IFireMode` interface).
- [ ] Recoil, sway, spread, bullet drop.
- [ ] Reload state machine.
- [ ] Attachments (silencer, scope, laser, mag).
- [ ] Ammo types.
- [ ] Server‑side hit registration with rewind (lag compensation).
- [ ] Melee weapons + tools (axe, pickaxe, hammer).
- [ ] Explosives: C4, satchel, rocket (raid mechanics).
- [ ] Killfeed.

**Exit criteria**: PvP playable, нет рассинхронов на ping 100 ms.

---

## Phase 6 — Crafting & Workbenches (≈ 3 hw)

- [ ] Crafting queue, blueprints.
- [ ] Workbench tiers (T1/T2/T3) + radius unlocks.
- [ ] Furnace smelting, fuel sim.
- [ ] Recycler.
- [ ] Research table.
- [ ] Repair bench.

---

## Phase 7 — AI & NPCs (≈ 4 hw)

- [ ] Behaviour tree micro‑lib.
- [ ] Animals (chicken, boar, bear, wolf) с senses.
- [ ] Scientists (peacekeeper / hostile).
- [ ] Patrol nodes, group behaviour.
- [ ] AI tier system (Active/Reduced/Sleeping).
- [ ] Loot tables.

---

## Phase 8 — Monuments, Loot, Caves (≈ 4 hw)

- [ ] Monument prefab system (4×4 chunk groups).
- [ ] 5 базовых монументов (gas station, supermarket, lighthouse, harbor, satellite).
- [ ] Loot containers с tier‑таблицами.
- [ ] Caves (separate y‑layer streaming).
- [ ] Roads, rivers, oceans.
- [ ] Underground tunnels (опционально, late).

---

## Phase 9 — Vehicles & Electricity (≈ 4 hw)

- [ ] Modular vehicle: chassis + module slots.
- [ ] Cars, boats; fuel system; storage.
- [ ] Electricity sim (lazy graph): batteries, solar, switches, turrets, smart doors.

---

## Phase 10 — Misc gameplay (≈ 3 hw)

- [ ] Farming: planting, growing, water, fertilizers.
- [ ] Fishing rod + mini‑game.
- [ ] Weather: rain, fog, wind, storms (URP shader cheap).
- [ ] Emotes (wave/clap/dance/sit/point/surrender).
- [ ] Vending machines, player shops.

---

## Phase 11 — Social & Progression (≈ 2 hw)

- [ ] Teams, clans, sleeping bag spawn list.
- [ ] Chat (text), voice chat ready hook.
- [ ] Player markers, team UI.
- [ ] XP, unlock tree, achievements, stats.

---

## Phase 12 — Polish & Optimization (≈ 4 hw)

- [ ] Профайлинг, фикс GC spikes.
- [ ] Aggressive culling proximity profiler.
- [ ] Quality presets (Low/Mid/High) + auto‑detect.
- [ ] Input remap, audio settings, accessibility.
- [ ] Localization framework (RU/EN minimum).
- [ ] Anti‑cheat hooks (EAC/BattlEye SDK ready).
- [ ] Server admin tools, RCON.
- [ ] Stress test 100 клиентов в LAN.

---

## Phase 13 — Content & Live (continuous)

- [ ] Map seeds, monument variants.
- [ ] Item rebalance.
- [ ] Server browser, master list.
- [ ] Steam integration (если требуется).

---

## Total estimate
≈ **50 hw для одного программиста** до бета‑качества.
Команда 3 программиста + 1 геймдиз + 1 артист = **3.5–4.5 месяцев** до closed beta.
