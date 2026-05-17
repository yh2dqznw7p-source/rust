// SPDX-License-Identifier: MIT
// RustLike — deterministic simulation clock.
//
// Goals:
//   - 20 Hz sim tick on server (50 ms), 30 Hz physics, 60+ render on client.
//   - Frame-rate independent: many sim ticks may run within one rendered frame
//     (catch-up after a hitch), but never more than MaxCatchUpTicks to prevent
//     spiral-of-death.
//   - Tick number is a uint that wraps after ~6.8 years at 20 Hz. Safe.
//
// Use:
//   - GameLoop owns one SimClock and calls `Advance(deltaTime)` each frame.
//   - Systems implementing ITickable are invoked once per accumulated tick.
//
// Determinism note:
//   - We use double for accumulator (precision), but tick counter is integer.
//   - Avoid Time.deltaTime jitter: when running headless, use a real-time stopwatch.

using System.Diagnostics;
using RustLike.Core.Logging;

namespace RustLike.Core.TimeSys
{
    public sealed class SimClock
    {
        public readonly float TickRate;        // ticks per second (e.g., 20)
        public readonly float TickDelta;       // 1/TickRate
        public readonly int   MaxCatchUpTicks; // safety cap

        public uint  Tick { get; private set; }
        public double TimeSeconds { get; private set; }

        private double _accum;

        public SimClock(float tickRate, int maxCatchUpTicks = 4)
        {
            UnityEngine.Debug.Assert(tickRate > 0f);
            TickRate = tickRate;
            TickDelta = 1f / tickRate;
            MaxCatchUpTicks = maxCatchUpTicks;
        }

        /// <summary> Returns how many sim ticks should run this frame. </summary>
        public int Advance(double deltaTime)
        {
            _accum += deltaTime;
            int ticks = 0;
            while (_accum >= TickDelta && ticks < MaxCatchUpTicks)
            {
                _accum -= TickDelta;
                Tick++;
                TimeSeconds += TickDelta;
                ticks++;
            }
            if (_accum > TickDelta * MaxCatchUpTicks)
            {
                // avoid spiral of death on very long hitches (alt-tab, gc, disk)
                Log.Warn(LogCat.Perf, "SimClock catch-up exceeded; clamping accumulator.");
                _accum = 0;
            }
            return ticks;
        }

        /// <summary> Interpolation alpha [0..1] between previous and next tick (for client render). </summary>
        public float Alpha => (float)(_accum / TickDelta);
    }
}
