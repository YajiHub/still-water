using NUnit.Framework;
using StillWater.Player;

namespace StillWater.Tests
{
    public class PlayerLookTests
    {
        [Test]
        public void StandingStillOnLandIdles() =>
            Assert.AreEqual(PlayerLook.Idle, PlayerLook.ClipFor(PlayerMotor.MotorState.Grounded, 0f));

        [Test]
        public void RunningJogsThenSprints()
        {
            Assert.AreEqual(PlayerLook.Run, PlayerLook.ClipFor(PlayerMotor.MotorState.Grounded, 3f));
            Assert.AreEqual(PlayerLook.Sprint, PlayerLook.ClipFor(PlayerMotor.MotorState.Grounded, PlayerLook.SprintFrom));
        }

        [Test]
        public void WaterTreadsOrSwims()
        {
            Assert.AreEqual(PlayerLook.Tread, PlayerLook.ClipFor(PlayerMotor.MotorState.Swimming, 0.1f));
            Assert.AreEqual(PlayerLook.Swim, PlayerLook.ClipFor(PlayerMotor.MotorState.Swimming, 1.5f));
        }

        [Test]
        public void AirAndClimbIgnoreSpeed()
        {
            Assert.AreEqual(PlayerLook.Air, PlayerLook.ClipFor(PlayerMotor.MotorState.Airborne, 9f));
            Assert.AreEqual(PlayerLook.Climb, PlayerLook.ClipFor(PlayerMotor.MotorState.Climbing, 0f));
        }

        [Test]
        public void PlaybackFollowsSpeedWithinLimits()
        {
            Assert.AreEqual(1f, PlayerLook.Playback(PlayerLook.Run, PlayerLook.JogSpeed), 1e-4f);
            Assert.AreEqual(PlayerLook.MaxPlayback, PlayerLook.Playback(PlayerLook.Run, 100f), 1e-4f);
            Assert.AreEqual(PlayerLook.MinPlayback, PlayerLook.Playback(PlayerLook.Swim, 0.01f), 1e-4f);
            Assert.AreEqual(1f, PlayerLook.Playback(PlayerLook.Idle, 0f), 1e-4f);
        }
    }
}
