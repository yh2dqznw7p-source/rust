# RustLike.Net

Networking abstraction. Built around FishNet but exposes its own interfaces so
the rest of the project (and tests) never types `using FishNet`.

## Sub-modules

- `Transport/` — `INetTransport`, `NetChannel`, `BitWriter`/`BitReader`.
- `AOI/` — uniform grid for area-of-interest queries.
- `Snapshots/` — `INetSerializable`, `SnapshotBuilder`.
- `Prediction/` — (future) client-side prediction & reconciliation glue.
- `Replication/` — (future) FishNet `NetworkBehaviour` thin wrappers.

## Channels

| Channel | Use case |
|---|---|
| `Unreliable` | One-shot effects, hit feedback |
| `UnreliableSequenced` | Movement, snapshots |
| `Reliable` | Inventory ops, building place, chat, RPCs |
| `ReliableSequenced` | Chunk asset bundles, large state |

## Adding a new networked entity

```csharp
public sealed class MyEnt : INetSerializable {
    public int  NetId            => _id;
    public bool HasPendingDelta  => _dirty != 0;
    public byte SnapshotPriority => 5;
    public bool WriteDelta(ref BitWriter w, bool clearDirty) {
        // write a tiny dirty mask, then changed fields
        w.WriteBits(_dirty, 4);
        // ...
        if (clearDirty) _dirty = 0;
        return true;
    }
}
```
Then register with `SnapshotBuilder.Register(myEnt)` and call
`AreaOfInterestGrid.AddOrUpdate(myEnt.NetId, x, z)` when its position changes.

## Bandwidth budget

`NetLimits.PacketBudgetBytes = 1100` (one MTU minus header).
`NetLimits.DownstreamBytesPerSecond = 25 000` per client.

If you need more headroom, **don't increase it** — instead:
- compress fields harder (`WriteCompressedFloat`),
- raise `SnapshotPriority` for important entities so they win the queue,
- back-off frequency for low-priority data via `HasPendingDelta`.
