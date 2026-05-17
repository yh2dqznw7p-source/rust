# RustLike — Architecture Overview

> Multiplayer survival sandbox inspired by Rust. Unity 2022.3 LTS + URP.
> Hard target: smooth gameplay on a 4‑core CPU, 8 GB RAM, GTX 1050‑class GPU,
> with a dedicated server hosting **100 players** on a single mid‑tier VPS.
>
> Optimization priorities (in order): **FPS → RAM → Net → Gameplay → Graphics.**

---

## 1. Полная архитектура проекта

Проект разделён на изолированные модули (Unity Assembly Definitions), общающиеся
через интерфейсы и шину событий. Никаких "god scripts".

```
┌──────────────────────────────────────────────────────────────────────┐
│                              CLIENT                                  │
│  Presentation (URP, UI, Audio, VFX, Input)                           │
│  ──────────────────────────────────────────                          │
│  Gameplay (Building, Inventory, Survival, Combat, Vehicles, ...)     │
│  ──────────────────────────────────────────                          │
│  World (Chunks, Streaming, Biomes, Weather, Time)                    │
│  ──────────────────────────────────────────                          │
│  Net (FishNet transport, Snapshots, Prediction, AOI)                 │
│  ──────────────────────────────────────────                          │
│  Core (Bootstrap, ServiceLocator, EventBus, Pools, Tick, Logging)    │
└──────────────────────────────────────────────────────────────────────┘
                                   ▲
                                   │  reliable + unreliable channels
                                   ▼
┌──────────────────────────────────────────────────────────────────────┐
│                          DEDICATED SERVER                            │
│  Same modules as client, but headless build flag strips:             │
│   - URP, UI, VFX, Audio, Animations (server uses cheap proxies)      │
│  Authoritative simulation: World, Net, Gameplay, AI, Survival        │
└──────────────────────────────────────────────────────────────────────┘
```

### Слои (нижний знает только о соседе ниже):

| Layer        | Знает о...                       | Не знает о...                   |
|--------------|----------------------------------|---------------------------------|
| Core         | —                                | всём остальном                  |
| Net          | Core                             | Gameplay, World, Presentation   |
| World        | Core, Net (через интерфейсы)     | Gameplay, Presentation          |
| Gameplay     | Core, Net, World                 | Presentation                    |
| Presentation | всё (только для отображения)     | —                               |

### Поток данных за 1 тик сервера (20 Hz):
1. **Net.Receive** → распаковка инпутов клиентов в lock‑free очередь.
2. **Sim.Tick(dt=50ms)** — фиксированный шаг:
   - Survival → Combat → Building → Vehicles → AI → Physics → Loot.
   - Все системы получают только relevant entities (через AOI grid).
3. **World.UpdateChunks** — выгрузка/загрузка чанков по player AOI.
4. **Net.BuildSnapshots** — per‑client delta snapshots, prio‑queue, MTU‑aware.
5. **Net.Send** — батчинг + сжатие.

### ECS/DOTS: где и почему
DOTS используем **точечно**, только там где это даёт измеримый прирост и не
убивает productivity:
- Расчёт стабильности построек (граф из тысяч блоков).
- Bulk simulation у пуль/осколков.
- AI sensors (raycast batched).
- Foliage / decals culling.

Всё остальное — обычный MonoBehaviour + Burst‑методы по необходимости. Это
сознательный trade‑off: гибридный подход быстрее в разработке и достаточен
для целевого железа.

---

## 2. Какие системы будут самыми тяжёлыми

| Подсистема                      | Узкое место              | Митигация                                                   |
|---------------------------------|--------------------------|-------------------------------------------------------------|
| Building (стабильность)         | Граф из 10k+ блоков      | Burst job, грязный флаг, инкрементальный пересчёт           |
| Networking (snapshots×100 игр.) | CPU + bandwidth          | AOI grid, delta + zigzag varint, prio queue, dirty mask     |
| Terrain rendering               | Draw calls, vertex count | Chunked mesh, GPU instancing, aggressive LOD, baked LM      |
| Vegetation                      | GPU + CPU                | GPU‑driven foliage (Indirect), 2 LOD + impostor             |
| Physics                         | Rigidbodies              | Sleep + kinematic proxies, не‑PhysX для большинства         |
| AI pathfinding                  | A* при 200+ NPC          | NavMesh tile streaming, hierarchical paths, async jobs      |
| Loot/inventory UI               | GC при drag&drop         | Object pool UI, no LINQ, structs, no string concat          |
| Particles / VFX                 | Overdraw                 | Pooled FX, low particle count, no soft particles            |
| Decals (пулевые отверстия)      | Память + draw            | Ring buffer 256 шт. + batched URP decals                    |
| Audio                           | Источники                | Voice limit + priority, distance culling                    |

