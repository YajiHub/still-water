using System;
using System.IO;
using UnityEngine;

namespace StillWater.Core
{
    /// <summary>
    /// Reads and writes save.json (Save Data decision). File I/O only; the rules live in ProgressRules.
    /// The game passes Application.persistentDataPath (the app's private folder); tests pass a temp folder.
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "save.json";
        private const int CurrentVersion = 1;

        /// <summary>The saved progress, or fresh defaults if the file is missing, empty, broken or from an unknown version. Never throws.</summary>
        public static SaveData Load(string folder)
        {
            string path = Path.Combine(folder, FileName);
            if (!File.Exists(path)) return new SaveData();

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version != CurrentVersion)
                {
                    Debug.LogWarning($"Save file {path} is empty or version {data?.version}; starting fresh.");
                    return new SaveData();
                }
                data.levels ??= new System.Collections.Generic.List<LevelRecord>();
                data.settings ??= new Settings();
                return data;
            }
            catch (Exception e) // broken JSON: lose the progress, but never crash the game
            {
                Debug.LogWarning($"Save file {path} is unreadable ({e.Message}); starting fresh.");
                return new SaveData();
            }
        }

        /// <summary>Writes a temp file first, then swaps it in, so a crash mid-save can't leave a half-written save.</summary>
        public static void Save(SaveData data, string folder)
        {
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, FileName);
            string temp = path + ".tmp";

            data.version = CurrentVersion;
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
    }
}
