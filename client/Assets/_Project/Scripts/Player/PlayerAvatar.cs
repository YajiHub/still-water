using StillWater.Level;
using UnityEngine;
using UnityEngine.Rendering;

namespace StillWater.Player
{
    /// <summary>
    /// The player's visible body. It replaces the grey-box capsule at runtime with the body chosen in the menu
    /// (Resources/Player/PlayerBody_*.prefab, built by Still Water → Art Spike → Build Player Bodies) and animates it
    /// from <see cref="PlayerMotor"/>: idle, jog or sprint, in the air, swimming at the surface, climbing out. In first
    /// person only its shadow is drawn, so the camera isn't inside your head. PlayerMotor adds this component itself, so
    /// every level gets it without a scene edit.
    /// </summary>
    public class PlayerAvatar : MonoBehaviour
    {
        public static readonly string[] Bodies = { "Female", "Male" };

        // Looks: while swimming, the chest floats this far under the surface; how fast the body settles there.
        private const float ChestUnderWater = 0.08f, LiftSharpness = 8f, Crossfade = 0.2f;

        /// <summary>Index into <see cref="Bodies"/>: the Settings page's Character (save.json).</summary>
        public static int Choice
        {
            get => Mathf.Clamp(Core.GameSettings.Current.character, 0, Bodies.Length - 1);
            set
            {
                Core.GameSettings.Current.character = Mathf.Clamp(value, 0, Bodies.Length - 1);
                Core.GameSettings.Changed();
            }
        }

        /// <summary>Spawns a body under <paramref name="parent"/> (null if the prefab hasn't been built yet).</summary>
        public static GameObject Spawn(int body, Transform parent)
        {
            var prefab = Resources.Load<GameObject>($"Player/PlayerBody_{Bodies[Mathf.Clamp(body, 0, Bodies.Length - 1)]}");
            return prefab != null ? Instantiate(prefab, parent, false) : null;
        }

        private PlayerMotor motor;
        private CameraModeSwitch cameras;
        private PoolWater water;
        private Animator animator;
        private Transform model, chest;
        private Renderer[] renderers;
        private string clip;
        private bool shadowOnly;
        private Vector3 lastPosition;

        private void Start()
        {
            motor = GetComponent<PlayerMotor>();
            cameras = FindFirstObjectByType<CameraModeSwitch>();
            water = FindFirstObjectByType<PoolWater>();
            var body = Spawn(Choice, transform);
            if (body == null) return; // prefab not built: keep the capsule
            foreach (string part in new[] { "Body", "Nose" })
            {
                var old = transform.Find(part);
                if (old != null) old.gameObject.SetActive(false);
            }
            model = body.transform;
            animator = body.GetComponent<Animator>();
            chest = animator != null ? animator.GetBoneTransform(HumanBodyBones.Chest) : null;
            renderers = body.GetComponentsInChildren<Renderer>(true);
            lastPosition = transform.position;
            shadowOnly = !ShouldShadowOnly(); // force the first SetShadowOnly
        }

        private void Update()
        {
            if (animator == null || motor == null) return;
            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            float speed = new Vector2(delta.x, delta.z).magnitude / Mathf.Max(Time.deltaTime, 1e-4f);

            string next = PlayerLook.ClipFor(motor.State, speed);
            if (next != clip) animator.CrossFadeInFixedTime(next, Crossfade);
            clip = next;
            animator.speed = PlayerLook.Playback(next, speed);

            bool wantShadowOnly = ShouldShadowOnly();
            if (wantShadowOnly != shadowOnly)
            {
                shadowOnly = wantShadowOnly;
                foreach (var r in renderers)
                    r.shadowCastingMode = shadowOnly ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            }
        }

        private bool ShouldShadowOnly() => cameras != null && cameras.IsFirstPerson && cameras.FirstPersonActive;

        // After the Animator: while swimming, lift the body so the chest is just under the surface (the swim clips lie
        // flat at hip height); otherwise settle back onto the feet.
        private void LateUpdate()
        {
            if (model == null) return;
            if (motor.State == PlayerMotor.MotorState.Swimming && chest != null && water != null)
            {
                float off = water.SurfaceY - ChestUnderWater - chest.position.y;
                model.position += Vector3.up * off * Mathf.Min(1f, LiftSharpness * Time.deltaTime);
            }
            else
            {
                model.localPosition = Vector3.Lerp(model.localPosition, Vector3.zero, Mathf.Min(1f, LiftSharpness * Time.deltaTime));
            }
        }
    }
}
