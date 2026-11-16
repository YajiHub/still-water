using System;

namespace StillWater.Net
{
    // The online co-op race circuit (server/src/logic/circuit.ts). Wire names match the server; JSON strings avoid
    // runtime schema generation on Android IL2CPP.
    [Serializable]
    public class CircuitSnapshot
    {
        public string phase, result, reason;
        public float lobby, countdown, remaining;
        public int roomCount;
        public CircuitListenerState[] listeners; // one per room
        public CircuitPlayer[] players;

        public CircuitPlayer Find(string id)
        {
            if (players != null) foreach (var p in players) if (p != null && p.id == id) return p;
            return null;
        }
    }

    [Serializable]
    public class CircuitListenerState
    {
        public int room;
        public float x, z;
        public string state;
    }

    [Serializable]
    public class CircuitPlayer
    {
        public string id;
        public int slot, body, room, reached, place, splashes, catches, score;
        public float x, y, z, yaw, penalty;
    }

    /// <summary>Someone was caught: every client plays the catch there ("caught").</summary>
    [Serializable]
    public class CircuitCaught
    {
        public string id;
        public float x, z;
        public int room;
    }

    /// <summary>You were pushed: fly this way ("pushed").</summary>
    [Serializable]
    public class CircuitPushed
    {
        public float dx, dz, speed;
    }

    /// <summary>Someone pushed someone ("shove"): for the sound.</summary>
    [Serializable]
    public class CircuitShove
    {
        public string pusher, target;
    }

    /// <summary>The rooms' geometry, sent on request ("circuit"): where each level layout sits in the world.</summary>
    [Serializable]
    public class CircuitGeometry
    {
        public CircuitRoomGeometry[] rooms;
    }

    [Serializable]
    public class CircuitRoomGeometry
    {
        public int index;
        public string id, name;
        public float offsetX, offsetZ, minZ, maxZ, exitTopY;
        public Vec3 spawn, door;
        public WaterBox water;
    }

    [Serializable]
    public class WaterBox
    {
        public float surfaceY, minX, maxX, minZ, maxZ;
    }

    [Serializable]
    public class Vec3
    {
        public float x, y, z;
    }
}
