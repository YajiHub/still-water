namespace StillWater.Monster
{
    /// <summary>Roaming (nothing heard) → Alerted (stops, turns to the sound) → Hunting → Lingering → Roaming.</summary>
    public enum ListenerState { Roaming, Alerted, Hunting, Lingering }

    /// <summary>How the Listener shows itself while roaming (per level, in LevelConfig).</summary>
    public enum ListenerRoamMode
    {
        Fin,    // its back ridge cuts the surface
        Wake,   // only ripples behind it
        Hidden, // only its sound
    }

    /// <summary>
    /// The Listener's decisions (Core Loop Rules, DESIGN §5.2), pure so they can be tested.
    /// It never teleports: ListenerAI only ever swims toward a target, up or down.
    /// </summary>
    public static class ListenerRules
    {
        /// <summary>The state after it hears a splash.</summary>
        public static ListenerState OnHeard(ListenerState state) =>
            state == ListenerState.Roaming || state == ListenerState.Alerted
                ? ListenerState.Alerted   // stop and listen first (the level's alert delay)
                : ListenerState.Hunting;  // already after you: no delay

        /// <summary>The state after one frame.</summary>
        /// <param name="playerSwimming">swimming is noisy: it always knows where you are while you're in the water</param>
        /// <param name="atTarget">it reached the last place it heard you</param>
        /// <param name="timerDone">the alert delay or the linger time ran out</param>
        public static ListenerState Tick(ListenerState state, bool playerSwimming, bool atTarget, bool timerDone)
        {
            switch (state)
            {
                case ListenerState.Roaming: return playerSwimming ? ListenerState.Alerted : ListenerState.Roaming;
                case ListenerState.Alerted: return timerDone ? ListenerState.Hunting : ListenerState.Alerted;
                case ListenerState.Hunting: return !playerSwimming && atTarget ? ListenerState.Lingering : ListenerState.Hunting;
                case ListenerState.Lingering:
                    if (playerSwimming) return ListenerState.Hunting;
                    return timerDone ? ListenerState.Roaming : ListenerState.Lingering;
                default: return state;
            }
        }

        /// <summary>Whether its back rises above the surface (otherwise it swims deep, out of sight).</summary>
        public static bool ShowsBack(ListenerState state, ListenerRoamMode mode, bool glimpsing) =>
            state != ListenerState.Roaming || mode == ListenerRoamMode.Fin || glimpsing;
    }
}
