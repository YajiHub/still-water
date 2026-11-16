using System;
using System.Linq;
using System.Runtime.CompilerServices;
using StillWater.Level;
using StillWater.Monster;
using UnityEngine;

[assembly: InternalsVisibleTo("StillWater.Tests.EditMode")]

namespace StillWater.Net
{
    [Serializable]
    internal sealed class LocalRaceConfig
    {
        public float countdownSeconds, matchSeconds, catchPenaltySeconds, doorZoneHalfWidth, doorHeightTolerance, slotSpacing;
        public float sideTolerance, startTolerance, finishTolerance, minY, maxY;
        public float roamMargin, entryDepth, listenerReach;
        public float maxHorizontalSpeed, maxVerticalSpeed, movementTolerance;
        public float pushRange, pushHeightTolerance, pushConeDegrees, pushSpeed, pushCooldownSeconds, pushGraceSeconds;
        public int[] doorPoints, finishPoints;
        public int splashPoints, caughtPoints, teamClearPoints;
    }

    /// <summary>The host phone owns the race state. Layouts and Listener tuning come from the same assets as Solo.</summary>
    internal sealed class LocalCircuit
    {
        public const string HostId = "host", GuestId = "guest";
        public const int PlayerCount = 2;

        private sealed class Runner
        {
            public CircuitPlayer State;
            public bool Ready, InWater;
            public float? LastPositionAt;
            public float PushReadyAt, PushGraceUntil;
        }

        private sealed class Listener
        {
            public ListenerState State;
            public Vector2 Position, Target, Roam;
            public float Timer, RoamPause;
            public int Splashes;
        }

        public CircuitGeometry Geometry { get; }
        public string Phase { get; private set; } = "waiting";
        public event Action<CircuitCaught, RaceRespawn> Caught;
        public event Action<CircuitShove, CircuitPushed> Pushed;

        private readonly LocalRaceConfig config;
        private readonly LevelConfig[] levels;
        private readonly LevelLayout[] layouts;
        private readonly Listener[] listeners;
        private readonly int[] doorArrivals;
        private readonly Runner[] runners = new Runner[PlayerCount];
        private readonly float minX, maxX;
        private int finishers;
        private float countdown, remaining;
        private string result = "", reason = "";

        public LocalCircuit(LevelCatalog catalog, LocalRaceConfig config)
        {
            this.config = config;
            levels = catalog.levels.Where(level => level != null).ToArray();
            layouts = levels.Select(level => LevelLayout.FromJson(level.layout.text)).ToArray();
            if (layouts.Length == 0) throw new InvalidOperationException("The race has no level layouts.");
            Geometry = new CircuitGeometry { rooms = BuildRooms() };
            minX = Geometry.rooms.Min(room => room.water.minX) - config.sideTolerance;
            maxX = Geometry.rooms.Max(room => room.water.maxX) + config.sideTolerance;
            listeners = Geometry.rooms.Select(room => new Listener
            {
                Position = new Vector2((room.water.minX + room.water.maxX) * 0.5f, (room.water.minZ + room.water.maxZ) * 0.5f),
                Roam = RoamPoint(room.water),
            }).ToArray();
            doorArrivals = new int[layouts.Length];
            remaining = config.matchSeconds;
            countdown = config.countdownSeconds;
            runners[0] = CreateRunner(HostId, 0);
            runners[1] = CreateRunner(GuestId, 1);
        }

        public void Ready(int slot, int body)
        {
            if (Phase != "waiting") return;
            var runner = runners[slot];
            runner.Ready = true;
            runner.State.body = body == 1 ? 1 : 0;
            if (runners.All(player => player.Ready)) Phase = "countdown";
        }

        public void Position(int slot, Vector3 position, float yaw, float now)
        {
            var runner = runners[slot];
            var current = runner.State;
            if (Phase != "running" || current.penalty > 0f || current.place > 0 || !Inside(position)) return;
            if (runner.LastPositionAt.HasValue)
            {
                float dt = Mathf.Max(now - runner.LastPositionAt.Value, 0.02f) * (1f + config.movementTolerance);
                float maxSpeed = config.maxHorizontalSpeed + (now < runner.PushGraceUntil ? config.pushSpeed : 0f);
                if (Vector2.Distance(Flat(current), new Vector2(position.x, position.z)) > maxSpeed * dt ||
                    Mathf.Abs(position.y - current.y) > config.maxVerticalSpeed * dt) return;
            }

            current.x = position.x;
            current.y = position.y;
            current.z = position.z;
            if (!float.IsNaN(yaw) && !float.IsInfinity(yaw)) current.yaw = yaw;
            runner.LastPositionAt = now;
            current.room = Array.FindLastIndex(Geometry.rooms, room => current.z >= room.minZ);
            current.room = Mathf.Max(current.room, 0);
            var room = Geometry.rooms[current.room];
            var water = room.water;
            bool wet = current.y < water.surfaceY - config.entryDepth &&
                current.x >= water.minX && current.x <= water.maxX && current.z >= water.minZ && current.z <= water.maxZ;
            if (wet && !runner.InWater) HearSplash(current);
            runner.InWater = wet;

            var exit = layouts[current.room].exit;
            if (wet || current.room <= current.reached || current.y < room.exitTopY - config.doorHeightTolerance ||
                Mathf.Abs(current.x - room.door.x) > config.doorZoneHalfWidth ||
                current.z < room.maxZ - exit.depth || current.z > room.maxZ) return;

            current.reached = current.room;
            current.score += config.doorPoints[doorArrivals[current.room]++];
            if (current.room != Geometry.rooms.Length - 1) return;
            current.place = ++finishers;
            current.score += config.finishPoints[current.place - 1];
            if (finishers == PlayerCount)
            {
                foreach (var player in runners) player.State.score += config.teamClearPoints;
                Finish("cleared", "Everyone made it through every room.");
            }
        }

