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

    /// <summary>
    /// The Settings page (Main Menu → Settings), saved as soon as something changes. New fields get their defaults
    /// when an older save.json is read (JsonUtility keeps initializers for missing fields).
    /// </summary>
    [Serializable]
    public class Settings
    {
        public float sfxVolume = 1f;       // not on the page yet: there's no separate effects bus (masterVolume covers it)
        public float musicVolume = 0.8f;   // music, relative to the master volume
        public bool vibration = true;      // not used yet
        public float lookSensitivity = 1f; // × the drag-to-look speed
        public float brightness = 1f;      // not used yet
        public float masterVolume = 1f;    // everything (AudioListener.volume)
        public float controlsScale = 1f;   // × the touch controls' size
        public bool leftHanded;            // stick on the right, JUMP/PUSH on the left
        public bool firstPerson = true;    // the camera you start in (the in-game 1st/3rd pill changes it too)
        public int character;              // 0 female, 1 male (PlayerAvatar.Bodies)
        public bool soundOn = true;        // Sound On/Off (Settings and the pause menus, lab R10); off is silent but keeps the volumes
    }
}
