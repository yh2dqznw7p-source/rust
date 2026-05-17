// SPDX-License-Identifier: MIT
// RustLike — explicit tick interfaces.
//
// We do NOT use Unity's Update/FixedUpdate for sim systems because:
//   - Update is frame-rate dependent (variable dt).
//   - FixedUpdate is tied to physics step we may want to vary.
//   - Reflection-based ordering is unpredictable; we want explicit ordering.
//
// Systems implement one or more of these and register with GameLoop.

namespace RustLike.Core.TimeSys
{
    /// <summary> Called every sim tick (default 20 Hz). Server + client. </summary>
    public interface ITickable
    {
        /// <param name="tick">Monotonic tick id.</param>
        /// <param name="dt">Fixed delta seconds.</param>
        void Tick(uint tick, float dt);

        /// <summary> Lower runs first. Use SystemOrder.* constants. </summary>
        int Order { get; }
    }

    /// <summary> Called every rendered frame on the client only. </summary>
    public interface IFrameTickable
    {
        void FrameTick(float dt);
        int Order { get; }
    }

    /// <summary> Called every Unity FixedUpdate (physics). Use sparingly. </summary>
    public interface IPhysicsTickable
    {
        void PhysicsTick(float fixedDt);
        int Order { get; }
    }

    /// <summary>
    /// Sort keys so any reader can grok ordering. Lower = earlier.
    /// Leave gaps so we can insert systems without renumbering.
    /// </summary>
    public static class SystemOrder
    {
        public const int NetReceive       = 0;
        public const int InputApply       = 100;
        public const int Survival         = 200;
        public const int Combat           = 300;
        public const int Building         = 400;
        public const int Vehicles         = 500;
        public const int Electricity      = 600;
        public const int AI               = 700;
        public const int Loot             = 800;
        public const int Physics          = 900;
        public const int World            = 1000;
        public const int SnapshotsBuild   = 1900;
        public const int NetSend          = 2000;
    }
}
