using System;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// C# mirror of server/src/logic/layout.ts (decision 16). Field names must match the JSON exactly.
    /// </summary>
    [Serializable]
    public class LevelLayout
    {
        public string id;
        public string name;
        public WaterArea water;
        public float platformTopY;
        public float exitTopY;
        public RectArea start;
        public RectArea exit;
        public PlatformSpot[] platforms;
        /// <summary>Platform ids from start to exit: the glowing pattern.</summary>
        public int[] safePath;
        /// <summary>No fakes: every non-path platform must be a dead end (server-tested).</summary>
        public bool oneDryRoute;
        /// <summary>Longest jump this level may ask for; 0 = the default 2.8 m (server-checked, never above 3.1).</summary>
        public float maxGap;

        public static LevelLayout FromJson(string json) => JsonUtility.FromJson<LevelLayout>(json);
    }

    [Serializable]
    public class WaterArea
    {
        public float y, minX, maxX, minZ, maxZ;
    }

    [Serializable]
    public class RectArea
    {
        public float x, z, width, depth;
    }

    [Serializable]
    public class PlatformSpot
    {
        public int id;
        public float x, z, size;
        /// <summary>Only on ferries. JsonUtility fills a missing one with zeros, so check <see cref="Moves"/>.</summary>
        public PlatformMove move;
        /// <summary>"crumble", "oneway", or empty (Level 4).</summary>
        public string sink;

        public bool Moves => move != null && (move.dx != 0f || move.dz != 0f) && move.seconds > 0f;
    }

    /// <summary>A ferry slides from x/z (end A) to A + (dx, dz) and back (Level Roster, Level 3).</summary>
    [Serializable]
    public class PlatformMove
    {
        public float dx, dz, seconds, pause, offset;
    }
}
