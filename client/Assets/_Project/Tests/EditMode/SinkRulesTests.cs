using NUnit.Framework;
using StillWater.Level;
using S = StillWater.Level.SinkState;

namespace StillWater.Tests
{
    // Level 4 "medium": stand 1.5 s (last 0.6 s shaking), sink 0.5 s, rise after 3 s, one-way 0.3 s after you leave.
    public class SinkRulesTests
    {
        private static readonly SinkTimings T = new SinkTimings { stand = 1.5f, shake = 0.6f, sink = 0.5f, riseAfter = 3f, onewayDelay = 0.3f };

        private static (S, float) Crumble(S s, float timer, bool standing, float dt, bool swimmer = false) =>
            SinkRules.StepCrumble(s, timer, standing, swimmer, dt, T);

        // Crumbling
        [Test] public void Crumble_NobodyOnIt_StaysUp() => Assert.AreEqual((S.Up, 0f), Crumble(S.Up, 0f, false, 1f));
        [Test] public void Crumble_ShortStand_StillUp() => Assert.AreEqual(S.Up, Crumble(S.Up, 0f, true, 0.5f).Item1);
        [Test] public void Crumble_StandNearlyTooLong_Shakes() => Assert.AreEqual(S.Shaking, Crumble(S.Up, 0.8f, true, 0.2f).Item1);
        [Test] public void Crumble_LeavingBeforeShaking_ResetsTimer() => Assert.AreEqual((S.Up, 0f), Crumble(S.Up, 0.8f, false, 0.1f));
        [Test] public void Crumble_LeavingWhileShaking_StillSinks() => Assert.AreEqual(S.Sinking, Crumble(S.Shaking, 1.4f, false, 0.2f).Item1);
        [Test] public void Crumble_SinkDone_IsDown() => Assert.AreEqual(S.Down, Crumble(S.Sinking, 0.4f, false, 0.2f).Item1);
        [Test] public void Crumble_RisesAfterDelay() => Assert.AreEqual(S.Rising, Crumble(S.Down, 2.9f, false, 0.2f).Item1);
        [Test] public void Crumble_WaitsWhileSwimmerAbove() => Assert.AreEqual(S.Down, Crumble(S.Down, 2.9f, false, 0.2f, swimmer: true).Item1);
        [Test] public void Crumble_RiseDone_IsUpAgain() => Assert.AreEqual((S.Up, 0f), Crumble(S.Rising, 0.4f, false, 0.2f));

        // One-way
        [Test] public void Oneway_Landing_Arms() => Assert.AreEqual(S.Armed, SinkRules.StepOneway(S.Up, 0f, true, 0.1f, T).Item1);
        [Test] public void Oneway_StandingAsLongAsYouLike_StaysArmed() => Assert.AreEqual((S.Armed, 0f), SinkRules.StepOneway(S.Armed, 0f, true, 10f, T));
        [Test] public void Oneway_JustLeft_NotYet() => Assert.AreEqual(S.Armed, SinkRules.StepOneway(S.Armed, 0f, false, 0.2f, T).Item1);
        [Test] public void Oneway_LeftLongEnough_Sinks() => Assert.AreEqual(S.Sinking, SinkRules.StepOneway(S.Armed, 0.2f, false, 0.2f, T).Item1);
        [Test] public void Oneway_Down_StaysDownForever() => Assert.AreEqual(S.Down, SinkRules.StepOneway(S.Down, 0f, false, 100f, T).Item1);

        // Depth and climbing
        [Test] public void Depth_HalfwayDown() => Assert.AreEqual(0.5f, SinkRules.Depth01(S.Sinking, 0.25f, T), 0.001f);
        [Test] public void Depth_HalfwayUp() => Assert.AreEqual(0.5f, SinkRules.Depth01(S.Rising, 0.25f, T), 0.001f);
        [Test] public void Depth_ShakingIsStillUp() => Assert.AreEqual(0f, SinkRules.Depth01(S.Shaking, 1f, T));
        [Test] public void Climbable_OnlyWhileUp()
        {
            Assert.IsTrue(SinkRules.Climbable(S.Up));
            Assert.IsTrue(SinkRules.Climbable(S.Armed));
            Assert.IsFalse(SinkRules.Climbable(S.Down));
            Assert.IsFalse(SinkRules.Climbable(S.Rising));
        }
    }
}
