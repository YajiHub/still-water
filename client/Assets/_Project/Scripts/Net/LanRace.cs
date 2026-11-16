using System;
using System.Threading.Tasks;
using Colyseus;
using StillWater.Core;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace StillWater.Net
{
    /// <summary>
    /// Persistent LAN multiplayer session (DontDestroyOnLoad) for the 2-4 player co-op circuit (decision 07). Created
    /// only when the player opens Online, so offline play never initializes Colyseus or touches the network.
    /// </summary>
    public class LanRace : MonoBehaviour
    {
        private const string EndpointKey = "sw_endpoint";
        private const string DefaultEndpoint = "ws://localhost:2567";
        private const string RoomName = "circuit";
        private const string RaceSceneName = "Level01";
        private const int ConnectTimeoutMs = 5000;
        // An internet server (wss://) on a free host sleeps when idle and needs about a minute to wake up
        // (render.yaml): ask its /health until it answers, for up to this long, before joining.
        private const float WakeTimeoutSeconds = 90f, WakePollSeconds = 2f;

        public static LanRace Instance { get; private set; }
        public static bool Active => Instance != null && Instance.room != null;
        public static bool OpenOnlineMenu { get; set; }

        public string Status { get; private set; } = "";
        public bool Connecting { get; private set; }
        public string SessionId { get; private set; }
        public string RoomId { get; private set; }
        public CircuitSnapshot Snapshot { get; private set; }
        /// <summary>Where each room of the circuit sits (sent once on join).</summary>
        public CircuitGeometry Geometry { get; private set; }

        public event Action<RaceRespawn> Respawned;
        /// <summary>Someone (maybe you) was caught: play the catch there.</summary>
        public event Action<CircuitCaught> Caught;
        /// <summary>Someone pushed you.</summary>
        public event Action<CircuitPushed> Pushed;
        /// <summary>Someone pushed someone (for the sound).</summary>
        public event Action<CircuitShove> Shoved;

        private Room<NoState> room;
        private string endpoint = DefaultEndpoint;
        private int operationId;

        [Serializable]
        private struct PosPacket
        {
            public float x, y, z, yaw;
        }

        public static LanRace Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("LanRace");
            return go.AddComponent<LanRace>();
        }

        public static void Cancel()
        {
            if (Instance != null) Instance.CancelConnect();
        }

        public string Endpoint
        {
            get => endpoint;
            set
            {
                string clean = string.IsNullOrWhiteSpace(value) ? DefaultEndpoint : value.Trim();
                if (clean == endpoint) return;
                endpoint = clean;
                PlayerPrefs.SetString(EndpointKey, endpoint);
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            endpoint = PlayerPrefs.GetString(EndpointKey, DefaultEndpoint);
        }

        public void QuickMatch() => _ = ConnectAsync(null);

        public void JoinRoom(string roomId)
        {
            string trimmed = roomId?.Trim() ?? "";
            if (string.IsNullOrEmpty(trimmed))
            {
                Status = "Enter a room ID.";
                return;
            }
            _ = ConnectAsync(trimmed);
        }

        public void CancelConnect()
        {
            operationId++;
            if (Connecting)
            {
                Connecting = false;
                Status = "Cancelled.";
            }
        }

        /// <summary>Loaded and ready; tells the others which character you picked.</summary>
        public void Ready(int body) => SendSafe("ready", "{\"body\":" + body + "}");

        /// <summary>Shove whoever is right in front of you (the server checks range, facing and cooldown).</summary>
        public void Push() => SendSafe("push", null);

        public void Position(Vector3 p, float yaw) =>
            SendSafe("pos", JsonUtility.ToJson(new PosPacket { x = p.x, y = p.y, z = p.z, yaw = yaw }));

        public void ReturnToLobby(string message = "")
        {
            operationId++;
            Connecting = false;
            var old = room;
            room = null;
            SessionId = null;
            RoomId = null;
            Snapshot = null;
            Geometry = null;
            Status = message ?? "";
            OpenOnlineMenu = true;

            _ = SafeLeave(old);
            if (this != null && SceneManager.GetActiveScene().name != GameState.MenuScene)
                SceneManager.LoadScene(GameState.MenuScene);
        }

        private async Task ConnectAsync(string roomId)
        {
            int op = ++operationId;
            Connecting = true;
            Status = "Connecting...";

            string url = endpoint.Trim().TrimEnd('/');
            // A host's dashboard shows https://…; the game speaks WebSocket over the same address.
            if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) url = "wss://" + url.Substring("https://".Length);
            else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) url = "ws://" + url.Substring("http://".Length);
            if (!url.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                url = "ws://" + url;
            }

            if (url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase) && !await WakeAsync(url, op))
            {
                if (op == operationId)
                {
                    Status = "The server didn't wake up in 90 s. Check the address, or try again.";
                    Connecting = false;
                }
                return;
            }

            Room<NoState> joined;
            try
            {
                var client = new Client(url);
                Task<Room<NoState>> joinTask = roomId == null
                    ? client.JoinOrCreate(RoomName)
                    : client.JoinById(roomId);
                Task done = await Task.WhenAny(joinTask, Task.Delay(ConnectTimeoutMs));
                if (done != joinTask)
                {
                    _ = ObserveAndCloseLateRoom(joinTask);
                    if (op == operationId) Status = "Connection timed out (5 s). Check server and Wi-Fi.";
                    return;
                }
                joined = await joinTask;
                if (op != operationId || this == null)
                {
                    await SafeLeave(joined);
                    return;
                }
            }
            catch (Exception e)
            {
                if (op == operationId) Status = $"Could not connect: {FriendlyError(e)}";
                return;
            }
            finally
            {
                if (op == operationId) Connecting = false;
            }

            await SafeLeave(room);
            if (op != operationId || this == null)
            {
                await SafeLeave(joined);
                return;
            }

            room = joined;
            room.Reconnection.Enabled = false;
            SessionId = room.SessionId;
            RoomId = room.RoomId;
            Snapshot = null;
            Geometry = null;
            Status = "";

            room.OnMessage<string>("snapshot", json =>
            {
                if (room == joined && !string.IsNullOrEmpty(json))
                    Snapshot = JsonUtility.FromJson<CircuitSnapshot>(json);
            });
            room.OnMessage<string>("caught", json =>
            {
                if (room == joined && !string.IsNullOrEmpty(json)) Caught?.Invoke(JsonUtility.FromJson<CircuitCaught>(json));
            });
            room.OnMessage<string>("pushed", json =>
            {
                if (room == joined && !string.IsNullOrEmpty(json)) Pushed?.Invoke(JsonUtility.FromJson<CircuitPushed>(json));
            });
            room.OnMessage<string>("shove", json =>
            {
                if (room == joined && !string.IsNullOrEmpty(json)) Shoved?.Invoke(JsonUtility.FromJson<CircuitShove>(json));
            });
            room.OnMessage<string>("circuit", json =>
            {
                if (room == joined && !string.IsNullOrEmpty(json))
                    Geometry = JsonUtility.FromJson<CircuitGeometry>(json);
            });
            room.OnMessage<string>("respawn", json =>
            {
                if (room == joined && !string.IsNullOrEmpty(json))
                    Respawned?.Invoke(JsonUtility.FromJson<RaceRespawn>(json));
            });
            room.OnError += (code, msg) =>
            {
                if (room == joined) ReturnToLobby($"Connection error: {msg}");
            };
            room.OnLeave += code =>
            {
                if (room != joined) return;
                if (Snapshot?.phase != "finished") ReturnToLobby("Disconnected from server.");
            };

            SendSafe("geometry", null); // handlers are registered now: ask for the rooms (the join-time copy may be lost)
            OpenOnlineMenu = true;
            SceneManager.LoadScene(RaceSceneName);
        }

        // Polls https://<host>/health until it answers 200 (the free host is awake), showing progress in Status.
        // False if it never did, or the player cancelled (operationId moved on).
        private async Task<bool> WakeAsync(string wsUrl, int op)
        {
            string health = "https://" + wsUrl.Substring("wss://".Length).TrimEnd('/') + "/health";
            float start = Time.realtimeSinceStartup;
            while (op == operationId && this != null)
            {
                using (var req = UnityWebRequest.Get(health))
                {
                    req.timeout = 10;
                    var send = req.SendWebRequest();
                    while (!send.isDone) await Task.Yield();
                    if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200) return true;
                }
                float waited = Time.realtimeSinceStartup - start;
                if (waited >= WakeTimeoutSeconds) return false;
                if (op == operationId) Status = $"Waking the server (free hosting sleeps when nobody plays)... {Mathf.CeilToInt(waited)} s";
                await Task.Delay(TimeSpan.FromSeconds(WakePollSeconds));
            }
            return false;
        }

        private async void SendSafe(string type, string payload)
        {
            if (room == null) return;
            try
            {
                if (payload == null) await room.Send(type);
                else await room.Send(type, payload);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanRace] Send '{type}' failed: {e.Message}");
            }
        }

        private static async Task ObserveAndCloseLateRoom(Task<Room<NoState>> task)
        {
            try
            {
                var late = await task;
                await SafeLeave(late);
            }
            catch
            {
                // Intentionally ignored after timeout/cancellation so background failures do not leak.
            }
        }

        private static async Task SafeLeave(Room<NoState> target)
        {
            if (target == null) return;
            try { await target.Leave(true); }
            catch { }
        }

        private static string FriendlyError(Exception e)
        {
            string msg = e.InnerException?.Message ?? e.Message;
            return string.IsNullOrWhiteSpace(msg) ? "Server unreachable." : msg;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            var old = room;
            room = null;
            _ = SafeLeave(old);
        }
    }
}