---

## 3. Как уменьшить использование RAM

**Бюджет клиента: 6 GB активной памяти, 2 GB буфер OS.**
Бюджет сервера: 5–6 GB на 100 игроков.

1. **Addressables + asset streaming** — текстуры/меши/звуки грузятся по чанкам,
   автоматически unload по reference count.
2. **Texture compression** — ASTC 6×6 на мобилку‑классе, BC7/BC5 на PC.
   Albedo 1024², detail 512², normal 512² (BC5). Запрет на 4K вне skybox.
3. **Mesh sharing** — одинаковые блоки/предметы — один shared mesh, GPU instancing.
4. **Shared materials** — Material Property Block вместо новых материалов.
5. **Object pooling** — пули, гильзы, частицы, decals, UI элементы инвентаря,
   tooltips, loot slots, damage numbers — всё из пулов.
6. **Chunk streaming мира** — в памяти держим radius=384 м, остальное unload.
   Soft cache (LRU) на 32 чанка.
7. **Sleeping entities** — sleeping players, dropped items, неактивные базы
   хранятся как POD‑структуры (struct, no MB), просыпаются при подходе игрока.
8. **No allocations в горячем пути** — `List<T>` reuse, `StringBuilder` pool,
   `Span<T>` для парсинга, structs over classes для tick‑data.
9. **Audio streaming** — long clips streaming, short clips DecompressOnLoad.
10. **Server stripping** — `#if !UNITY_SERVER` исключает UI/VFX/Audio/Anim
    из серверной сборки → −1.5 GB RAM минимум.

---

## 4. Как добиться нормального FPS на слабом железе

Цель: **60 FPS @ 1080p Low**, **45 FPS @ 1440p Low**, **90+ FPS @ 720p**.

- **URP Forward+**, без HDRP. Single‑pass pipeline.
- **Только baked lighting** для статики + 1 directional sun (cascaded).
  Realtime shadows только у solnце на расстоянии ≤80 м, без shadow на foliage.
- **No SSAO/SSR/Bloom/DOF по умолчанию**. Опционально только bloom (cheap).
- **Aggressive occlusion culling** + portals у больших монументов и пещер.
- **LOD bias 0.5 на Low**. У всех моделей 3 LOD + крошечный billboard.
- **GPU Instancing + SRP Batcher** — материалы compatible, без Per‑Object data.
- **Texture streaming** включён, mip bias +1 на Low.
- **VFX**: ParticleSystem с sub‑emitters запрещены. Один shared decals batch.
- **Animation**: Animator только для игроков/NPC; для дверей/предметов —
  Animation jobs или прямой transform tween.
- **Shaders**: 5 кастомных URP Lit‑lite шейдеров под весь проект.
- **UI**: одна Canvas на экран, без overdraw, atlas через Sprite Atlas.
- **Frame pacing**: `Application.targetFrameRate` + `QualitySettings.vSyncCount`.

---

## 5. Как организовать networking

**Транспорт: FishNet** (бесплатный, mature, лучшие prediction/AOI tools для
Unity, чем NGO для нашей нагрузки).

```
  Client                                Server
   ▲ │  CommandPacket (input, 30Hz)      │
   │ └────────────► [AOI grid] ──► Sim ──┤
   │                                     │
   │      DeltaSnapshot (20Hz, prio)     ▼
   └───────────────── [build snapshot per client] ◄── Sim state
```

Слои Net‑модуля:
- **Transport**: FishNet Tugboat (UDP).
- **Channels**: `Reliable` (chat, building place, inventory ops), `Unreliable`
  (movement, snapshots), `ReliableSequenced` (chunk loads).
