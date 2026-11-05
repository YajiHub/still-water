using UnityEngine;

namespace StillWater.Level
{
    /// <summary>Everything that makes one level different (DESIGN §5.3, §6). All Draft: tune in playtests.</summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Still Water/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Save id, e.g. level01 (never rename after release: saves use it)")] public string id = "level01";
        public string displayName = "Shallow End";
        [Tooltip("Scene to load for this level")] public string sceneName = "Level01";
        [TextArea] public string goalText = "Remember the glowing path. Reach the door.";
        [Tooltip("The layout JSON copied into the client by Build Level")] public TextAsset layout;

        [Header("Rules")]
        [Tooltip("Off-path platforms sink (off for Level 1: it introduces the system)")] public bool fakePlatforms;
        [Tooltip("seconds before a fake sinks after you land")] public float fakeSinkDelay = 0.4f;
        [Tooltip("seconds; 0 = lose")] public float timeLimit = 120f;
        [Tooltip("seconds for the ★★★ time")] public float parTime = 60f;

        [Header("Reveal")]
        [Tooltip("Show the glowing path before the crossing (off for Level 1: it only teaches moving and the water)")] public bool revealPath = true;
        [Tooltip("seconds the goal text shows before the glow starts")] public float introSeconds = 2f;
        [Tooltip("seconds between path tiles lighting up")] public float glowSecondsPerTile = 0.8f;
        [Tooltip("metres above the start for the overhead view")] public float revealHeight = 8f;
        [Tooltip("seconds the camera takes to glide back into the player's eyes")] public float revealGlideSeconds = 1.5f;
        [Tooltip("fog thickness during the Reveal, as a share of the normal fog (lower = clearer view)")] [Range(0f, 1f)] public float revealFogMultiplier = 0.35f;

        [Header("Listener")]
        [Tooltip("seconds from a splash until it starts swimming")] public float listenerAlertDelay = 1.5f;
        [Tooltip("m/s (you swim at 2.0)")] public float listenerHuntSpeed = 3.5f;
        [Tooltip("extra speed per splash after the first (0.1 = +10%)")] public float listenerSpeedPerSplash = 0.1f;
        [Tooltip("seconds it lingers after you climb out, before sinking back")] public float listenerLingerSeconds = 2f;
        [Tooltip("metres: this close to a swimming player = caught")] public float listenerCatchDistance = 0.9f;
    }
}
