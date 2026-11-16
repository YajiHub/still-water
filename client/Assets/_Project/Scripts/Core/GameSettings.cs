using UnityEngine;

namespace StillWater.Core
{
    /// <summary>
    /// The player's settings (Save Data decision: they live in save.json and save when changed). The Settings page
    /// edits <see cref="Current"/> and calls <see cref="Changed"/>. The game reads them where they matter:
    /// - sound: here (AudioListener) and MusicDirector;
    /// - look sensitivity, control size and left-handed layout: TouchControls;
    /// - camera: CameraModeSwitch;
    /// - character: PlayerAvatar.
    /// </summary>
    public static class GameSettings
    {
        // Slider ranges on the Settings page.
        public const float MinControlsScale = 0.75f, MaxControlsScale = 1.5f, MinLook = 0.4f, MaxLook = 2.5f;

        public static Settings Current => GameState.Save.settings ??= new Settings();

        /// <summary>Apply the sound settings now and write save.json.</summary>
        public static void Changed()
        {
            Apply();
            SaveSystem.Save(GameState.Save, Application.persistentDataPath);
        }

        /// <summary>Apply what isn't read live (Sound On/Off and the master volume). Called when the menu opens and on every change.</summary>
        public static void Apply()
        {
            AudioListener.volume = Current.soundOn ? Mathf.Clamp01(Current.masterVolume) : 0f;
        }

        /// <summary>Sound On/Off: flips it, applies it at once and saves (used by Settings and the pause menus).</summary>
        public static void ToggleSound()
        {
            Current.soundOn = !Current.soundOn;
            Changed();
        }

        public static string SoundLabel => Current.soundOn ? "Sound: On" : "Sound: Off";
    }
}
