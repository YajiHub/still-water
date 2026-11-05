using StillWater.Level;
using UnityEngine;

namespace StillWater.UI
{
    /// <summary>Level overlay: goal text, timer + splashes, Pause menu and the Result screen (lab R3).</summary>
    // shortcut: IMGUI grey-box screens; rebuild in UI Toolkit in Phase 5 polish.
    public class LevelHud : MonoBehaviour
    {
        [SerializeField] private LevelController level;

        private void OnGUI()
        {
            var config = level.Config;
            switch (level.Phase)
            {
                case LevelController.LevelPhase.Intro:
                    Gui.Text(6f, config.displayName, Gui.Title, 2.5f);
                    Gui.Text(9f, config.goalText);
                    break;

                case LevelController.LevelPhase.Reveal:
                    Gui.Text(1f, "Remember the glowing path...");
                    break;

                case LevelController.LevelPhase.Crossing:
                    int seconds = Mathf.CeilToInt(level.TimeLeft);
                    Gui.Text(0.4f, $"{seconds / 60}:{seconds % 60:00}    Splashes {level.Splashes}");
                    if (level.Paused) PauseMenu();
                    break;

                case LevelController.LevelPhase.Won:
                    Gui.Dim();
                    Gui.Text(2.5f, "Cleared!", Gui.Title, 2.5f);
                    Gui.Text(6f, $"Stars {level.Stars} / 3");
                    Gui.Text(7.5f, $"Time {level.Elapsed:0.0} s   (par {config.parTime:0} s)    Splashes {level.Splashes}");
                    float row = 10f;
                    if (level.HasNextLevel && Gui.Button(row, "Next")) level.NextLevel();
                    if (level.HasNextLevel) row += 2.2f;
                    if (Gui.Button(row, "Retry")) level.Retry();
                    if (Gui.Button(row + 2.2f, "Menu")) level.ToMenu();
                    break;

                case LevelController.LevelPhase.Lost:
                    Gui.Dim();
                    Gui.Text(3f, "You didn't make it", Gui.Title, 2.5f);
                    Gui.Text(6.5f, level.LoseReason);
                    if (Gui.Button(10f, "Retry")) level.Retry();
                    if (Gui.Button(12.2f, "Menu")) level.ToMenu();
                    break;
            }
        }

        private void PauseMenu()
        {
            Gui.Dim();
            Gui.Text(3f, "Paused", Gui.Title, 2.5f);
            if (Gui.Button(8f, "Resume")) level.TogglePause();
            if (Gui.Button(10.2f, "Retry")) level.Retry();
            if (Gui.Button(12.4f, "Menu")) level.ToMenu();
        }
    }
}
