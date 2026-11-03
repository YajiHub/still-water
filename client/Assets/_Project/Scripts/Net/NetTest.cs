using System;
using Colyseus;
using UnityEngine;

namespace StillWater.Net
{
    /// <summary>
    /// Phase 0 smoke test: joins the server's default room and logs the result.
    /// Throwaway: the real NetClient replaces it in Phase 4.
    /// </summary>
    public class NetTest : MonoBehaviour
    {
        // Editor: ws://localhost:2567 · Phone: ws://<laptop Wi-Fi IP>:2567 (set in the Inspector, never hard-code it in code).
        [SerializeField] private string endpoint = "ws://localhost:2567";
        [SerializeField] private string roomName = "my_room";

        private Room<NoState> room;

        // async void is only OK for Unity event methods like Start; everything inside must catch its own errors.
        private async void Start()
        {
            Debug.Log($"[NetTest] Connecting to {endpoint} …");
            try
            {
                var client = new Client(endpoint);
                // Matchmaking: joins an open room with this name, or creates one. Non-generic = no schema codegen needed yet.
                room = await client.JoinOrCreate(roomName);

                Debug.Log($"[NetTest] Joined room {room.RoomId} as {room.SessionId}");
            }
            catch (Exception e)
            {
                // Server down, wrong IP or firewall: a clear message instead of a crash (lab R8).
                Debug.LogError($"[NetTest] Could not join: {e.Message}");
            }
        }

        private async void OnDestroy()
        {
            if (room == null) return;
            try { await room.Leave(); }
            catch (Exception e) { Debug.LogWarning($"[NetTest] Leave failed: {e.Message}"); }
        }
    }
}
