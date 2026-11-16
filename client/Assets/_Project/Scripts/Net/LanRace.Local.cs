using System;
using System.Net;
using System.Net.Sockets;
using StillWater.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StillWater.Net
{
    public partial class LanRace
    {
        private const string Hello = "still-water-local/1", StartMatch = "start/1", Ping = "ping";
        private const float SnapshotInterval = 0.1f, PingInterval = 1f, PeerTimeout = 10f;
        private const float HostStep = 0.05f;

        [Serializable]
        private sealed class LocalStart
        {
            public CircuitGeometry geometry;
            public CircuitSnapshot snapshot;
        }

        [Serializable]
        private sealed class ReadyPacket
        {
            public int body;
        }

        public bool IsLocal { get; private set; }
        public int MaxPlayers => IsLocal ? LocalCircuit.PlayerCount : 4;
        public string[] HostAddresses { get; private set; } = Array.Empty<string>();
        public float LocalRaceTime
        {
            get
            {
                if (Snapshot?.phase == "running")
                    return localMatchSeconds - Snapshot.remaining + Mathf.Clamp(Time.unscaledTime - localSnapshotAt, 0f, 0.3f);
                return Snapshot?.phase == "finished" ? localMatchSeconds - Snapshot.remaining : 0f;
            }
        }

        private LocalRaceLink localLink;
        private LocalCircuit localCircuit;
        private bool localActive;
        private float lastLocalPacket, nextPing, nextLocalSnapshot, hostAccumulator, localSnapshotAt, localMatchSeconds;

        public void SelectMultiplayerMode(bool local)
        {
            CancelConnect();
            IsLocal = local;
            Status = "";
        }

        public void HostLocal()
        {
            if (Connecting || Active) return;
            IsLocal = true;
            var asset = Resources.Load<TextAsset>("LocalRaceConfig");
            if (asset == null)
            {
                Status = "The local race configuration is missing.";
                return;
            }
            try
            {
                localCircuit = new LocalCircuit(GameState.Catalog, JsonUtility.FromJson<LocalRaceConfig>(asset.text));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Local Multiplayer] {e.Message}");
                Status = "Could not load the local race layouts.";
                return;
            }
            localCircuit.Caught += LocalCatch;
            localCircuit.Pushed += LocalPush;
            HostAddresses = LocalRaceLink.Addresses();
            Status = "Waiting for the other phone…";
            _ = NewLocalLink().Listen();
        }

        public void JoinLocal(string address)
        {
            if (Connecting || Active) return;
            IsLocal = true;
            if (!IPAddress.TryParse(address?.Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork ||
                IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any))
            {
                Status = "Enter the host's IPv4 address.";
                return;
            }
            Status = "Connecting to the host…";
            _ = NewLocalLink().Connect(ip);
        }

        private LocalRaceLink NewLocalLink()
        {
            var link = new LocalRaceLink();
            localLink = link;
            Connecting = true;
            lastLocalPacket = -1f; // The host may wait in its lobby as long as needed.
            nextPing = nextLocalSnapshot = hostAccumulator = 0f;
            link.Connected += () =>
            {
                lastLocalPacket = Time.realtimeSinceStartup;
                if (localCircuit == null) link.Send(Hello);
            };
            link.Received += ReceiveLocal;
            link.Failed += LocalFailed;
            return link;
        }

        private void ReceiveLocal(string type, string json)
        {
            lastLocalPacket = Time.realtimeSinceStartup;
            if (type == Ping && localActive) return;
            if (!localActive)
            {
                if (localCircuit != null && type == Hello)
                {
                    var start = new LocalStart { geometry = localCircuit.Geometry, snapshot = localCircuit.Snapshot() };
                    localLink.Send(StartMatch, JsonUtility.ToJson(start));
                    if (localLink != null) EnterLocalRace(start, LocalCircuit.HostId);
                }
                else if (localCircuit == null && type == StartMatch)
                    EnterLocalRace(JsonUtility.FromJson<LocalStart>(json), LocalCircuit.GuestId);
                else
                    LocalFailed("Incompatible local match. Install the same game build on both phones.");
                return;
            }

            if (localCircuit != null)
            {
                ApplyLocalInput(1, type, json);
                return;
            }
            switch (type)
            {
                case "snapshot": SetLocalSnapshot(JsonUtility.FromJson<CircuitSnapshot>(json)); break;
                case "caught": Caught?.Invoke(JsonUtility.FromJson<CircuitCaught>(json)); break;
                case "respawn": Respawned?.Invoke(JsonUtility.FromJson<RaceRespawn>(json)); break;
                case "shove": Shoved?.Invoke(JsonUtility.FromJson<CircuitShove>(json)); break;
                case "pushed": Pushed?.Invoke(JsonUtility.FromJson<CircuitPushed>(json)); break;
            }
        }

        private void EnterLocalRace(LocalStart start, string id)
        {
            if (start?.geometry?.rooms == null || start.geometry.rooms.Length == 0 || start.snapshot?.Find(id) == null)
            {
                LocalFailed("The host sent an incomplete match.");
                return;
            }
            Geometry = start.geometry;
            localMatchSeconds = start.snapshot.remaining;
            SetLocalSnapshot(start.snapshot);
            SessionId = id;
            Connecting = false;
            Status = "";
            localActive = true;
            OpenMultiplayerMenu = true;
            SceneManager.LoadScene(RaceSceneName);
        }

        private void ApplyLocalInput(int slot, string type, string json)
        {
            float now = Time.unscaledTime;
            switch (type)
            {
                case "ready":
                    var ready = JsonUtility.FromJson<ReadyPacket>(json);
                    if (ready != null) localCircuit.Ready(slot, ready.body);
                    break;
                case "pos":
                    var pos = JsonUtility.FromJson<PosPacket>(json);
                    localCircuit.Position(slot, new Vector3(pos.x, pos.y, pos.z), pos.yaw, now);
                    break;
                case "push":
                    localCircuit.Push(slot, now);
                    break;
            }
        }

        private void Update()
        {
            if (localLink == null || lastLocalPacket < 0f) return;
            float now = Time.realtimeSinceStartup;
            if (now - lastLocalPacket > PeerTimeout)
            {
                LocalFailed(localActive ? "The other phone stopped responding." : "The local match handshake timed out.");
                return;
            }
            if (!localActive) return;
            if (now >= nextPing)
            {
                nextPing = now + PingInterval;
                localLink.Send(Ping);
            }
            if (localCircuit == null) return;
            string phase = localCircuit.Phase;
            // Limit catch-up after a frame hitch so one frame cannot consume the entire match clock.
            hostAccumulator += Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            while (localCircuit != null && hostAccumulator >= HostStep)
            {
                hostAccumulator -= HostStep;
                localCircuit.Tick(HostStep);
            }
            if (localCircuit == null || (now < nextLocalSnapshot && phase == localCircuit.Phase)) return;
            nextLocalSnapshot = now + SnapshotInterval;
            SetLocalSnapshot(localCircuit.Snapshot());
            localLink.Send("snapshot", JsonUtility.ToJson(Snapshot));
        }

        private void SetLocalSnapshot(CircuitSnapshot snapshot)
        {
            Snapshot = snapshot;
            localSnapshotAt = Time.unscaledTime;
        }

        private void LocalCatch(CircuitCaught caught, RaceRespawn respawn)
        {
            Caught?.Invoke(caught);
            localLink?.Send("caught", JsonUtility.ToJson(caught));
            if (caught.id == LocalCircuit.HostId) Respawned?.Invoke(respawn);
            else localLink?.Send("respawn", JsonUtility.ToJson(respawn));
        }

        private void LocalPush(CircuitShove shove, CircuitPushed pushed)
        {
            Shoved?.Invoke(shove);
            localLink?.Send("shove", JsonUtility.ToJson(shove));
            if (shove.target == LocalCircuit.HostId) Pushed?.Invoke(pushed);
            else localLink?.Send("pushed", JsonUtility.ToJson(pushed));
        }

        private void LocalFailed(string message)
        {
            if (localActive && Snapshot?.phase != "finished")
            {
                ReturnToLobby(message);
                return;
            }
            // Keep the results on screen when the other player leaves after a completed race.
            bool finished = localActive;
            CloseLocal();
            localActive = finished;
            Connecting = false;
            Status = message;
        }

        private void CloseLocal()
        {
            localLink?.Dispose();
            localLink = null;
            localCircuit = null;
            localActive = false;
            HostAddresses = Array.Empty<string>();
        }
    }
}
