// SPDX-License-Identifier: MIT
// RustLike — single, deterministic game loop.
//
// One MonoBehaviour drives EVERYTHING. We don't rely on per-class Update calls
// for sim systems because:
//   - explicit ordering,
//   - sim ticks decoupled from frame rate (catch-up),
//   - single profiler marker per system,
//   - server runs the same code path with `-batchmode` and a synthesized loop.
//
// Server mode:
//   When `UNITY_SERVER` define is set or `--server` CLI arg, the bootstrap can
//   spin up a synthetic loop on a dedicated thread and bypass MonoBehaviour
//   Update entirely. For now we share the Unity main loop (works for both).

using System.Collections.Generic;
using RustLike.Core.Logging;
using RustLike.Core.TimeSys;
using UnityEngine;
using UnityEngine.Profiling;

namespace RustLike.Core.Bootstrap
{
    [DefaultExecutionOrder(-10000)]
    public sealed class GameLoop : MonoBehaviour
    {
        public static GameLoop Instance { get; private set; }

        [Header("Tick rates")]
        [SerializeField] private float _simTickRate = 20f;
        [SerializeField] private int   _maxCatchUpTicks = 4;
        [SerializeField] private int   _targetFrameRate = 60;

        public SimClock Sim { get; private set; }

        // sorted by Order on registration; we keep two lists to avoid per-frame sorting
        private readonly List<ITickable>        _tickables    = new(64);
        private readonly List<IFrameTickable>   _frameTickables = new(32);
        private readonly List<IPhysicsTickable> _physicsTickables = new(8);

        // pre-allocated profiler markers per system to avoid string alloc each call
        private readonly Dictionary<ITickable, CustomSampler> _samplers = new(64);

        private bool _started;

        public static void Boot(GameObject host, float simTickRate = 20f)
        {
            if (Instance != null) return;
            var gl = host.AddComponent<GameLoop>();
            gl._simTickRate = simTickRate;
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Sim = new SimClock(_simTickRate, _maxCatchUpTicks);

            Application.targetFrameRate = _targetFrameRate;
            QualitySettings.vSyncCount = 0;

            Log.Info(LogCat.Boot, "GameLoop ready. tickRate=", _simTickRate);
            _started = true;
        }

        public void Register(ITickable t)
        {
            _tickables.Add(t);
            _tickables.Sort((a, b) => a.Order.CompareTo(b.Order));
            _samplers[t] = CustomSampler.Create(t.GetType().Name);
        }

        public void Register(IFrameTickable t)
        {
            _frameTickables.Add(t);
            _frameTickables.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void Register(IPhysicsTickable t)
        {
            _physicsTickables.Add(t);
            _physicsTickables.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        public void Unregister(ITickable t)         { _tickables.Remove(t); _samplers.Remove(t); }
        public void Unregister(IFrameTickable t)    => _frameTickables.Remove(t);
        public void Unregister(IPhysicsTickable t)  => _physicsTickables.Remove(t);

        private void Update()
        {
            if (!_started) return;

            int simTicks = Sim.Advance(Time.unscaledDeltaTime);
            for (int s = 0; s < simTicks; s++)
            {
                uint tick = Sim.Tick;
                float dt = Sim.TickDelta;
                for (int i = 0; i < _tickables.Count; i++)
                {
                    var sys = _tickables[i];
                    var sampler = _samplers[sys];
                    sampler.Begin();
                    try { sys.Tick(tick, dt); }
                    catch (System.Exception ex)
                    {
                        Log.Error(LogCat.Core, "ITickable threw: " + sys.GetType().Name, ex);
                    }
                    sampler.End();
                }
            }

            // Frame tick (client only; server has no frame tickables registered)
            float frameDt = Time.unscaledDeltaTime;
            for (int i = 0; i < _frameTickables.Count; i++)
            {
                try { _frameTickables[i].FrameTick(frameDt); }
                catch (System.Exception ex)
                {
                    Log.Error(LogCat.Core, "IFrameTickable threw: " + _frameTickables[i].GetType().Name, ex);
                }
            }
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            for (int i = 0; i < _physicsTickables.Count; i++)
            {
                try { _physicsTickables[i].PhysicsTick(dt); }
                catch (System.Exception ex)
                {
                    Log.Error(LogCat.Core, "IPhysicsTickable threw: " + _physicsTickables[i].GetType().Name, ex);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
