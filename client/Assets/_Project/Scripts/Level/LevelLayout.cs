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
    }
}