        public void Push(int slot, float now)
        {
            var runner = runners[slot];
            var from = runner.State;
            var target = runners[1 - slot];
            var to = target.State;
            if (Phase != "running" || from.place > 0 || from.penalty > 0f || now < runner.PushReadyAt) return;
            runner.PushReadyAt = now + config.pushCooldownSeconds;
            if (to.place > 0 || to.penalty > 0f || to.room != from.room ||
                Mathf.Abs(to.y - from.y) > config.pushHeightTolerance) return;
            Vector2 delta = Flat(to) - Flat(from);
            float distance = delta.magnitude;
            if (distance > config.pushRange) return;
            float angle = from.yaw * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            var direction = distance < 0.05f ? forward : delta / distance;
            if (Vector2.Dot(forward, direction) < Mathf.Cos(config.pushConeDegrees * Mathf.Deg2Rad)) return;
            target.PushGraceUntil = now + config.pushGraceSeconds;
            Pushed?.Invoke(new CircuitShove { pusher = from.id, target = to.id },
                new CircuitPushed { dx = direction.x, dz = direction.y, speed = config.pushSpeed });
        }

        public void Tick(float dt)
        {
            if (Phase == "countdown")
            {
                countdown = Mathf.Max(0f, countdown - dt);
                if (countdown == 0f) Phase = "running";
                return;
            }
            if (Phase != "running") return;
            foreach (var runner in runners)
                runner.State.penalty = Mathf.Max(0f, runner.State.penalty - dt);
            for (int room = 0; room < listeners.Length; room++) TickListener(room, dt);
            remaining = Mathf.Max(0f, remaining - dt);
            if (remaining == 0f) Finish("timeout", $"Time's up: {finishers} of {PlayerCount} made it through.");
        }

        public CircuitSnapshot Snapshot()
        {
            var players = new CircuitPlayer[runners.Length];
            for (int i = 0; i < players.Length; i++)
            {
                var p = runners[i].State;
                players[i] = new CircuitPlayer
                {
                    id = p.id, slot = p.slot, body = p.body, x = p.x, y = p.y, z = p.z, yaw = p.yaw,
                    room = p.room, reached = p.reached, place = p.place, splashes = p.splashes,
                    catches = p.catches, penalty = p.penalty, score = p.score,
                };
            }
            var monsters = new CircuitListenerState[listeners.Length];
            for (int i = 0; i < monsters.Length; i++)
            {
                var listener = listeners[i];
                monsters[i] = new CircuitListenerState
                {
                    room = i, x = listener.Position.x, z = listener.Position.y,
                    state = listener.State.ToString().ToLowerInvariant(),
                };
            }
            return new CircuitSnapshot
            {
                phase = Phase, result = result, reason = reason, countdown = countdown, remaining = remaining,
                roomCount = monsters.Length, listeners = monsters, players = players,
            };
        }

        private CircuitRoomGeometry[] BuildRooms()
        {
            var rooms = new CircuitRoomGeometry[layouts.Length];
            for (int i = 0; i < rooms.Length; i++)
            {
                var layout = layouts[i];
                var start = layout.start;
                var exit = layout.exit;
                float south = start.z - start.depth / 2f;
                float north = exit.z + exit.depth / 2f;
                float x = i == 0 ? 0f : rooms[i - 1].door.x - start.x;
                float z = i == 0 ? 0f : rooms[i - 1].maxZ - south;
                rooms[i] = new CircuitRoomGeometry
                {
                    index = i, id = levels[i].id, name = levels[i].displayName, offsetX = x, offsetZ = z,
                    minZ = south + z, maxZ = north + z, exitTopY = layout.exitTopY,
                    spawn = new Vec3 { x = start.x + x, y = layout.platformTopY + 0.05f, z = start.z + z },
                    door = new Vec3 { x = exit.x + x, y = layout.exitTopY + 1.1f, z = north + z - 0.15f },
                    water = new WaterBox
                    {
                        surfaceY = layout.water.y, minX = layout.water.minX + x, maxX = layout.water.maxX + x,
                        minZ = layout.water.minZ + z, maxZ = layout.water.maxZ + z,
                    },
                };
            }
            return rooms;
        }

