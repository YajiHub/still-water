using StillWater.Core;
using StillWater.Level;
using StillWater.Player;
using UnityEngine;

namespace StillWater.Monster
{
    /// <summary>
    /// The Listener (DESIGN §5.2): hunts by sound. Dormant → (splash) Alerted → Hunting → Catch, or Returning
    /// if the player climbs out first. Offline only; online the server will drive it (Phase 4).
    /// </summary>
    // shortcut: swims in a straight line through platform pillars; add steering if playtests notice it.
    public class ListenerAI : MonoBehaviour
    {
        public enum ListenerState { Dormant, Alerted, Hunting, Returning }

        [Tooltip("The visible body (a child), hidden while Dormant")]
        [SerializeField] private GameObject body;

        private const float SwimDepth = 0.35f, HiddenDepth = 4f;

        public ListenerState State { get; private set; } = ListenerState.Dormant;
        /// <summary>Raised once when it reaches a swimming player.</summary>
        public event System.Action Caught;

        private LevelConfig config;
        private PlayerMotor player;
        private PoolWater water;
        private int splashes;
        private bool hasSurfaced; // after the first hunt it stays where it sank, so it rises close by next time
        private float timer;
        private AudioSource heartbeat;
        private AudioClip splashClip;

        public void Init(LevelConfig levelConfig, PlayerMotor motor, PoolWater pool)
        {
            config = levelConfig;
            player = motor;
            water = pool;
            splashClip = ProceduralAudio.Splash();
            heartbeat = gameObject.AddComponent<AudioSource>();
            heartbeat.clip = ProceduralAudio.Heartbeat();
            heartbeat.loop = true;
            heartbeat.spatialBlend = 0f; // your own heartbeat
            Hide();
        }

        /// <summary>The player hit the water at <paramref name="where"/>.</summary>
        public void HearSplash(Vector3 where)
        {
            splashes++;
            AudioSource.PlayClipAtPoint(splashClip, where); // 3D: you hear where it came from
            // Already coming, or still lingering nearby: keep chasing from where it is (it just got faster).
            if (State == ListenerState.Hunting || State == ListenerState.Returning)
            {
                State = ListenerState.Hunting;
                return;
            }
            State = ListenerState.Alerted;
            timer = config.listenerAlertDelay;
        }

        public float HuntSpeed => config.listenerHuntSpeed * (1f + config.listenerSpeedPerSplash * Mathf.Max(0, splashes - 1));

        private void Update()
        {
            if (config == null) return;
            bool playerInWater = player.State == PlayerMotor.MotorState.Swimming; // Climbing counts as out: safe once CLIMB starts

            switch (State)
            {
                case ListenerState.Alerted:
                    timer -= Time.deltaTime;
                    if (timer <= 0f) Surface();
                    break;

                case ListenerState.Hunting:
                    if (!playerInWater)
                    {
                        State = ListenerState.Returning;
                        timer = config.listenerLingerSeconds;
                        break;
                    }
                    SwimToward(player.transform.position);
                    if (FlatDistance(player.transform.position) <= config.listenerCatchDistance)
                    {
                        State = ListenerState.Dormant;
                        Caught?.Invoke();
                    }
                    break;

                case ListenerState.Returning:
                    if (playerInWater) { State = ListenerState.Hunting; break; } // fell back in while it lingered
                    timer -= Time.deltaTime;
                    if (timer <= 0f) Hide();
                    break;
            }

            if (heartbeat != null)
            {
                bool near = State == ListenerState.Hunting || State == ListenerState.Returning;
                if (near && !heartbeat.isPlaying) heartbeat.Play();
                if (!near && heartbeat.isPlaying) heartbeat.Stop();
                // Faster and louder as it closes in.
                float closeness = near ? Mathf.Clamp01(1f - FlatDistance(player.transform.position) / 15f) : 0f;
                heartbeat.pitch = 0.8f + closeness * 0.9f;
                heartbeat.volume = 0.3f + closeness * 0.7f;
            }
        }

        // The first time: rises at the corner of the pool farthest from the player. After that: where it last sank.
        private void Surface()
        {
            float x = transform.position.x, z = transform.position.z;
            if (!hasSurfaced)
            {
                Bounds area = water.Area;
                Vector3 p = player.transform.position;
                x = p.x < area.center.x ? area.max.x - 1f : area.min.x + 1f;
                z = p.z < area.center.z ? area.max.z - 1f : area.min.z + 1f;
                hasSurfaced = true;
            }
            transform.position = new Vector3(x, water.SurfaceY - SwimDepth, z);
            body.SetActive(true);
            State = ListenerState.Hunting;
        }

        private void SwimToward(Vector3 target)
        {
            Vector3 flat = new Vector3(target.x, water.SurfaceY - SwimDepth, target.z);
            Vector3 next = Vector3.MoveTowards(transform.position, flat, HuntSpeed * Time.deltaTime);
            if ((flat - transform.position).sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(flat - transform.position);
            transform.position = next;
        }

        private void Hide()
        {
            State = ListenerState.Dormant;
            transform.position = new Vector3(transform.position.x, water.SurfaceY - HiddenDepth, transform.position.z);
            body.SetActive(false);
        }

        private float FlatDistance(Vector3 p) => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(transform.position.x, transform.position.z));
    }
}