- **Snapshots**: компонент `INetworked` сериализуется через codegen
  (`NetworkBehaviour.SyncVar` + `Replicate/Reconcile`).
- **AOI (Area of Interest)**: 64 м grid; игроку шлются только сущности из
  3×3 ячеек вокруг него.
- **Prediction & Reconciliation**: built‑in FishNet PredictionV2 для движения
  и стрельбы. Lag compensation — server rewinds hitbox histories на ping ms.
- **Compression**: varint, half‑precision позиции (сетка 0.01 м), bit‑packed
  rotations (smallest‑three).
- **Snapshot priority**: ближе → чаще; локальный игрок и его команда —
  всегда; sleeping entities — никогда.
- **Reconnect**: state token + chunk‑resync при reconnect ≤60s.

---

## 6. Как оптимизировать сервер под 100 игроков

Цель CPU: **≤16 ms тик** (20 Hz) на 4 vCPU.

1. **Headless build** (`-batchmode -nographics`), no rendering, no audio.
2. **Fixed tick 20 Hz** для симуляции; physics tick 30 Hz; AI tick 5–10 Hz.
3. **Job System + Burst** для тяжёлых батчей (raycasts, AI senses,
   stability recompute, AOI rebuild).
4. **AOI grid** (uniform 64 м) — все системы итерируются по ячейкам, а не
   по всем сущностям.
5. **Entity sleep**: на сервере 80% сущностей спит. Wake by trigger‑only.
6. **Dirty‑flag everywhere**: snapshot и stability работают только по
   изменённым объектам.
7. **No reflection, no LINQ** в горячем пути.
8. **Networking I/O в отдельном потоке** (FishNet это умеет).
9. **DB**: SQLite (одиночный сервер) или Postgres (кластер). Запись —
   write‑behind, fsync раз в 30 с + при graceful shutdown.
10. **Метрики**: встроенный Prometheus exporter (тик‑время, RAM, msg/s, ent count).

Ожидаемый бюджет на тик при 100 игроках:
```
Net read         1.5 ms
Input apply      0.5 ms
Survival         0.8 ms
Combat / hits    1.5 ms
Building (dirty) 0.7 ms
AI (5Hz)         2.0 ms
Physics          2.5 ms
World / chunks   0.5 ms
Snapshots build  3.5 ms
Net send         1.0 ms
─────────────────────────
Total           ≈14.5 ms  (50 ms бюджет → запас ~3×)
```

---

## 7. Какие механики лучше делать server-side

**Authoritative server, dumb client**. Клиент только предсказывает.

| Server‑side (authoritative)                 | Client‑side (cosmetic / predicted)    |
|---------------------------------------------|---------------------------------------|
| Здоровье, голод, жажда, температура         | Анимация ран, UI HUD                  |
| Урон, попадания, headshots, bleeding        | Muzzle flash, гильзы, decals          |
| Инвентарь, крафт, queue                     | Drag&drop preview, tooltips           |
| Постройки: place / upgrade / repair / decay | Preview‑призрак при размещении        |
| Privilege cupboard, authorized list         | UI панели                             |
| Двери, замки, ключи, коды                   | Звук открытия                         |
| Электричество (если упрощённое)             | LED‑индикаторы                        |
| AI behaviour, лут, спавн                    | Animator/IK                           |
| Транспорт: позиция, fuel, owner             | Колёса, выхлоп                        |
| Погода, время суток                         | Облака, постпроцесс                   |
| Spawn / respawn / sleeping bag              | Камера, переходы                      |
| Экономика, vending machines, торговля       | UI магазина                           |

---

## 8. Как уменьшить нагрузку от построек

Постройки — **главная RAM/CPU/Net нагрузка** в Rust‑like играх.

1. **Block = struct**, не GameObject. Визуал создаётся только при стриминге.
2. **Per‑base aggregate GameObject** — одна база = один MeshCombiner результат
   на LOD2/LOD3 (для рендеринга издалека).
3. **Stability — incremental**: dirty‑flag, BFS только от изменённого узла.
   Burst job, расчёт раз в 0.5 с после последнего изменения.
