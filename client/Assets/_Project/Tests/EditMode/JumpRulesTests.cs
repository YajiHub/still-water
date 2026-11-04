using NUnit.Framework;
using StillWater.Player;

namespace StillWater.Tests
{
    // Times use exact binary fractions (0.25, 0.5) so the "exactly on the limit" tests aren't broken by float rounding.
    public class JumpRulesTests
    {
        private const float Coyote = 0.25f;
        private const float Buffer = 0.25f;
        private const float Never = float.NegativeInfinity;

        [Test]
        public void CanJump_GroundedAndPressedNow_True()
        {
            Assert.IsTrue(JumpRules.CanJump(10f, 10f, 10f, Coyote, Buffer));
        }

        [Test]
        public void CanJump_WalkedOffEdgeInsideCoyoteTime_True()
        {
            Assert.IsTrue(JumpRules.CanJump(10f, 9.875f, 10f, Coyote, Buffer));
        }

        [Test]
        public void CanJump_WalkedOffEdgeExactlyAtCoyoteLimit_True()
        {
            Assert.IsTrue(JumpRules.CanJump(10f, 9.75f, 10f, Coyote, Buffer));
        }

        [Test]
        public void CanJump_WalkedOffEdgeTooLongAgo_False()
        {
            Assert.IsFalse(JumpRules.CanJump(10f, 9.5f, 10f, Coyote, Buffer));
        }

        [Test]
        public void CanJump_PressedJustBeforeLanding_True()
        {
            Assert.IsTrue(JumpRules.CanJump(10f, 10f, 9.875f, Coyote, Buffer));
        }

        [Test]
        public void CanJump_PressedTooLongBeforeLanding_False()
        {
            Assert.IsFalse(JumpRules.CanJump(10f, 10f, 9.5f, Coyote, Buffer));
        }

        [Test]
        public void CanJump_NeverPressed_False()
        {
            Assert.IsFalse(JumpRules.CanJump(10f, 10f, Never, Coyote, Buffer));
        }

        [Test]
        public void CanJump_NeverGrounded_False()
        {
            Assert.IsFalse(JumpRules.CanJump(10f, Never, 10f, Coyote, Buffer));
        }

        [Test]
        public void JumpVelocity_ReachesDesignHeight()
        {
            // DESIGN §3.3: 1.2 m under -20 m/s² → sqrt(48) ≈ 6.928 m/s
            Assert.AreEqual(6.928f, JumpRules.JumpVelocity(1.2f, -20f), 0.001f);
        }

        [Test]
        public void JumpVelocity_GravitySignDoesNotMatter()
        {
            Assert.AreEqual(JumpRules.JumpVelocity(1.2f, -20f), JumpRules.JumpVelocity(1.2f, 20f), 0.0001f);
        }

        [Test]
        public void JumpVelocity_ZeroHeight_Zero()
        {
            Assert.AreEqual(0f, JumpRules.JumpVelocity(0f, -20f), 0.0001f);
        }

        [Test]
        public void GravityScale_RisingAndReleased_UsesLowJumpMultiplier()
        {
            Assert.AreEqual(2.5f, JumpRules.GravityScale(3f, false, 2.5f));
        }

        [Test]
        public void GravityScale_RisingAndHeld_Normal()
        {
            Assert.AreEqual(1f, JumpRules.GravityScale(3f, true, 2.5f));
        }

        [Test]
        public void GravityScale_FallingAndReleased_Normal()
        {
            Assert.AreEqual(1f, JumpRules.GravityScale(-3f, false, 2.5f));
        }

        [Test]
        public void GravityScale_StandingStill_Normal()
        {
            Assert.AreEqual(1f, JumpRules.GravityScale(0f, false, 2.5f));
        }
    }
}
