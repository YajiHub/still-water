using NUnit.Framework;
using StillWater.Monster;
using S = StillWater.Monster.ListenerState;

namespace StillWater.Tests
{
    public class ListenerLookTests
    {
        [Test] public void Roaming_Swims() => Assert.AreEqual(ListenerLook.Swim, ListenerLook.ClipFor(S.Roaming, false));
        [Test] public void Hunting_Swims() => Assert.AreEqual(ListenerLook.Swim, ListenerLook.ClipFor(S.Hunting, false));
        [Test] public void Alerted_TreadsWater() => Assert.AreEqual(ListenerLook.Tread, ListenerLook.ClipFor(S.Alerted, false));
        [Test] public void Lingering_TreadsWater() => Assert.AreEqual(ListenerLook.Tread, ListenerLook.ClipFor(S.Lingering, false));
        [Test] public void Caught_Lunges_WhateverTheState() => Assert.AreEqual(ListenerLook.Lunge, ListenerLook.ClipFor(S.Hunting, true));

        [Test] public void Playback_AtStrokeSpeed_IsNormal() => Assert.AreEqual(1f, ListenerLook.Playback(1.5f), 1e-4f);
        [Test] public void Playback_Standing_StillMovesSlowly() => Assert.AreEqual(0.6f, ListenerLook.Playback(0f), 1e-4f);
        [Test] public void Playback_Hunting14_Capped() => Assert.AreEqual(2.5f, ListenerLook.Playback(14f), 1e-4f);
    }
}
