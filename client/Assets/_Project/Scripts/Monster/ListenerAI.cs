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
    // Steers round platforms and dives under them when it can't (PlatformAvoidance): it used to swim through like a ghost.
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
        private Animator animator; // the modelled body (Visual Pass); null on an old grey-box Listener
        private Transform chest, head;
        private Material pits; // runtime copy of ListenerGlow: the sound pits pulse with its state
        private Color glow;    // the look's glow colour: lights the vein map on the skin, pulsing with the pits
        private const float VeinGlow = 3f; // looks: the vein map is thin, so it needs more than the old flat skin glow
        private Color pitColor;
        // Looks: how the sound pits breathe (cycles per second, dimmest level) while roaming, lingering and hunting.
        private const float RoamPulse = 0.5f, RoamLow = 0.2f, LingerPulse = 1.2f, LingerLow = 0.35f, HuntPulse = 3f, HuntLow = 0.55f;

        /// <summary>The body's chest and head (null on a grey-box Listener): the catch camera frames them.</summary>
        public Transform Chest => chest;
        public Transform Head => head;
        private string clip;
        private Vector3 lastPos;
        private bool diving; // under a platform, or about to be
        private float steerAngle, steerRecheck, diveHold, playbackSpeed, surfaceFast;
        // Looks, not gameplay: how often it reconsiders its way round a platform, how fast it turns (far / near its
        // target), how long a dive lasts at least, how quickly the stroke speed follows its real speed.
        // Without these it zig-zagged, bobbed and slid sideways (2026-11-16 playtest: "glitchy").
        private const float SteerRecheck = 0.3f, TurnRate = 140f, CloseTurnRate = 400f, CloseRange = 4f, DiveHoldSeconds = PlatformAvoidance.DiveHold, PlaybackSmoothing = 4f;

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
            chest = FindBone(animator, HumanBodyBones.Chest, "Chest");
            head = FindBone(animator, HumanBodyBones.Head, "Head");
            var bodyRenderer = body.GetComponentInChildren<Renderer>();
            if (bodyRenderer != null)
            {
                var mats = bodyRenderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.StartsWith("ListenerGlow")) continue;
                    pits = mats[i] = new Material(mats[i]);
                    pitColor = pits.GetColor("_EmissionColor");
                }
                bodyRenderer.sharedMaterials = mats;
            }
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

        /// <summary>
        /// How close the danger feels, 0..1, for the music's hunt layer. Roaming 0. Lingering and Alerted are uneasy.
        /// Hunting starts at 0.6 and rises to 1 as it closes in (within 15 m, like the heartbeat).
        /// </summary>
        public float Threat
        {
            get
            {
                if (config == null || caught) return 0f;
                switch (State)
                {
                    case ListenerState.Alerted: return 0.45f;
                    case ListenerState.Lingering: return 0.3f;
                    case ListenerState.Hunting: return 0.6f + 0.4f * Mathf.Clamp01(1f - FlatDistance(player.transform.position) / 15f);
                    default: return 0f;
                }
            }
        }

        public float HuntSpeed =>config.listenerHuntSpeed * (1f + config.listenerSpeedPerSplash * Mathf.Max(0, splashes - 1));

        private void Update()
        {
            UpdatePits();
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
                        // Square up to you: the lunge, the open head and the catch camera all line up on you.
                        var toPlayer = player.transform.position - transform.position;
                        toPlayer.y = 0f;
                        if (toPlayer.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(toPlayer);
                        Scream();
                        Animate(0f);
                        Caught?.Invoke();
                    }
                    break;
            }

            bool back = ListenerRules.ShowsBack(State, config.listenerRoamMode, glimpseLeft > 0f);
            if (PlatformAvoidance.UnderPlatform(transform.position, water.SurfaceY))
                diveHold = DiveHoldSeconds; // once under, it stays under a moment: no bobbing at the edge of a platform
            else diveHold -= Time.deltaTime;
            diving = diveHold > 0f;
            if (diving) surfaceFast = 2f; else surfaceFast -= Time.deltaTime; // back up quickly after ducking under a platform
            float targetY = water.SurfaceY - (back && !diving ? BackDepth : DeepDepth);
            var pos = transform.position;
            float vertical = diving || surfaceFast > 0f ? PlatformAvoidance.DiveSpeed : RiseSpeed;
            transform.position = new Vector3(pos.x, Mathf.MoveTowards(pos.y, targetY, vertical * Time.deltaTime), pos.z);

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

        /// <summary>
        /// The body's chest: the Humanoid bone, or on a Generic rig (the custom Listener) the transform named "Chest".
        /// </summary>
        public static Transform FindChest(Animator animator) => FindBone(animator, HumanBodyBones.Chest, "Chest");

        /// <summary>A Humanoid bone, or on a Generic rig the transform with that name.</summary>
        public static Transform FindBone(Animator animator, HumanBodyBones bone, string genericName)
        {
            if (animator == null) return null;
            if (animator.isHuman) return animator.GetBoneTransform(bone);
            foreach (var t in animator.GetComponentsInChildren<Transform>(true))
                if (t.name == genericName) return t;
            return null;
        }

        // Its own scream on the catch, from its head (3D, loud, close): Resources/Music/sfx_listener_scream (compose.py).
        private void Scream()
        {
            var clip = Resources.Load<AudioClip>("Music/sfx_listener_scream");
            if (clip == null) return;
            var src = new GameObject("Scream").AddComponent<AudioSource>();
            src.transform.SetParent(head != null ? head : transform, false);
            src.clip = clip;
            src.spatialBlend = 0.7f; // mostly in your face, a little from where its head is
            src.minDistance = 4f;
            src.maxDistance = 30f;
            src.Play();
            Destroy(src.gameObject, clip.length + 0.5f);
        }

        // The sound pits breathe slowly while it roams, flicker when it hears you, throb fast while it hunts, and blaze
        // on the catch: in the dark, the string of lights is how you see it coming.
        private void UpdatePits()
        {
            if (pits == null) return;
            float k;
            if (caught) k = 1.8f;
            else if (State == ListenerState.Alerted) k = Mathf.PerlinNoise(Time.time * 14f, 0f) > 0.45f ? 1.4f : 0.1f;
            else
            {
                float hz = State == ListenerState.Hunting ? HuntPulse : State == ListenerState.Lingering ? LingerPulse : RoamPulse;
                float low = State == ListenerState.Hunting ? HuntLow : State == ListenerState.Lingering ? LingerLow : RoamLow;
                k = Mathf.Lerp(low, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * hz * 2f * Mathf.PI));
            }
            pits.SetColor("_EmissionColor", pitColor * k);
            if (skin != null) skin.SetColor("_EmissionColor", glow * VeinGlow * (0.45f + 0.55f * k));
        }

        // After the Animator posed it: slide the whole body so the chest sits at ChestLift, whatever the swim stroke does.
        // Not during the catch: the lunge rears the chest out of the water on purpose.
        private void LateUpdate()
        {
            if (animator == null || chest == null || caught) return;
            float off = transform.position.y + ChestLift - chest.position.y;
            body.transform.position += Vector3.up * off;
        }

        private void Animate(float speed)
        {
            if (animator == null) return;
            string next = ListenerLook.ClipFor(State, caught);
            if (next != clip) animator.CrossFadeInFixedTime(next, 0.25f);
            clip = next;
            playbackSpeed = Mathf.Lerp(playbackSpeed, speed, 1f - Mathf.Exp(-PlaybackSmoothing * Time.deltaTime));
            animator.speed = caught ? 1f : ListenerLook.Playback(playbackSpeed); // the lunge plays at its own pace: the catch camera shows it
        }

        private Vector3 PickRoamTarget()
        {
            Bounds area = water.Area;
            var target = Vector3.zero;
            for (int tries = 0; tries < 8; tries++) // open water, not a spot under a platform it could never reach
            {
                target = new Vector3(Random.Range(area.min.x + RoamMargin, area.max.x - RoamMargin), 0f,
                                     Random.Range(area.min.z + RoamMargin, area.max.z - RoamMargin));
                if (!PlatformAvoidance.Overlapping(target, water.SurfaceY)) break;
            }
            return target;
        }

        private float NextGlimpse() => config.listenerGlimpseEvery * Random.Range(0.75f, 1.25f);

        // Moves only across the water plane, round the platforms; depth is handled separately (rising/sinking).
        private void SwimToward(Vector3 target, float speed)
        {
            var pos = transform.position;
            var toTarget = new Vector3(target.x - pos.x, 0f, target.z - pos.z);
            float distance = toTarget.magnitude;
            if (distance < 1e-4f) return;
            bool close = distance < CloseRange;
            if (close) steerAngle = 0f; // the last metres go straight at the sound, even right beside a platform (you fell off one)
            else if ((steerRecheck -= Time.deltaTime) <= 0f)
            {
                steerRecheck = SteerRecheck;
                steerAngle = PlatformAvoidance.SteerAngle(pos, water.SurfaceY, toTarget, steerAngle, out _);
            }
            var desired = Quaternion.Euler(0f, steerAngle, 0f) * (toTarget / distance);
            float turn = close || State == ListenerState.Hunting ? CloseTurnRate : TurnRate;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(desired), turn * Time.deltaTime);
            float step = speed * Time.deltaTime;
            if (distance < 1f)
            {
                transform.position = Vector3.MoveTowards(pos, new Vector3(target.x, pos.y, target.z), step); // land exactly on the spot
                return;
            }
            // It swims where it faces (an arc, never a sideways slide), a bit slower while it turns, but never stalls.
            float along = Mathf.Clamp01(0.35f + 0.65f * Vector3.Dot(transform.forward, desired));
            transform.position = pos + transform.forward * Mathf.Min(step * along, distance);
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
                foreach (var r in body.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials; // slot 0 only: the mouth, sound pits, throat and hair keep their own
                    mats[0] = skin;
                    r.sharedMaterials = mats;
                }
            }
            skin.EnableKeyword("_EMISSION");
            glow = color;
            skin.SetColor("_EmissionColor", color * VeinGlow); // through the vein map: the veins glow, not the whole skin
        }

        private void OnDestroy()
        {
            if (skin != null) Destroy(skin);
            if (pits != null) Destroy(pits);
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
