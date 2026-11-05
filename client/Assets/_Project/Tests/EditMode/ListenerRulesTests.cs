using NUnit.Framework;
using StillWater.Monster;
using S = StillWater.Monster.ListenerState;

namespace StillWater.Tests
{
    public class ListenerRulesTests
    {
        // Hearing a sound
        [Test] public void Heard_WhileRoaming_StopsToListen() => Assert.AreEqual(S.Alerted, ListenerRules.OnHeard(S.Roaming));
        [Test] public void Heard_WhileAlerted_StaysAlerted() => Assert.AreEqual(S.Alerted, ListenerRules.OnHeard(S.Alerted));
        [Test] public void Heard_WhileLingering_HuntsAtOnce() => Assert.AreEqual(S.Hunting, ListenerRules.OnHeard(S.Lingering));
        [Test] public void Heard_WhileHunting_KeepsHunting() => Assert.AreEqual(S.Hunting, ListenerRules.OnHeard(S.Hunting));

        // Each frame: Tick(state, playerSwimming, atTarget, timerDone)
        [Test] public void Roaming_PlayerSwimming_Alerted() => Assert.AreEqual(S.Alerted, ListenerRules.Tick(S.Roaming, true, false, false));
        [Test] public void Roaming_Quiet_KeepsRoaming() => Assert.AreEqual(S.Roaming, ListenerRules.Tick(S.Roaming, false, true, true));
        [Test] public void Alerted_DelayOver_Hunts() => Assert.AreEqual(S.Hunting, ListenerRules.Tick(S.Alerted, false, false, true));
        [Test] public void Alerted_DelayRunning_Waits() => Assert.AreEqual(S.Alerted, ListenerRules.Tick(S.Alerted, true, false, false));
        [Test] public void Hunting_PlayerOutAndSpotReached_Lingers() => Assert.AreEqual(S.Lingering, ListenerRules.Tick(S.Hunting, false, true, false));
        [Test] public void Hunting_PlayerOutStillSwimmingThere_KeepsGoing() => Assert.AreEqual(S.Hunting, ListenerRules.Tick(S.Hunting, false, false, false));
        [Test] public void Hunting_PlayerSwimming_KeepsTracking() => Assert.AreEqual(S.Hunting, ListenerRules.Tick(S.Hunting, true, true, false));
        [Test] public void Lingering_PlayerBackInWater_Hunts() => Assert.AreEqual(S.Hunting, ListenerRules.Tick(S.Lingering, true, false, false));
        [Test] public void Lingering_TimeUp_RoamsFromThere() => Assert.AreEqual(S.Roaming, ListenerRules.Tick(S.Lingering, false, true, true));

        // Showing its back above the water
        [Test] public void Fin_ShowsBackWhileRoaming() => Assert.IsTrue(ListenerRules.ShowsBack(S.Roaming, ListenerRoamMode.Fin, false));
        [Test] public void Wake_HidesBackWhileRoaming() => Assert.IsFalse(ListenerRules.ShowsBack(S.Roaming, ListenerRoamMode.Wake, false));
        [Test] public void Hidden_ShowsBackDuringGlimpse() => Assert.IsTrue(ListenerRules.ShowsBack(S.Roaming, ListenerRoamMode.Hidden, true));
        [Test] public void Hidden_RisesWhenItHearsYou() => Assert.IsTrue(ListenerRules.ShowsBack(S.Alerted, ListenerRoamMode.Hidden, false));
    }
}
