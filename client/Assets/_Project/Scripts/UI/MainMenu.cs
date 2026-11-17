using StillWater.Core;
using StillWater.Net;
using StillWater.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StillWater.UI
{
    /// <summary>Title, Solo Level Select, Online co-op lobby, Settings, and Credits (lab R4/R7/R8/R10). Lives in the Menu scene.</summary>
    // shortcut: IMGUI screens; rebuild in UI Toolkit in Phase 5 polish.
    public class MainMenu : MonoBehaviour
    {
        // PlayerPrefs keys from before the Settings page (migrated into save.json once, then deleted).
        private const string OldMuteKey = "sw_mute", OldCharacterKey = "sw_player_body";

        private enum Page { Title, Levels, Local, Online, Settings, Layout, Credits }

        private Page current = Page.Title;
        private string endpointInput;
        private string roomIdInput = "";
        private string localAddressInput = "";
        private float openedAt;
        private bool settingsDirty;

        private void Awake()
        {
            if (PlayerPrefs.HasKey(OldMuteKey)) // "Sound: Off" from before Settings existed → Sound: Off
            {
                if (PlayerPrefs.GetInt(OldMuteKey, 0) == 1) GameSettings.Current.soundOn = false;
                PlayerPrefs.DeleteKey(OldMuteKey);
                GameSettings.Changed();
            }
            if (PlayerPrefs.HasKey(OldCharacterKey)) // the character picked before Settings existed
            {
                GameSettings.Current.character = PlayerPrefs.GetInt(OldCharacterKey, 0);
                PlayerPrefs.DeleteKey(OldCharacterKey);
                GameSettings.Changed();
            }
            GameSettings.Apply();
            AudioListener.pause = false;
            openedAt = Time.unscaledTime;
            MusicDirector.Instance.Play(MusicDirector.Menu);
            if (LanRace.OpenMultiplayerMenu)
            {
                LanRace.OpenMultiplayerMenu = false;
                current = LanRace.Instance != null && LanRace.Instance.IsLocal ? Page.Local : Page.Online;
            }
        }

        private void OnGUI()
        {
            // The poolroom backdrop (Build Menu Backdrop) shows through: darken the edges, and more behind the sub-pages.
            Gui.Vignette();
            if (current != Page.Title) Gui.Dim(0.45f);
            Gui.LeftColumn = current == Page.Title; // the title page sits on the left, so the pool and the Listener show
            DrawPage();
            Gui.LeftColumn = false;
            Gui.FadeIn(openedAt, 2.5f);
        }

        private void DrawPage()
        {
            switch (current)
            {
                case Page.Title:
                    Gui.BigTitle(2.0f, "STILL WATER", 2.4f);
                    Gui.Whisper(4.5f, "Don't make a sound.", 0.55f + 0.45f * Mathf.PingPong(Time.unscaledTime * 0.25f, 1f));
                    if (Gui.Button(7.4f, "Solo", 0.26f)) { LanRace.Cancel(); current = Page.Levels; }
                    if (Gui.Button(9.0f, "Local Multiplayer", 0.30f)) OpenMultiplayer(Page.Local);
                    if (Gui.Button(10.6f, "Online", 0.26f)) OpenMultiplayer(Page.Online);
                    if (Gui.Button(12.2f, "Settings", 0.26f)) current = Page.Settings;
                    if (Gui.Button(13.8f, "Credits", 0.26f)) current = Page.Credits;
                    if (Gui.Button(15.4f, "Quit", 0.26f)) Application.Quit();
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
                        if (Gui.Button(4.5f + i * 2.2f, label, 0.5f)) SceneManager.LoadScene(level.sceneName);
                        GUI.enabled = true;
                    }
                    if (Gui.Button(17.5f, "Back", 0.25f)) current = Page.Title;
                    break;

                case Page.Local:
                    DrawLocal();
                    break;

                case Page.Online:
                    DrawOnline();
                    break;

                case Page.Settings:
                    DrawSettings();
                    break;

                case Page.Layout:
                    DrawLayout();
                    break;

                case Page.Credits:
                    Gui.Text(1.2f, "Credits", Gui.Title, 2.2f);
                    Gui.Text(4.2f, "Engine & Net: Unity 6 · Colyseus (MIT) · Node.js · Docker");
                    Gui.Text(5.8f, "Player Rig: Unity Starter Assets (Asset Store EULA)");
                    Gui.Text(7.4f, "Player Bodies & Animations: Quaternius (CC0 1.0) · Listener & Music: original");
                    Gui.Text(9.0f, "Pool Tiles: ambientCG Tiles020 (CC0 1.0)");
                    Gui.Text(10.6f, "Splash Audio: Universfield — Water Splash 02 (Pixabay)");
                    if (Gui.Button(17.5f, "Back", 0.25f)) current = Page.Title;
                    break;
            }
        }

        // Sound, controls, camera and character. Sliders apply live; save.json is written once when you leave the page.
        private void DrawSettings()
        {
            var s = GameSettings.Current;
            Gui.Text(0.8f, "Settings", Gui.Title, 2.2f);

            Gui.Whisper(3.0f, "SOUND", 0.7f);
            if (Gui.Button(4.0f, GameSettings.SoundLabel, 0.62f)) GameSettings.ToggleSound(); // saves at once
            GUI.enabled = s.soundOn; // the sliders do nothing while the sound is off
            float master = Gui.Slider(5.3f, "Master volume", s.masterVolume, 0f, 1f, $"{Mathf.RoundToInt(s.masterVolume * 100)}%");
            float music = Gui.Slider(6.6f, "Music", s.musicVolume, 0f, 1f, $"{Mathf.RoundToInt(s.musicVolume * 100)}%");
            GUI.enabled = true;

            Gui.Whisper(7.9f, "CONTROLS", 0.7f);
            float look = Gui.Slider(8.9f, "Look sensitivity", s.lookSensitivity, GameSettings.MinLook, GameSettings.MaxLook, $"{s.lookSensitivity:0.0}×");
            if (Gui.Button(10.2f, s.customLayout ? "Edit control layout (custom)" : "Edit control layout", 0.62f))
            {
                current = Page.Layout; // button size lives there too, next to what it changes
            }
            if (Gui.Button(11.5f, s.leftHanded ? "Layout: left-handed (stick right)" : "Layout: right-handed (stick left)", 0.62f))
            {
                s.leftHanded = !s.leftHanded;
                settingsDirty = true;
            }
            if (Gui.Button(12.8f, s.firstPerson ? "Camera: first person" : "Camera: third person", 0.62f))
            {
                s.firstPerson = !s.firstPerson;
                settingsDirty = true;
            }

            Gui.Whisper(14.2f, "PLAYER", 0.7f);
            if (Gui.Button(15.2f, $"Character: {PlayerAvatar.Bodies[PlayerAvatar.Choice]}", 0.62f))
                PlayerAvatar.Choice = (PlayerAvatar.Choice + 1) % PlayerAvatar.Bodies.Length; // saves at once

            if (!Mathf.Approximately(master, s.masterVolume) || !Mathf.Approximately(music, s.musicVolume) ||
                !Mathf.Approximately(look, s.lookSensitivity))
            {
                s.masterVolume = master;
                s.musicVolume = music;
                s.lookSensitivity = look;
                GameSettings.Apply(); // hear the volume change while dragging
                settingsDirty = true;
            }

            if (Gui.Button(17.6f, "Back", 0.25f))
            {
                SaveSettings();
                current = Page.Title;
            }
        }

        // The control layout editor: the in-game controls drawn where they'll be; drag the stick, JUMP and PUSH anywhere.
        // Pause and the camera pill stay in the top corners. Saved to save.json on Done (ControlLayout keeps the maths).
        private ControlLayout.Item? dragging;
        private Vector2 grabOffset;
        private static readonly ControlLayout.Item[] Draggable = { ControlLayout.Item.Push, ControlLayout.Item.Jump, ControlLayout.Item.Stick };

        private void DrawLayout()
        {
            var s = GameSettings.Current;
            Event e = Event.current;
            Vector2 p = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y); // IMGUI → touch coordinates
            if (e.type == EventType.MouseDown)
            {
                foreach (var item in Draggable)
                {
                    Vector2 c = ControlLayout.Center(item);
                    if (Vector2.Distance(p, c) > ControlLayout.Radius(item) * 1.2f) continue;
                    dragging = item;
                    grabOffset = c - p;
                    e.Use();
                    break;
                }
            }
            else if (e.type == EventType.MouseDrag && dragging != null)
            {
                ControlLayout.Move(dragging.Value, p + grabOffset);
                settingsDirty = true;
                e.Use();
            }
            else if (e.rawType == EventType.MouseUp) dragging = null;

            // The controls, as in a level.
            Vector2 stick = ControlLayout.Center(ControlLayout.Item.Stick);
            ControlLayout.DrawStick(stick, stick, dragging == ControlLayout.Item.Stick ? 1f : 0.85f);
            GUI.Label(ControlLayout.ToGui(stick - new Vector2(0f, ControlLayout.StickRadius * 0.62f), ControlLayout.StickRadius * 0.5f), "MOVE", ControlLayout.Small);
            ControlLayout.RoundButton(ControlLayout.Center(ControlLayout.Item.Jump), ControlLayout.Radius(ControlLayout.Item.Jump), "JUMP",
                                      ControlLayout.Cyan, dragging == ControlLayout.Item.Jump, true, false);
            ControlLayout.RoundButton(ControlLayout.Center(ControlLayout.Item.Push), ControlLayout.Radius(ControlLayout.Item.Push), "PUSH",
                                      ControlLayout.Coral, dragging == ControlLayout.Item.Push, true, false);
            Rect pr = ControlLayout.PauseRect;
            ControlLayout.RoundButton(pr.center, pr.width / 2f, "II", ControlLayout.Cyan, false, false, false);
            ControlLayout.Pill(ControlLayout.ToggleRect, "camera");

            Gui.Text(1.0f, "Drag the stick, JUMP and PUSH where your thumbs want them");
            Gui.Whisper(2.1f, s.leftHanded ? "Left-handed: mirrored. PUSH shows in multiplayer only." : "PUSH shows in multiplayer only.", 0.6f);
            float size = Gui.Slider(3.3f, "Button size", s.controlsScale, GameSettings.MinControlsScale, GameSettings.MaxControlsScale, $"{Mathf.RoundToInt(s.controlsScale * 100)}%");
            if (!Mathf.Approximately(size, s.controlsScale))
            {
                s.controlsScale = size;
                settingsDirty = true;
            }
            if (Gui.Button(4.7f, s.fixedStick ? "Stick: fixed (stays where you put it)" : "Stick: floating (appears under your thumb)", 0.62f))
            {
                s.fixedStick = !s.fixedStick;
                settingsDirty = true;
            }
            if (Gui.Button(6.1f, "Reset to default", 0.3f))
            {
                ControlLayout.ResetLayout();
                settingsDirty = true;
            }
            if (Gui.Button(7.5f, "Done", 0.25f))
            {
                dragging = null;
                SaveSettings();
                current = Page.Settings;
            }
        }

        private void SaveSettings()
        {
            if (!settingsDirty) return;
            settingsDirty = false;
            GameSettings.Changed();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveSettings(); // a phone going to the background mid-Settings still keeps the changes
        }

        private void OnDestroy() => SaveSettings();

        private void OpenMultiplayer(Page page)
        {
            LanRace.Ensure().SelectMultiplayerMode(page == Page.Local);
            current = page;
        }

        private void DrawLocal()
        {
            var net = LanRace.Ensure();
            Gui.Text(0.8f, "Local multiplayer (2 players)", Gui.Title, 2.2f);
            Gui.Text(3.0f, "Turn on the host phone's hotspot and connect the other phone.");
            GUI.enabled = !net.Connecting;
            if (Gui.Button(5f, "Host")) net.HostLocal();
            Gui.Text(7f, "Host IP address");
            localAddressInput = Gui.Field(8.1f, localAddressInput, 0.45f);
            GUI.enabled = !net.Connecting && !string.IsNullOrWhiteSpace(localAddressInput);
            if (Gui.Button(10f, "Join")) net.JoinLocal(localAddressInput);
            GUI.enabled = true;
            if (net.HostAddresses.Length > 0)
                Gui.Text(11.8f, "Host IP: " + string.Join(", ", net.HostAddresses), height: 1.5f);
            if (net.Connecting)
                Gui.Text(13.2f, "The guest can find the host IP as Gateway in its hotspot Wi-Fi details.");
            if (!string.IsNullOrEmpty(net.Status))
                Gui.Text(14.5f, net.Status);
            if (net.Connecting && Gui.Button(16f, "Cancel", 0.3f)) net.CancelConnect();
            if (Gui.Button(17.6f, "Back", 0.28f)) { net.CancelConnect(); current = Page.Title; }
        }

        private void DrawOnline()
        {
            var net = LanRace.Ensure();
            if (endpointInput == null) endpointInput = net.Endpoint;
            Gui.Text(0.8f, "Online co-op (2-4)", Gui.Title, 2.2f);
            Gui.Text(3.3f, "Server URL (ws://<laptop IP>:2567 or wss://<host>)");
            GUI.enabled = !net.Connecting;
            endpointInput = Gui.Field(4.5f, endpointInput);
            if (Gui.Button(6.3f, "Quick Match")) { net.Endpoint = endpointInput; endpointInput = net.Endpoint; net.QuickMatch(); }
            Gui.Text(8.4f, "Room ID (optional)");
            roomIdInput = Gui.Field(9.6f, roomIdInput, 0.35f);
            GUI.enabled = !net.Connecting && !string.IsNullOrWhiteSpace(roomIdInput);
            if (Gui.Button(11.3f, "Join Room")) { net.Endpoint = endpointInput; endpointInput = net.Endpoint; net.JoinRoom(roomIdInput); }
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(net.Status)) Gui.Text(13.3f, net.Status, height: 1.8f);
            if (net.Connecting && Gui.Button(15.5f, "Cancel", 0.3f)) net.CancelConnect();
            if (Gui.Button(17.6f, "Back", 0.28f)) { net.CancelConnect(); current = Page.Title; }
        }
    }
}
