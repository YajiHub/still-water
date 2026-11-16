using NUnit.Framework;
using StillWater.Level;
using StillWater.Net;
using UnityEngine;

namespace StillWater.Tests
{
    public class LocalCircuitTests
    {
        private LocalCircuit circuit;
        private LocalRaceConfig config;

        [SetUp]
        public void SetUp()
        {
            config = JsonUtility.FromJson<LocalRaceConfig>(Resources.Load<TextAsset>("LocalRaceConfig").text);
            circuit = new LocalCircuit(Resources.Load<LevelCatalog>("LevelCatalog"), config);
        }

        [Test]
        public void BothPhonesMustBeReadyAndEveryRoomStartsAtThePreviousDoor()
        {
            var rooms = circuit.Geometry.rooms;
            Assert.That(rooms.Length, Is.EqualTo(5));
            for (int i = 1; i < rooms.Length; i++)
            {
                Assert.That(rooms[i].minZ, Is.EqualTo(rooms[i - 1].maxZ).Within(0.001f));
                Assert.That(rooms[i].spawn.x, Is.EqualTo(rooms[i - 1].door.x).Within(0.001f));
            }

            circuit.Ready(0, 1);
            circuit.Tick(config.countdownSeconds);
            Assert.That(circuit.Phase, Is.EqualTo("waiting"));
            circuit.Ready(1, 0);
            Assert.That(circuit.Phase, Is.EqualTo("countdown"));
            circuit.Tick(config.countdownSeconds);
            Assert.That(circuit.Phase, Is.EqualTo("running"));
            Assert.That(circuit.Snapshot().Find(LocalCircuit.HostId).body, Is.EqualTo(1));
        }

        [Test]
        public void HostScoresDoorsAndReturnsTheSameFinishOrderForBothPlayers()
        {
            StartRace();
            var rooms = circuit.Geometry.rooms;
            for (int i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                var door = new Vector3(room.door.x, room.exitTopY, room.door.z);
                circuit.Position(1, door, 0f, 100f * i);
                circuit.Position(0, door, 0f, 100f * i);
            }

            var snapshot = circuit.Snapshot();
            Assert.That(snapshot.result, Is.EqualTo("cleared"));
            Assert.That(snapshot.Find(LocalCircuit.GuestId).place, Is.EqualTo(1));
            Assert.That(snapshot.Find(LocalCircuit.HostId).place, Is.EqualTo(2));
            Assert.That(snapshot.Find(LocalCircuit.GuestId).score,
                Is.EqualTo(rooms.Length * config.doorPoints[0] + config.finishPoints[0] + config.teamClearPoints));
        }

        [Test]
        public void ListenerCatchesTheSwimmerAndHostRespawnsOnlyThatPlayer()
        {
            StartRace();
            var water = circuit.Geometry.rooms[0].water;
            var at = new Vector3((water.minX + water.maxX) / 2f, water.surfaceY - 1f, (water.minZ + water.maxZ) / 2f);
            CircuitCaught caught = null;
            RaceRespawn respawn = null;
            circuit.Caught += (hit, spawn) => { caught = hit; respawn = spawn; };

            circuit.Position(1, at, 0f, 1f);
            for (int i = 0; caught == null && i < 100; i++) circuit.Tick(0.05f);

            Assert.That(caught?.id, Is.EqualTo(LocalCircuit.GuestId));
            Assert.That(respawn?.z, Is.EqualTo(circuit.Geometry.rooms[0].spawn.z));
            var snapshot = circuit.Snapshot();
            Assert.That(snapshot.Find(LocalCircuit.GuestId).catches, Is.EqualTo(1));
            Assert.That(snapshot.Find(LocalCircuit.HostId).catches, Is.Zero);
        }

        [Test]
        public void HostChecksPushDirectionAndCooldown()
        {
            StartRace();
            var spawn = circuit.Geometry.rooms[0].spawn;
            var host = new Vector3(spawn.x, spawn.y, spawn.z);
            circuit.Position(0, host, 0f, 1f);
            circuit.Position(1, host + Vector3.forward, 0f, 1f);
            int count = 0;
            CircuitShove shove = null;
            circuit.Pushed += (hit, _) => { count++; shove = hit; };

            circuit.Push(1, 1f); // The host is behind the guest.
            circuit.Push(0, 1f);
            circuit.Push(0, 1.1f);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(shove?.target, Is.EqualTo(LocalCircuit.GuestId));
        }

        private void StartRace()
        {
            circuit.Ready(0, 0);
            circuit.Ready(1, 1);
            circuit.Tick(config.countdownSeconds);
        }
    }
}