4. **Decay** — ленивый: timer на блок, проверка при visit чанка, не каждый тик.
5. **Privilege cupboard zone** — kd‑tree из активных cupboards в чанке.
6. **Шаринг мешей**: foundation/wall/floor/roof — один меш на тип×матерал
   ×состояние повреждения; разница только через MaterialPropertyBlock + GPU
   instancing.
7. **Damage states** — 4 состояния максимум, без real‑time mesh deformation.
8. **Repair** — без pooling новых entity, изменение полей структуры.
9. **Demolish** — server schedules removal, шлёт один пакет "delete‑id".
10. **Net**: постройки sync через **delta‑по‑chunk**, не пер‑объект.
    Игрок далеко → не получает обновления вообще.

Бюджет: **50 000 блоков на сервер** (≈ 100 баз × 500 блоков), памяти ~12 МБ.

---

## 9. Как оптимизировать AI

1. **Tier‑system по дистанции от ближайшего игрока**:
   - `Active` (≤30 м): full AI tick 10 Hz, animator, физика.
   - `Reduced` (30–80 м): tick 2 Hz, без animator, без сложного pathfind.
   - `Sleeping` (>80 м или нет игроков в чанке): полный сон, не тикается.
2. **Behaviour Trees** через лёгкую самописную либу (no GC, struct nodes).
3. **NavMesh tile streaming** по чанкам.
4. **Pathfinding** — асинхронный, batched, hierarchical (грид → детальный).
5. **Senses (зрение/слух)** — Burst raycast jobs, batched на тик.
6. **Shared sensor cache** — несколько NPC в группе делят результаты.
7. **Patrol path** — pre‑baked ноды, без runtime pathfind.
8. **Animator culling**: `AnimatorCullingMode.CullCompletely` на сервере и при
   далёких NPC.
9. **NPC = struct‑heavy** — компонент с BTState, Vitals, Senses; визуал
   spawn‑on‑demand при wake.
10. **Predicate scheduling** — не "проверяй каждый кадр", а "проверь через 0.7 c".

---

## 10. Как организовать streaming мира

**Мир — бесконечная (ограниченная картой) сетка чанков 64×64 м.**

```
ChunkCoord(int x, int z)  →  Chunk
   ├ TerrainPatch (mesh + heightmap)
   ├ BiomeData
   ├ Static decoration (rocks, foliage refs)
   ├ Buildings list (block ids)
   ├ Entities list (NPC, items, vehicles)
   └ NavMesh tile
```

- **Server** — держит все чанки с хотя бы одной "якорной" сущностью
  (sleeping bag, привязанный игрок, активная база). Остальное в БД.
- **Client** — держит чанки в радиусе 6×6 (≈384 м) от игрока + 2 чанка
  предзагрузки по направлению движения.
- **Загрузка**:
  - Heightmap + базовый mesh — загружается синхронно (быстро, мало).
  - Decoration / vegetation / buildings detail — асинхронно через
    Addressables, LOD3 сначала, затем LOD0.
- **Выгрузка**: задержка 5 с после выхода из радиуса (избежать ping‑pong).
- **LRU cache** на 32 чанка в памяти, остальное unload.
- **Procedural seed** — детерминирован, чанк всегда восстанавливается из
  seed + diff (изменения, постройки, копание).
- **Monuments/RT** — пре‑скомпонованные prefab‑чанки 4×4, грузятся одной пачкой.
- **Caves / underground** — отдельный chunk‑layer (y‑axis), грузится только при
  входе в zone trigger.

---

## Бюджеты (целевые на Low quality, 1080p)

| Метрика                          | Клиент       | Сервер (100p) |
|----------------------------------|--------------|---------------|
| RAM (working set)                | ≤ 5.5 GB     | ≤ 5.5 GB      |
| CPU main thread (frame)          | ≤ 12 ms      | tick ≤ 16 ms  |
| GPU frame                        | ≤ 14 ms      | —             |
| Draw calls                       | ≤ 1500       | —             |
| Set‑pass calls                   | ≤ 200        | —             |
| Triangles on screen              | ≤ 1.5 M      | —             |
| Network upstream / client        | —            | ≤ 80 kbit/s   |
| Network downstream / client      | —            | ≤ 200 kbit/s  |
| Bandwidth at 100p                | —            | ≤ 25 Mbit/s   |

---
