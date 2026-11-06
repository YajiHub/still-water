using NUnit.Framework;
using StillWater.Level;

namespace StillWater.Tests
{
    // A ferry that slides for 3 s and pauses 1 s at each end: one cycle = 8 s.
    public class FerryRulesTests
    {
        private const float Seconds = 3f, Pause = 1f, Tolerance = 0.001f;

        private static float At(float time, float offset = 0f) => FerryRules.Progress(time, Seconds, Pause, offset);

        [Test] public void Start_WaitsAtA() => Assert.AreEqual(0f, At(0f), Tolerance);
        [Test] public void DuringFirstPause_StillAtA() => Assert.AreEqual(0f, At(0.5f), Tolerance);
        [Test] public void HalfwayThere() => Assert.AreEqual(0.5f, At(1f + 1.5f), Tolerance);
        [Test] public void ArrivesAtB() => Assert.AreEqual(1f, At(1f + 3f), Tolerance);
        [Test] public void DuringSecondPause_StaysAtB() => Assert.AreEqual(1f, At(4.5f), Tolerance);
        [Test] public void HalfwayBack() => Assert.AreEqual(0.5f, At(5f + 1.5f), Tolerance);
        [Test] public void QuarterWayBack_IsCloserToB() => Assert.AreEqual(0.75f, At(5f + 0.75f), Tolerance);
        [Test] public void AfterOneCycle_BackAtA() => Assert.AreEqual(0f, At(8f), Tolerance);
        [Test] public void Repeats() => Assert.AreEqual(At(2.5f), At(2.5f + 8f * 10f), Tolerance);
        [Test] public void Offset_StartsPartWay() => Assert.AreEqual(0.5f, At(0f, offset: 2.5f), Tolerance);
        [Test] public void NoPause_StillWorks() => Assert.AreEqual(0.5f, FerryRules.Progress(1.5f, 3f, 0f, 0f), Tolerance);
    }
}
