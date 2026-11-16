using System;

namespace StillWater.Net
{
    // Wire names match the server; JSON strings avoid runtime schema generation on Android IL2CPP.
    [Serializable]
    public class RaceSnapshot
    {
        public string phase, winner, reason;
        public float countdown, remaining;
        public RacePlayer[] players;

        public RacePlayer Find(string id)
        {
            if (players != null) foreach (var p in players) if (p != null && p.id == id) return p;
            return null;
        }
    }

    [Serializable]
    public class RacePlayer
    {
        public string id, monsterState;
        public float x, y, z, yaw, penalty, monsterX, monsterZ;
        public int splashes;
    }

    [Serializable]
    public class RaceRespawn
    {
        public float x, y, z;
    }
}
