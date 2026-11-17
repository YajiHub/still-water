namespace StillWater.Monster
{
    /// <summary>
    /// Which animation the Listener plays, and how fast (Visual Pass): pure so it can be tested. Depth (back at the
    /// surface or deep) stays in <see cref="ListenerRules.ShowsBack"/>.
    /// </summary>
    public static class ListenerLook
    {
        // Clip names in Listener.controller (built by Still Water → Art Spike → Build Listener Prefab).
        public const string Swim = "Swim_Fwd_Loop", Tread = "Swim_Idle_Loop", Lunge = "Zombie_Scratch";
        // Looks, not gameplay: the swim clip looks right at about this speed; slower/faster plays it slower/faster.
        public const float StrokeSpeed = 1.5f, MinPlayback = 0.6f, MaxPlayback = 2.5f;
        // Looks: on a catch the Listener stands this far (m) from its victim, so the lunge's head comes down on them
        // (the slam reaches about 1.6 m forward, measured from the clip; closer and it overshoots past them), and how
        // long it takes to slide there.
        public const float LungeReach = 1.4f, LungeSettle = 0.3f;

        public static string ClipFor(ListenerState state, bool caught)
        {
            if (caught) return Lunge;
            return state == ListenerState.Roaming || state == ListenerState.Hunting ? Swim : Tread; // Alerted/Lingering hold still
        }

        /// <summary>Playback speed for how fast it is moving (m/s): strokes match the swim, within limits.</summary>
        public static float Playback(float metresPerSecond)
        {
            float p = metresPerSecond / StrokeSpeed;
            return p < MinPlayback ? MinPlayback : p > MaxPlayback ? MaxPlayback : p;
        }
    }
}
