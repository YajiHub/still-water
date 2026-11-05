using System;
using System.Collections.Generic;

namespace StillWater.Core
{
    /// <summary>
    /// What save.json holds (Save Data decision). Lists, not dictionaries: Unity's JsonUtility can't save dictionaries.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public List<LevelRecord> levels = new List<LevelRecord>();
        public Settings settings = new Settings();
        public bool seenTutorial;
    }

    [Serializable]
    public class LevelRecord
    {
        public string id;          // "level01": a name, so worlds can be added later
        public bool cleared;
        public int bestStars;      // 0 = never cleared
        public float bestTime;     // seconds; 0 = never cleared
    }

    [Serializable]
    public class Settings
    {
        public float sfxVolume = 1f;
        public float musicVolume = 0.8f;
        public bool vibration = true;
        public float lookSensitivity = 1f;
        public float brightness = 1f;
    }
}
