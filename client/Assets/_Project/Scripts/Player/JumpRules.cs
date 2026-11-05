using System;

namespace StillWater.Player
{
    /// <summary>
    /// The jump "feel helpers" (DESIGN §3.2) as pure functions: no MonoBehaviour, no Time.time,
    /// so EditMode tests can call them directly. PlayerMotor passes in the times and config values.
    /// Times are seconds. "Never happened" is float.NegativeInfinity.
    /// </summary>
    public static class JumpRules
    {
        /// <summary>
        /// True if a jump should start now.
        /// Coyote time: the player stood on ground at most <paramref name="coyoteTime"/> seconds ago.
        /// Jump buffer: Jump was pressed at most <paramref name="jumpBuffer"/> seconds ago.
        /// Both must hold. Both limits are inclusive (exactly 0.10 s ago still counts).
        /// </summary>
        public static bool CanJump(float now, float lastGroundedTime, float lastJumpPressedTime,
                                   float coyoteTime, float jumpBuffer)
        {
            return now - lastGroundedTime <= coyoteTime && now - lastJumpPressedTime <= jumpBuffer;
        }

        /// <summary>
        /// Upward take-off speed (m/s) that reaches exactly <paramref name="jumpHeight"/> metres
        /// under <paramref name="gravity"/>. Gravity may be given as -20 or 20; only its size matters.
        /// </summary>
        public static float JumpVelocity(float jumpHeight, float gravity)
        {
            return MathF.Sqrt(2 * jumpHeight * MathF.Abs(gravity));
        }

        /// <summary>
        /// Variable jump height: how strongly gravity pulls this frame.
        /// Returns <paramref name="lowJumpMultiplier"/> while the player is still rising
        /// (<paramref name="verticalVelocity"/> &gt; 0) but has let go of Jump, so an early release gives a short hop.
        /// Returns 1 otherwise (holding Jump, falling, or standing still).
        /// </summary>
        public static float GravityScale(float verticalVelocity, bool jumpHeld, float lowJumpMultiplier)
        {
            if (verticalVelocity > 0 && !jumpHeld)
            {
                return lowJumpMultiplier;
            }
            else
            {
                return 1f;
            }
        }
    }
}
