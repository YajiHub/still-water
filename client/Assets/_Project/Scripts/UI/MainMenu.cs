using StillWater.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StillWater.UI
{
    /// <summary>Title + Level Select with locks and best stars (lab R4). Lives in the Menu scene.</summary>
    // shortcut: IMGUI grey-box screens; rebuild in UI Toolkit in Phase 5 polish.
    public class MainMenu : MonoBehaviour
    {
        private enum Page { Title, Levels, ConfirmReset }

        private Page current = Page.Title;

        private void OnGUI()
        {
            switch (current)
            {
                case Page.Title:
                    Gui.Text(3f, "STILL WATER", Gui.Title, 3f);
                    Gui.Text(6.5f, "Don't make a sound.");
                    if (Gui.Button(10f, "Play")) current = Page.Levels;
                    if (Gui.Button(12.2f, "Quit")) Application.Quit();
                    if (Gui.Button(17.5f, "Reset progress (debug)", 0.3f)) current = Page.ConfirmReset;
                    break;

                case Page.Levels:
                    Gui.Text(1f, "Select level", Gui.Title, 2.5f);
                    var levels = GameState.Catalog.levels;
                    for (int i = 0; i < levels.Length; i++)
                    {
                        var level = levels[i];
                        if (level == null) continue; // a deleted level config
                        bool open = GameState.IsUnlocked(level);
                        var record = GameState.RecordFor(level);
                        string stars = record != null && record.cleared ? $"   Stars {record.bestStars}/3" : "";
                        string label = open ? $"{i + 1}. {level.displayName}{stars}" : $"{i + 1}. Locked";
                        GUI.enabled = open;
                        if (Gui.Button(4.5f + i * 2.2f, label, 0.55f)) SceneManager.LoadScene(level.sceneName);
                        GUI.enabled = true;
                    }
                    if (Gui.Button(17.5f, "Back", 0.25f)) current = Page.Title;
                    break;

                case Page.ConfirmReset:
                    Gui.Text(5f, "Erase all progress?", Gui.Title, 2.5f);
                    if (Gui.Button(9f, "Yes, reset")) { GameState.ResetProgress(); current = Page.Title; }
                    if (Gui.Button(11.2f, "Cancel")) current = Page.Title;
                    break;
            }
        }
    }
}
