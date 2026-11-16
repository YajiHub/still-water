using NUnit.Framework;
using StillWater.Net;
using UnityEngine;

namespace StillWater.Tests
{
    /// <summary>
    /// The client reads the server's JSON with JsonUtility: these samples were printed by the real server code
    /// (server/src/logic/circuit.ts toSnapshot and buildCircuit), so a renamed field on either side fails here.
    /// </summary>
    public class CircuitSnapshotTests
    {
        private const string Snapshot =
            "{\"phase\":\"running\",\"lobby\":0,\"countdown\":0,\"remaining\":900,\"roomCount\":5,\"result\":\"\",\"reason\":\"\"," +
            "\"listeners\":[{\"room\":0,\"x\":0,\"z\":12.75,\"state\":\"roaming\"},{\"room\":1,\"x\":0,\"z\":54.5,\"state\":\"roaming\"}]," +
            "\"players\":[{\"id\":\"abc\",\"slot\":0,\"body\":0,\"x\":-2.4000000000000004,\"y\":0.55,\"z\":-2,\"yaw\":0,\"room\":0,\"reached\":-1,\"place\":0,\"splashes\":0,\"catches\":0,\"penalty\":0,\"score\":0}," +
            "{\"id\":\"def\",\"slot\":1,\"body\":1,\"x\":-0.8,\"y\":0.55,\"z\":-2,\"yaw\":0,\"room\":2,\"reached\":1,\"place\":0,\"splashes\":2,\"catches\":1,\"penalty\":0,\"score\":5}]}";

        private const string Geometry =
            "{\"rooms\":[{\"index\":0,\"id\":\"level01\",\"name\":\"Shallow End\",\"offsetX\":0,\"offsetZ\":0,\"minZ\":-4,\"maxZ\":29.5," +
            "\"water\":{\"surfaceY\":0,\"minX\":-15,\"maxX\":15,\"minZ\":0,\"maxZ\":25.5},\"spawn\":{\"x\":0,\"y\":0.55,\"z\":-2},\"exitTopY\":1.1," +
            "\"door\":{\"x\":0,\"y\":2.2,\"z\":29.35},\"doorZone\":{\"minX\":-4,\"maxX\":4,\"minZ\":25.5,\"maxZ\":29.5,\"minY\":0.8},\"listenerStart\":{\"x\":0,\"z\":12.75}}," +
            "{\"index\":1,\"id\":\"level02\",\"name\":\"Deep End\",\"offsetX\":0,\"offsetZ\":33.5,\"minZ\":29.5,\"maxZ\":79.5," +
            "\"water\":{\"surfaceY\":0,\"minX\":-20,\"maxX\":20,\"minZ\":33.5,\"maxZ\":75.5},\"spawn\":{\"x\":0,\"y\":0.55,\"z\":31.5},\"exitTopY\":1.1," +
            "\"door\":{\"x\":0,\"y\":2.2,\"z\":79.35},\"doorZone\":{\"minX\":-4,\"maxX\":4,\"minZ\":75.5,\"maxZ\":79.5,\"minY\":0.8},\"listenerStart\":{\"x\":0,\"z\":54.5}}]}";

        [Test]
        public void ReadsTheServerSnapshot()
        {
            var s = JsonUtility.FromJson<CircuitSnapshot>(Snapshot);
            Assert.AreEqual("running", s.phase);
            Assert.AreEqual(5, s.roomCount);
            Assert.AreEqual(2, s.listeners.Length);
            Assert.AreEqual(1, s.listeners[1].room);
            Assert.AreEqual("roaming", s.listeners[1].state);
            Assert.AreEqual(54.5f, s.listeners[1].z, 1e-4f);
            var def = s.Find("def");
            Assert.AreEqual(1, def.slot);
            Assert.AreEqual(1, def.body);
            Assert.AreEqual(2, def.room);
            Assert.AreEqual(1, def.reached);
            Assert.AreEqual(0, def.place);
            Assert.AreEqual(2, def.splashes);
            Assert.AreEqual(1, def.catches);
            Assert.AreEqual(5, def.score);
            Assert.IsNull(s.Find("nobody"));
        }

        [Test]
        public void ReadsCatchesAndPushes()
        {
            var c = JsonUtility.FromJson<CircuitCaught>("{\"id\":\"abc\",\"x\":1.5,\"z\":90.25,\"room\":2}");
            Assert.AreEqual("abc", c.id);
            Assert.AreEqual(2, c.room);
            Assert.AreEqual(90.25f, c.z, 1e-4f);
            var p = JsonUtility.FromJson<CircuitPushed>("{\"dx\":0,\"dz\":1,\"speed\":7}");
            Assert.AreEqual(1f, p.dz, 1e-4f);
            Assert.AreEqual(7f, p.speed, 1e-4f);
        }

        [Test]
        public void ReadsTheRoomsGeometry()
        {
            var g = JsonUtility.FromJson<CircuitGeometry>(Geometry);
            Assert.AreEqual(2, g.rooms.Length);
            Assert.AreEqual("level02", g.rooms[1].id);
            Assert.AreEqual(33.5f, g.rooms[1].offsetZ, 1e-4f);
            Assert.AreEqual(g.rooms[0].maxZ, g.rooms[1].minZ, 1e-4f, "rooms join end to end");
            Assert.AreEqual(29.35f, g.rooms[0].door.z, 1e-4f);
            Assert.AreEqual(1.1f, g.rooms[0].exitTopY, 1e-4f);
            Assert.AreEqual(-20f, g.rooms[1].water.minX, 1e-4f);
        }
    }
}
