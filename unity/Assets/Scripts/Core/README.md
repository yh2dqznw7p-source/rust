# RustLike.Core

Foundation layer. Knows nothing about the rest of the game.

## What lives here

| Sub-module | Files | Purpose |
|---|---|---|
| Bootstrap | `Bootstrap.cs`, `GameLoop.cs`, `ServiceLocator.cs` | Single entry point, deterministic frame loop, DI |
| Events | `EventBus.cs` | Type-safe struct events, snapshot iteration, no GC |
| Time | `SimClock.cs`, `ITickable.cs` | Fixed-step simulation clock, system ordering |
| Memory | `ObjectPool.cs`, `ListPool.cs`, `PrefabPool.cs`, `NativeArrayPool.cs`, `SwapList.cs` | All things to avoid GC |
| Streaming | `IAssetService.cs`, `AddressablesAssetService.cs`, `StreamingBudget.cs` | Asset loading abstraction + RAM budgets |
| Math | `FastRng.cs` | Deterministic xorshift64* RNG |
| Logging | `Log.cs` | Cheap categorized logger |

## Adding a new tickable system

```csharp
public sealed class MySystem : ITickable
{
    public int Order => SystemOrder.Combat; // pick a constant
    public void Tick(uint tick, float dt) { /* hot path */ }
}

// in your bootstrap:
GameLoop.Instance.Register(new MySystem());
```

## Rules

- Do **not** allocate on the hot tick path. Use pools.
- Do **not** depend on Unity types you don't actually need (e.g., no `Animator`
  reference here — only `UnityEngine` math/scene primitives).
- Keep classes/structs ≤ ~250 lines. Split when you exceed that.
