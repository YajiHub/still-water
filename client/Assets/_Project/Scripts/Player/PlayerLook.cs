namespace StillWater.Player
{
    /// <summary>
    /// Which animation the player's body plays, and how fast. Pure, so it can be tested. The clip names come from the
    /// Quaternius Universal Animation Library and match the states in Art/Player/Player.controller.
    /// </summary>
    public static class PlayerLook
    {
        public const string Idle = "Idle_Loop", Run = "Jog_Fwd_Loop", Sprint = "Sprint_Loop", Air = "Jump_Loop",
            Swim = "Swim_Fwd_Loop", Tread = "Swim_Idle_Loop", Climb = "ClimbUp_1m";
        /// <summary>Every state in Player.controller. The first one is the default.</summary>
        public static readonly string[] AllClips = { Idle, Run, Sprint, Air, Swim, Tread, Climb };

        // Looks, not gameplay: below MoveThreshold (m/s) you stand still. The jog clip looks right at JogSpeed and the
        // swim stroke at StrokeSpeed. From SprintFrom up you sprint.
        public const float MoveThreshold = 0.25f, JogSpeed = 3.5f, SprintFrom = 5.5f, SprintSpeed = 6f, StrokeSpeed = 1.5f;
        public const float MinPlayback = 0.6f, MaxPlayback = 1.8f;

        public static string ClipFor(PlayerMotor.MotorState state, float speed)
        {
            switch (state)
            {
                case PlayerMotor.MotorState.Climbing: return Climb;
                case PlayerMotor.MotorState.Airborne: return Air;
                case PlayerMotor.MotorState.Swimming: return speed < MoveThreshold ? Tread : Swim;
                default: return speed < MoveThreshold ? Idle : speed >= SprintFrom ? Sprint : Run;
            }
        }

        /// <summary>Playback speed so the feet (or strokes) match how fast you move, within limits.</summary>
        public static float Playback(string clip, float speed)
        {
            float natural = clip == Run ? JogSpeed : clip == Sprint ? SprintSpeed : clip == Swim ? StrokeSpeed : 0f;
            if (natural <= 0f) return 1f;
            float p = speed / natural;
            return p < MinPlayback ? MinPlayback : p > MaxPlayback ? MaxPlayback : p;
        }
    }
}
