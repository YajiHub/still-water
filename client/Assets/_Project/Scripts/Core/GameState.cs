using StillWater.Level;
using UnityEngine;

namespace StillWater.Core
{
    /// <summary>
    /// Progress shared by every scene (SYSTEM_ATLAS §3): the loaded save + the level catalog.
    /// Static, so it survives scene changes without a DontDestroyOnLoad object.
    /// </summary>
    public static class GameState
    {
        public const string MenuScene = "Menu";

        private static SaveData save;
        private static LevelCatalog catalog;

        public static SaveData Save => save ??= SaveSystem.Load(Application.persistentDataPath);
        public static LevelCatalog Catalog => catalog ??= Resources.Load<LevelCatalog>("LevelCatalog");

        public static bool IsUnlocked(LevelConfig level) => ProgressRules.IsUnlocked(level.id, Catalog.Ids(), Save.levels);

        public static LevelRecord RecordFor(LevelConfig level) => ProgressRules.FindRecord(Save.levels, level.id);

        /// <summary>Merge a win into the save and write it to disk (before the Result screen, lab R5).</summary>
        public static void RecordWin(LevelConfig level, int stars, float time)
        {
            var merged = ProgressRules.MergeBest(RecordFor(level), level.id, stars, time);
            Save.levels.RemoveAll(r => r.id == level.id);
            Save.levels.Add(merged);
            SaveSystem.Save(Save, Application.persistentDataPath);
        }

        /// <summary>Debug: wipe all progress (Main Menu → Reset progress).</summary>
        public static void ResetProgress()
        {
            save = new SaveData();
            SaveSystem.Save(save, Application.persistentDataPath);
        }
    }
}
