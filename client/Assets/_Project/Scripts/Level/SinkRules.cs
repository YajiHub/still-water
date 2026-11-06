namespace StillWater.Level
{
    /// <summary>Where a sinking tile is in its life (Level Roster, Level 4).</summary>
    public enum SinkState
    {
        Up,       // solid, nothing happening
        Shaking,  // crumbling: you stood too long; it will sink now even if you leave
        Armed,    // one-way: you're on it; it sinks once you leave
        Sinking,  // going down
        Down,     // under water: not solid, not climbable
        Rising,   // crumbling only: coming back up
    }

    /// <summary>The level's sinking timings (from LevelConfig), so the rules don't depend on Unity assets.</summary>
    public struct SinkTimings
    {
        public float stand;       // crumbling: seconds of standing before it sinks
        public float shake;       // crumbling: the last part of `stand`, when it shakes (no way back)
        public float sink;        // seconds to go down (and to come back up)
        public float riseAfter;   // crumbling: seconds under water before it rises
        public float onewayDelay; // one-way: seconds after you leave before it sinks
    }

    /// <summary>
    /// The two sinking tiles' rules, pure so they can be tested. Each Step takes the current state and its timer
    /// and returns the next ones; SinkingPlatform calls it every frame.
    /// </summary>
    public static class SinkRules
    {
        /// <summary>Crumbling: Up → (stand too long) Shaking → Sinking → Down → (rise after, no swimmer above) Rising → Up.</summary>
        public static (SinkState state, float timer) StepCrumble(SinkState state, float timer, bool standing, bool swimmerAbove, float dt, SinkTimings t)
        {
            switch (state)
            {
                case SinkState.Up:
                    if (!standing) return (SinkState.Up, 0f); // leaving resets the timer
                    timer += dt;
                    return timer >= t.stand - t.shake ? (SinkState.Shaking, timer) : (SinkState.Up, timer);
                case SinkState.Shaking:
                    timer += dt; // committed: keeps going even if you jump off
                    return timer >= t.stand ? (SinkState.Sinking, 0f) : (SinkState.Shaking, timer);
                case SinkState.Down:
                    timer += dt;
                    // Never rise into a swimmer: wait until they've moved off.
                    return timer >= t.riseAfter && !swimmerAbove ? (SinkState.Rising, 0f) : (SinkState.Down, timer);
                case SinkState.Rising:
                    timer += dt;
                    return timer >= t.sink ? (SinkState.Up, 0f) : (SinkState.Rising, timer);
                default:
                    return StepSinking(state, timer, dt, t);
            }
        }

        /// <summary>One-way: Up → (you land) Armed → (you leave, delay) Sinking → Down, and stays down.</summary>
        public static (SinkState state, float timer) StepOneway(SinkState state, float timer, bool standing, float dt, SinkTimings t)
        {
            switch (state)
            {
                case SinkState.Up:
                    return standing ? (SinkState.Armed, 0f) : (SinkState.Up, 0f);
                case SinkState.Armed:
                    if (standing) return (SinkState.Armed, 0f); // back on it before it went: wait again
                    timer += dt;
                    return timer >= t.onewayDelay ? (SinkState.Sinking, 0f) : (SinkState.Armed, timer);
                case SinkState.Down:
                    return (SinkState.Down, timer);
                default:
                    return StepSinking(state, timer, dt, t);
            }
        }

        /// <summary>How far down it is: 0 = up, 1 = fully under water.</summary>
        public static float Depth01(SinkState state, float timer, SinkTimings t)
        {
            switch (state)
            {
                case SinkState.Sinking: return t.sink > 0f ? System.Math.Min(1f, timer / t.sink) : 1f;
                case SinkState.Down: return 1f;
                case SinkState.Rising: return t.sink > 0f ? System.Math.Max(0f, 1f - timer / t.sink) : 0f;
                default: return 0f;
            }
        }

        /// <summary>You can climb onto it from the water only while it's up.</summary>
        public static bool Climbable(SinkState state) => state == SinkState.Up || state == SinkState.Shaking || state == SinkState.Armed;

        private static (SinkState, float) StepSinking(SinkState state, float timer, float dt, SinkTimings t)
        {
            if (state != SinkState.Sinking) return (state, timer);
            timer += dt;
            return timer >= t.sink ? (SinkState.Down, 0f) : (SinkState.Sinking, timer);
        }
    }
}
