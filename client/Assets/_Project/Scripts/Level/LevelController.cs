using System.Collections;
using System.Collections.Generic;
using StillWater.Core;
using StillWater.Monster;
using StillWater.Player;
using StillWater.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StillWater.Level
{
    /// <summary>
    /// Runs one level (Core Loop Rules): Intro → Reveal → Crossing ⇄ in water → Won | Lost, plus Pause.
    /// One reusable script for every level; the differences live in its LevelConfig.
    /// Scene objects are found at runtime, so rebuilding the level never leaves stale links.
    /// </summary>
    [RequireComponent(typeof(PatternRevealer))]
    public class LevelController : MonoBehaviour
    {
        public enum LevelPhase { Intro, Reveal, Crossing, Won, Lost }

        [SerializeField] private LevelConfig config;

        private const float DoorReach = 1.6f; // metres from the door that count as "through it"

        public LevelConfig Config => config;
        public LevelPhase Phase { get; private set; } = LevelPhase.Intro;
        public bool Paused { get; private set; }
        public float TimeLeft { get; private set; }
        public float Elapsed { get; private set; }
        public int Splashes { get; private set; }
        public int Stars { get; private set; }
        public string LoseReason { get; private set; }
        public bool HasNextLevel => GameState.Catalog != null && GameState.Catalog.Next(config) != null;

        private PlayerMotor motor;
        private TouchControls touch;
        private CameraModeSwitch cameras;
        private ListenerAI listener;
        private Transform door;
        private readonly List<Platform> path = new List<Platform>();

        private IEnumerator Start()
        {
            motor = FindFirstObjectByType<PlayerMotor>();
            touch = FindFirstObjectByType<TouchControls>();
            cameras = FindFirstObjectByType<CameraModeSwitch>();
            listener = FindFirstObjectByType<ListenerAI>();
            door = FindFirstObjectByType<ExitDoor>().transform;
            var water = FindFirstObjectByType<PoolWater>();

            var byId = new Dictionary<int, Platform>();
            foreach (var p in FindObjectsByType<Platform>(FindObjectsSortMode.None)) byId[p.Id] = p;
            foreach (int id in LevelLayout.FromJson(config.layout.text).safePath) path.Add(byId[id]);
            if (config.fakePlatforms)
                foreach (var p in byId.Values)
                    if (p.Id > 0 && !p.OnSafePath) p.MakeFake(); // ids 0 (start) and -1 (exit) are decks

            motor.Splashed += OnSplash;
            listener.Init(config, motor, water);
            listener.Caught += () => Lose("The Listener caught you.");
            touch.PausePressed += TogglePause;

            yield return null; // let every other Start run first (the camera rigs), then take the camera
            motor.InputEnabled = false;
            touch.enabled = false;

            if (config.revealPath)
            {
                cameras.DisableAll();
                var revealer = GetComponent<PatternRevealer>();
                Transform cam = Camera.main.transform;
                revealer.MoveToOverhead(config, cam, byId[0].transform, path);
                yield return new WaitForSeconds(config.introSeconds);
                Phase = LevelPhase.Reveal;
                yield return revealer.Reveal(config, cam, motor, path);
                cameras.Restore();
            }
            else
            {
                yield return new WaitForSeconds(config.introSeconds); // goal text only, already in first person
            }

            motor.InputEnabled = true;
            touch.enabled = true;
            TimeLeft = config.timeLimit;
            Phase = LevelPhase.Crossing;
        }

        private void Update()
        {
            if (Phase != LevelPhase.Crossing || Paused) return;

            Elapsed += Time.deltaTime;
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f)
            {
                TimeLeft = 0f;
                Lose("Time's up.");
                return;
            }

            if (motor.GroundPlatform != null) motor.GroundPlatform.SteppedOn(config.fakeSinkDelay);

            Vector3 d = motor.transform.position - door.position;
            if (new Vector2(d.x, d.z).magnitude <= DoorReach) Win();
        }

        private void OnSplash()
        {
            if (Phase != LevelPhase.Crossing) return;
            Splashes++;
            listener.HearSplash(motor.transform.position);
        }

        private void Win()
        {
            Phase = LevelPhase.Won;
            Stars = ProgressRules.Stars(Splashes, Elapsed, config.parTime);
            GameState.RecordWin(config, Stars, Elapsed); // saved before the Result screen shows (lab R5)
            StopPlayer();
        }

        private void Lose(string reason)
        {
            if (Phase != LevelPhase.Crossing) return;
            Phase = LevelPhase.Lost;
            LoseReason = reason;
            StopPlayer();
        }

        private void StopPlayer()
        {
            motor.InputEnabled = false;
            touch.enabled = false;
        }

        public void TogglePause()
        {
            if (Phase == LevelPhase.Won || Phase == LevelPhase.Lost) return;
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : 1f; // freezes everything, the timer and the Listener included
            AudioListener.pause = Paused;      // timeScale doesn't stop sound: the heartbeat kept beating
            touch.enabled = !Paused && Phase == LevelPhase.Crossing;
        }

        public void Retry() => SceneManager.LoadScene(SceneManager.GetActiveScene().name); // fresh scene = fakes reset

        public void NextLevel() => SceneManager.LoadScene(GameState.Catalog.Next(config).sceneName);

        public void ToMenu() => SceneManager.LoadScene(GameState.MenuScene);

        private void OnDestroy()
        {
            Time.timeScale = 1f; // leaving while paused must not freeze (or mute) the next scene
            AudioListener.pause = false;
        }
    }
}
