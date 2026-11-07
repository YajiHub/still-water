using StillWater.Core;
using StillWater.Level;
using StillWater.Player;
using UnityEngine;

namespace StillWater.Monster
{
    /// <summary>
    /// The Listener (DESIGN §5.2): always in the pool, hunts by sound, never teleports.
    /// Roams slowly → a splash stops it (Alerted) → swims to the sound, tracking you while you swim (Hunting)
    /// → lingers where it last heard you → roams on from there. Decisions live in <see cref="ListenerRules"/>.
    /// Offline only; online the server will drive it (Phase 4).
    /// </summary>
    // shortcut: swims in a straight line through platform pillars; add steering if playtests notice it.
    public class ListenerAI : MonoBehaviour
    {
        [Tooltip("The visible body (a child)")]
        [SerializeField] private GameObject body;

        // Looks, not gameplay: how deep it swims, and how fast it rises or sinks (the water is opaque).
        private const float BackDepth = 0.35f, DeepDepth = 2.2f, RiseSpeed = 1.2f, ReachedDistance = 0.3f, RoamMargin = 1.5f;
        private const float WakeEvery = 0.35f, WakeLife = 1.6f, WakeSize = 1.4f;
        // Humanoid body: keep its chest bone this far above the Listener's origin, so "back at the surface" puts the
        // chest on the water line and the back and shoulder blades out of it (not just the raised head).
        private const float ChestLift = BackDepth;

        public ListenerState State { get; private set; } = ListenerState.Roaming;
        /// <summary>Raised once when it reaches a swimming player.</summary>
        public event System.Action Caught;

        private LevelConfig config;
        private PlayerMotor player;
        private PoolWater water;
        private int splashes;
        private bool caught;
        private float timer, glimpseTimer, glimpseLeft, roamPauseLeft, wakeTimer;
        private Vector3 heardAt, roamTarget;
        private AudioSource heartbeat;
        private AudioClip splashClip;
        private Material skin; // runtime copy: glow changes never touch the material asset
        private Animator animator; // the Quaternius body (Visual Pass); null on an old grey-box Listener
        private Transform chest;
        private string clip;
        private Vector3 lastPos;

        public void Init(LevelConfig levelConfig, PlayerMotor motor, PoolWater pool)
        {
            config = levelConfig;
            player = motor;
            water = pool;
            splashClip = Resources.Load<AudioClip>("Audio/Splash"); // real recording (CREDITS.md)
            if (splashClip == null) splashClip = ProceduralAudio.Splash();
            heartbeat = gameObject.AddComponent<AudioSource>();
            heartbeat.clip = ProceduralAudio.Heartbeat();
            heartbeat.loop = true;
            heartbeat.spatialBlend = 0f; // your own heartbeat

            // Its own 3D drone: in Hidden mode the only way to tell where it is.
            var rumble = gameObject.AddComponent<AudioSource>();
            rumble.clip = ProceduralAudio.Rumble();
            rumble.loop = true;
            rumble.spatialBlend = 1f;
            rumble.rolloffMode = AudioRolloffMode.Linear;
            rumble.minDistance = 2f;
            rumble.maxDistance = 14f;
            rumble.Play();

            // Starts in the middle of the pool, already swimming: from a far corner the first fall was almost always safe.
            Vector3 middle = water.Area.center;
            float x = middle.x, z = middle.z;
            bool back = ListenerRules.ShowsBack(State, config.listenerRoamMode, false);
            transform.position = new Vector3(x, water.SurfaceY - (back ? BackDepth : DeepDepth), z);
            body.SetActive(true);
            animator = body.GetComponentInChildren<Animator>();
            if (animator != null) chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            lastPos = transform.position;
            roamTarget = PickRoamTarget();
            glimpseTimer = NextGlimpse();
        }

        /// <summary>The player hit the water at <paramref name="where"/>.</summary>
        public void HearSplash(Vector3 where)
        {
            splashes++;
            AudioSource.PlayClipAtPoint(splashClip, where); // 3D: you hear where it came from
            heardAt = where;
            var next = ListenerRules.OnHeard(State);
            if (next == ListenerState.Alerted && State != ListenerState.Alerted) timer = config.listenerAlertDelay;
            State = next;
        }

        public float HuntSpeed => config.listenerHuntSpeed * (1f + config.listenerSpeedPerSplash * Mathf.Max(0, splashes - 1));

