using System.Collections.Generic;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>All levels in play order. Lives in a Resources folder so any scene can load it by name.</summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Still Water/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        public LevelConfig[] levels;

        public List<string> Ids()
        {
            var ids = new List<string>();
            foreach (var level in levels)
                if (level != null) ids.Add(level.id); // a deleted config leaves an empty slot
            return ids;
        }

        /// <summary>The level after this one, or null if it's the last.</summary>
        public LevelConfig Next(LevelConfig level)
        {
            int i = System.Array.IndexOf(levels, level);
            return i >= 0 && i + 1 < levels.Length ? levels[i + 1] : null;
        }
    }
}