        private Runner CreateRunner(string id, int slot)
        {
            var runner = new Runner { State = new CircuitPlayer { id = id, slot = slot, reached = -1 } };
            Spawn(runner, Geometry.rooms[0]);
            return runner;
        }

        private void Spawn(Runner runner, CircuitRoomGeometry room)
        {
            var p = runner.State;
            p.x = room.spawn.x + (p.slot - 0.5f) * config.slotSpacing;
            p.y = room.spawn.y;
            p.z = room.spawn.z;
            runner.InWater = false;
            runner.LastPositionAt = null;
        }

        private bool Inside(Vector3 p) => p.x >= minX && p.x <= maxX && p.y >= config.minY && p.y <= config.maxY &&
            p.z >= Geometry.rooms[0].minZ - config.startTolerance &&
            p.z <= Geometry.rooms[Geometry.rooms.Length - 1].maxZ + config.finishTolerance;

        private void HearSplash(CircuitPlayer player)
        {
            player.splashes++;
            player.score += config.splashPoints;
            var listener = listeners[player.room];
            listener.Splashes++;
            listener.Target = Flat(player);
            if (listener.State == ListenerState.Roaming)
            {
                listener.State = ListenerState.Alerted;
                listener.Timer = levels[player.room].listenerAlertDelay;
            }
            else if (listener.State == ListenerState.Lingering) listener.State = ListenerState.Hunting;
        }

        private void TickListener(int room, float dt)
        {
            var listener = listeners[room];
            var tuning = levels[room];
            var water = Geometry.rooms[room].water;
            Runner nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (var runner in runners)
            {
                if (!Swimming(runner, room)) continue;
                float distance = (Flat(runner.State) - listener.Position).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearest = runner;
                nearestDistance = distance;
            }
            if (nearest != null) listener.Target = Flat(nearest.State);
            listener.Timer -= dt;
            var previous = listener.State;
            listener.State = ListenerRules.Tick(previous, nearest != null,
                Vector2.Distance(listener.Position, listener.Target) <= config.listenerReach, listener.Timer <= 0f);
            if (listener.State != previous)
            {
                if (listener.State == ListenerState.Alerted) listener.Timer = tuning.listenerAlertDelay;
                if (listener.State == ListenerState.Lingering) listener.Timer = tuning.listenerLingerSeconds;
                if (listener.State == ListenerState.Roaming)
                {
                    listener.Roam = RoamPoint(water);
                    listener.RoamPause = 0f;
                }
            }
            if (listener.State == ListenerState.Roaming)
            {
                if (listener.RoamPause > 0f) listener.RoamPause -= dt;
                else
                {
                    listener.Position = Vector2.MoveTowards(listener.Position, listener.Roam, tuning.listenerRoamSpeed * dt);
                    if (Vector2.Distance(listener.Position, listener.Roam) <= config.listenerReach)
                    {
                        listener.RoamPause = tuning.listenerRoamPause;
                        listener.Roam = RoamPoint(water);
                    }
                }
            }
            if (listener.State != ListenerState.Hunting) return;
            float speed = tuning.listenerHuntSpeed * (1f + tuning.listenerSpeedPerSplash * Mathf.Max(0, listener.Splashes - 1));
            listener.Position = Vector2.MoveTowards(listener.Position, listener.Target, speed * dt);
            foreach (var runner in runners)
            {
                if (!Swimming(runner, room) || Vector2.Distance(listener.Position, Flat(runner.State)) > tuning.listenerCatchDistance) continue;
                var player = runner.State;
                var caught = new CircuitCaught { id = player.id, room = room, x = player.x, z = player.z };
                Spawn(runner, Geometry.rooms[room]);
                player.penalty = config.catchPenaltySeconds;
                player.catches++;
                player.score += config.caughtPoints;
                listener.State = ListenerState.Lingering;
                listener.Timer = tuning.listenerLingerSeconds;
                Caught?.Invoke(caught, new RaceRespawn { x = player.x, y = player.y, z = player.z });
            }
        }

        private static bool Swimming(Runner runner, int room) =>
            runner.State.room == room && runner.InWater && runner.State.penalty <= 0f && runner.State.place == 0;

        private Vector2 RoamPoint(WaterBox water) => new Vector2(
            water.minX + config.roamMargin + UnityEngine.Random.value * Mathf.Max(0f, water.maxX - water.minX - 2f * config.roamMargin),
            water.minZ + config.roamMargin + UnityEngine.Random.value * Mathf.Max(0f, water.maxZ - water.minZ - 2f * config.roamMargin));

        private static Vector2 Flat(CircuitPlayer p) => new Vector2(p.x, p.z);

        private void Finish(string outcome, string message)
        {
            Phase = "finished";
            result = outcome;
            reason = message;
        }
    }
}
