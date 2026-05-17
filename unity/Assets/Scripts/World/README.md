# RustLike.World

World model and streaming.

## Layout

- `Chunks/` — 64×64 m chunks, anchor-driven streaming.
- `Terrain/` — heightmap + mesh generation (Phase 1).
- `Biomes/` — climate / spawn rules (Phase 1).
- `TimeOfDay/`, `Weather/` — Phase 1+.
- `Monuments/` — pre-baked points-of-interest (Phase 8).

## Streaming model

```
Player position  →  PlayerAnchor.Update()
                      ↓
       diff{prev,next set of ChunkCoord}
                      ↓
       ChunkManager.UpdateAnchorSet
                      ↓
       AddAnchor → load (async, bounded concurrency)
       RemoveAnchor → unload after grace 5s
```

## Adding a new chunk-aware decoration

1. Implement `IChunkProvider` (composite-able with the existing one), OR
2. Hook into `ChunkManager.AddAnchor` events to spawn your decoration into
   `Chunk.VisualRoot`.

## Memory hygiene

- One Chunk = one `GameObject` root. Sub-elements are children. On unload
  we `Destroy` the root and rely on Addressables `ReleaseInstance` for spawned
  Addressables prefabs.
- Don't keep references to chunk visuals outside the chunk. The `Chunk` object
  itself is safe to keep (it's lightweight metadata).
