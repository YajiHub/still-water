namespace StillWater.Level
{
    /// <summary>
    /// Where a ferry is on its trip (Level Roster, Level 3). Pure, so it can be tested.
    /// One cycle: pause at A → slide to B → pause at B → slide back to A, then repeat.
    /// </summary>
    public static class FerryRules
    {
        /// <summary>
        /// How far along A → B the ferry is at <paramref name="time"/>: 0 = at A, 1 = at B, 0.5 = halfway.
        /// </summary>
        /// <param name="time">seconds since the level started</param>
        /// <param name="seconds">one-way travel time, A → B (&gt; 0)</param>
        /// <param name="pause">seconds it waits at each end (≥ 0)</param>
        /// <param name="offset">seconds already "played" at the start, so ferries aren't in step</param>
        public static float Progress(float time, float seconds, float pause, float offset)
        {
            float cycle = 2f * (pause + seconds);
            float t = (time + offset) % cycle;
            if (t < pause) return 0f;                                  // waiting at A
            if (t < pause + seconds) return (t - pause) / seconds;     // sliding A → B
            if (t < 2f * pause + seconds) return 1f;                   // waiting at B
            return 1f - (t - 2f * pause - seconds) / seconds;          // sliding back B → A
        }
    }
}
