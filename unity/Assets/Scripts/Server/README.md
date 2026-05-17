# RustLike.Server

Dedicated server entry point. Composes `Core + Net + World + Gameplay`.

> **Status:** placeholder — `ServerBootstrap` will be added at the end of
> Phase 0 once the FishNet transport is wired up.

## What goes here

- `ServerBootstrap.cs` — `[RuntimeInitializeOnLoadMethod]` hook that
  - constructs the asset service,
  - registers all gameplay services in `ServiceLocator`,
  - starts the FishNet server on configured port,
  - opens the persistence layer (SQLite first).
- `Persistence/` — write-behind save with `fsync` on graceful shutdown.
- `Admin/` — RCON-style command console (telnet or simple UDP).

## Stripping

Server build defines `UNITY_SERVER`, which:
- excludes `RustLike.Presentation` (asmdef constraint),
- skips audio, animation, particles in Player Settings,
- targets `Linux64` headless or `Windows64 -nographics`.

Expected RAM usage: ≤ 5.5 GB at 100 players. CPU tick budget: 16 ms @ 20 Hz
(see [docs/ARCHITECTURE.md](../../../../docs/ARCHITECTURE.md#6-как-оптимизировать-сервер-под-100-игроков)).

## CLI flags (planned)

```
-server                  run as dedicated server
-port <n>                bind port (default 28015)
-maxplayers <n>          default 100
-seed <n>                world seed
-savedir <path>          persistence directory
-tick <hz>               sim tick rate (default 20)
```
