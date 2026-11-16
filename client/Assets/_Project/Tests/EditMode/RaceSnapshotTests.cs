using NUnit.Framework;
using StillWater.Net;
using UnityEngine;

namespace StillWater.Tests
{
    public class RaceSnapshotTests
    {
        [Test] public void Snapshot_ParsesWireFieldsAndFindsLocalPlayer()
        {
            var s = JsonUtility.FromJson<RaceSnapshot>("{\"phase\":\"racing\",\"remaining\":150,\"players\":[{\"id\":\"me\",\"x\":2,\"penalty\":3,\"monsterState\":\"hunting\",\"splashes\":1}]}");
            Assert.AreEqual("racing", s.phase);
            Assert.AreEqual(150, s.remaining);
            Assert.AreEqual(2, s.Find("me").x);
            Assert.AreEqual(3, s.Find("me").penalty);
            Assert.AreEqual("hunting", s.Find("me").monsterState);
            Assert.AreEqual(1, s.Find("me").splashes);
            Assert.IsNull(s.Find("missing"));
        }
        [Test] public void EmptySnapshot_HasNoPlayer() => Assert.IsNull(new RaceSnapshot().Find("me"));
        [Test] public void Respawn_ParsesServerCoordinates()
        {
            var r = JsonUtility.FromJson<RaceRespawn>("{\"x\":0,\"y\":0.55,\"z\":-2}");
            Assert.AreEqual(-2, r.z);
            Assert.AreEqual(0.55f, r.y);
        }
    }
}