        private void Update()
        {
            if (config == null || caught) return;
            bool swimming = player.State == PlayerMotor.MotorState.Swimming; // Climbing counts as out: safe once CLIMB starts
            if (swimming) heardAt = player.transform.position; // swimming is noisy

            timer -= Time.deltaTime;
            var before = State;
            State = ListenerRules.Tick(State, swimming, FlatDistance(heardAt) <= ReachedDistance, timer <= 0f);
            if (State != before)
            {
                if (State == ListenerState.Alerted) timer = config.listenerAlertDelay;
                if (State == ListenerState.Lingering) timer = config.listenerLingerSeconds;
                if (State == ListenerState.Roaming) { roamTarget = PickRoamTarget(); glimpseTimer = NextGlimpse(); }
            }

            switch (State)
            {
                case ListenerState.Roaming:
                    Roam();
                    break;
                case ListenerState.Alerted:
                    Face(heardAt); // stops and turns to the sound
                    break;
                case ListenerState.Hunting:
                    SwimToward(heardAt, HuntSpeed);
                    if (swimming && FlatDistance(player.transform.position) <= config.listenerCatchDistance)
                    {
                        caught = true;
                        Animate(0f);
                        Caught?.Invoke();
                    }
                    break;
            }

            bool back = ListenerRules.ShowsBack(State, config.listenerRoamMode, glimpseLeft > 0f);
            float targetY = water.SurfaceY - (back ? BackDepth : DeepDepth);
            var pos = transform.position;
            transform.position = new Vector3(pos.x, Mathf.MoveTowards(pos.y, targetY, RiseSpeed * Time.deltaTime), pos.z);

            Animate(FlatDistance(lastPos) / Mathf.Max(Time.deltaTime, 1e-4f));
            lastPos = transform.position;

            UpdateWake(State == ListenerState.Roaming && config.listenerRoamMode == ListenerRoamMode.Wake && !back);
            UpdateHeartbeat();
        }

        private void Roam()
        {
            if (config.listenerRoamMode != ListenerRoamMode.Fin && config.listenerGlimpseEvery > 0f)
            {
                if (glimpseLeft > 0f) glimpseLeft -= Time.deltaTime;
                else if ((glimpseTimer -= Time.deltaTime) <= 0f)
                {
                    glimpseLeft = config.listenerGlimpseSeconds;
                    glimpseTimer = NextGlimpse();
                }
            }

            if (roamPauseLeft > 0f) { roamPauseLeft -= Time.deltaTime; return; }
            SwimToward(roamTarget, config.listenerRoamSpeed);
            if (FlatDistance(roamTarget) <= ReachedDistance)
            {
                roamPauseLeft = config.listenerRoamPause;
                roamTarget = PickRoamTarget();
            }
        }

        // After the Animator posed it: slide the whole body so the chest sits at ChestLift, whatever the swim stroke does.
        private void LateUpdate()
        {
            if (animator == null || chest == null) return;
            float off = transform.position.y + ChestLift - chest.position.y;
            body.transform.position += Vector3.up * off;
        }

        private void Animate(float speed)
        {
            if (animator == null) return;
            string next = ListenerLook.ClipFor(State, caught);
            if (next != clip) animator.CrossFadeInFixedTime(next, 0.25f);
            clip = next;
            animator.speed = caught ? 1.5f : ListenerLook.Playback(speed); // the lunge is quick
        }

        private Vector3 PickRoamTarget()
        {
            Bounds area = water.Area;
            return new Vector3(Random.Range(area.min.x + RoamMargin, area.max.x - RoamMargin), 0f,
                               Random.Range(area.min.z + RoamMargin, area.max.z - RoamMargin));
        }

        private float NextGlimpse() => config.listenerGlimpseEvery * Random.Range(0.75f, 1.25f);

        // Moves only across the water plane; depth is handled separately (rising/sinking at RiseSpeed).
        private void SwimToward(Vector3 target, float speed)
        {
            var pos = transform.position;
            var flat = new Vector3(target.x, pos.y, target.z);
            Face(target);
            transform.position = Vector3.MoveTowards(pos, flat, speed * Time.deltaTime);
        }

        private void Face(Vector3 target)
        {
            var d = new Vector3(target.x - transform.position.x, 0f, target.z - transform.position.z);
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 270f * Time.deltaTime);
        }

        /// <summary>Faint glow of its skin and wake (from the current look), so it reads in the dark.</summary>
        public void SetGlow(Color color)
        {
            if (skin == null)
            {
                var bodyRenderer = body.GetComponentInChildren<Renderer>();
                if (bodyRenderer == null) return;
                skin = new Material(bodyRenderer.sharedMaterial);
                foreach (var r in body.GetComponentsInChildren<Renderer>()) r.sharedMaterial = skin;
            }
            skin.EnableKeyword("_EMISSION");
            skin.SetColor("_EmissionColor", color);
        }

        private void OnDestroy()
        {
            if (skin != null) Destroy(skin);
        }

        // Wake mode: ripple rings trailing it across the surface (WaterFx).
        private void UpdateWake(bool emit)
        {
            if (!emit || water.Fx == null || (wakeTimer -= Time.deltaTime) > 0f) return;
            wakeTimer = WakeEvery;
            water.Fx.Ripple(transform.position, WakeSize * 1.6f, WakeLife);
        }

        private void UpdateHeartbeat()
        {
            if (heartbeat == null) return;
            bool near = State != ListenerState.Roaming;
            if (near && !heartbeat.isPlaying) heartbeat.Play();
            if (!near && heartbeat.isPlaying) heartbeat.Stop();
            // Faster and louder as it closes in.
            float closeness = near ? Mathf.Clamp01(1f - FlatDistance(player.transform.position) / 15f) : 0f;
            heartbeat.pitch = 0.8f + closeness * 0.9f;
            heartbeat.volume = 0.3f + closeness * 0.7f;
        }

        private float FlatDistance(Vector3 p) => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(transform.position.x, transform.position.z));
    }
}
