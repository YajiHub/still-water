using StillWater.Player;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// A crumbling or one-way tile (Level 4). Each frame: is the player standing on me, is a swimmer above me →
    /// <see cref="SinkRules"/> → move down/up, shake, switch the collider and climbing. Added by Build Level.
    /// </summary>
    [RequireComponent(typeof(Platform))]
    public class SinkingPlatform : MonoBehaviour
    {
        [Tooltip("On = one-way (sinks after you leave, stays down). Off = crumbling (sinks if you stand too long, rises again).")]
        [SerializeField] private bool oneway;

        private const float Depth = 2.5f, ShakeAmount = 0.05f; // looks, not gameplay: how far it goes and how it shakes

        private Platform platform;
        private Collider solid;
        private PlayerMotor motor;
        private LevelConfig config;
        private Vector3 home;
        private SinkState state = SinkState.Up;
        private float timer;

        private void Start()
        {
            platform = GetComponent<Platform>();
            solid = GetComponent<Collider>();
            home = transform.position;
            motor = FindFirstObjectByType<PlayerMotor>();
            var level = FindFirstObjectByType<LevelController>();
            if (level != null) config = level.Config;
        }

        private void Update()
        {
            if (config == null || motor == null) return;
            var t = new SinkTimings
            {
                stand = config.crumbleStandSeconds,
                shake = config.crumbleShakeSeconds,
                sink = config.sinkSeconds,
                riseAfter = config.crumbleRiseAfter,
                onewayDelay = config.onewayDelay,
            };
            bool standing = motor.GroundPlatform == platform;
            (state, timer) = oneway
                ? SinkRules.StepOneway(state, timer, standing, Time.deltaTime, t)
                : SinkRules.StepCrumble(state, timer, standing, SwimmerAbove(), Time.deltaTime, t);

            var pos = home + Vector3.down * (Depth * SinkRules.Depth01(state, timer, t));
            if (state == SinkState.Shaking) pos += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * ShakeAmount;
            transform.position = pos;
            solid.enabled = state != SinkState.Down;
            platform.SetClimbable(SinkRules.Climbable(state));
        }

        // A swimmer over the tile's spot (plus a little room): a rising tile would pop them up, so it waits.
        private bool SwimmerAbove()
        {
            if (motor.State != PlayerMotor.MotorState.Swimming) return false;
            Vector3 d = motor.transform.position - home;
            float reach = transform.localScale.x / 2f + 0.5f;
            return Mathf.Abs(d.x) < reach && Mathf.Abs(d.z) < reach;
        }
    }
}
