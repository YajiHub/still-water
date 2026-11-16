using StillWater.Level;
using StillWater.Monster;
using UnityEngine;

namespace StillWater.UI
{
    /// <summary>
    /// The Listener behind the main menu: it circles slowly under the surface of the menu's pool, its back and the
    /// glowing sound pits breaking the water, leaving ripples. Every so often it sinks out of sight and comes back up
    /// somewhere else. Looks only, no AI. Built into the Menu scene by Still Water → Art Spike → Build Menu Backdrop.
    /// </summary>
    public class MenuListener : MonoBehaviour
    {
        [SerializeField] private Vector3 centre = new Vector3(0f, 0f, 14f);
        [SerializeField] private Vector2 radii = new Vector2(7f, 4.5f);
        [Tooltip("seconds per lap")]
        [SerializeField] private float lapSeconds = 46f;
        [Tooltip("the look's glow colour, lighting its veins (Look C's listenerGlow)")]
        [SerializeField] private Color veinGlow = new Color(0.30f, 0.36f, 0.38f);

        // Looks: chest depth at the surface and when hidden, sink/rise speed, how often it dives, ripple rhythm.
        private const float BackDepth = 0.25f, HiddenDepth = 2.5f, DepthSpeed = 0.8f, DiveEvery = 17f, DiveSeconds = 5f;
        private const float RippleEvery = 0.45f, VeinBoost = 3f, PulseHz = 0.5f;

        private PoolWater water;
        private Animator animator;
        private Transform body, chest;
        private Material skin, pits;
        private Color pitColor;
        private float depth = BackDepth, rippleTimer, phase, underHold;

        private void Start()
        {
            water = FindFirstObjectByType<PoolWater>();
            animator = GetComponentInChildren<Animator>();
            body = animator != null ? animator.transform : transform;
            chest = ListenerAI.FindChest(animator);
            if (animator != null) animator.Play(ListenerLook.Swim);
            var r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (i == 0) { skin = mats[i] = new Material(mats[i]); skin.EnableKeyword("_EMISSION"); }
                    else if (mats[i].name.StartsWith("ListenerGlow")) { pits = mats[i] = new Material(mats[i]); pitColor = pits.GetColor("_EmissionColor"); }
                }
                r.sharedMaterials = mats;
            }
            phase = Random.value;
        }

        private void Update()
        {
            float surface = water != null ? water.SurfaceY : 0f;
            phase += Time.deltaTime / lapSeconds;
            float a = phase * 2f * Mathf.PI;
            var pos = centre + new Vector3(Mathf.Cos(a) * radii.x, 0f, Mathf.Sin(2f * a) * radii.y); // a slow figure eight
            var ahead = centre + new Vector3(Mathf.Cos(a + 0.05f) * radii.x, 0f, Mathf.Sin(2f * (a + 0.05f)) * radii.y);
            if (PlatformAvoidance.UnderPlatform(new Vector3(pos.x, 0f, pos.z), surface)) underHold = PlatformAvoidance.DiveHold;
            else underHold -= Time.deltaTime; // stays under a moment: no bobbing at a platform's edge
            bool under = underHold > 0f;
            bool diving = under || Mathf.Repeat(Time.time, DiveEvery) > DiveEvery - DiveSeconds;
            depth = Mathf.MoveTowards(depth, diving ? HiddenDepth : BackDepth, (under ? PlatformAvoidance.DiveSpeed : DepthSpeed) * Time.deltaTime);
            transform.position = new Vector3(pos.x, surface - depth, pos.z);
            var dir = ahead - pos;
            if (dir.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(dir);
            if (animator != null) animator.speed = 0.75f;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * PulseHz * 2f * Mathf.PI);
            if (pits != null) pits.SetColor("_EmissionColor", pitColor * Mathf.Lerp(0.25f, 1f, pulse));
            if (skin != null) skin.SetColor("_EmissionColor", veinGlow * VeinBoost * Mathf.Lerp(0.4f, 1f, pulse));

            if (water != null && water.Fx != null && depth < 1f && (rippleTimer -= Time.deltaTime) <= 0f)
            {
                rippleTimer = RippleEvery;
                water.Fx.Ripple(new Vector3(pos.x, surface, pos.z), 2.4f, 2f);
            }
        }

        // After the Animator: keep the chest at the chosen depth whatever the stroke does (as ListenerAI does).
        private void LateUpdate()
        {
            if (chest == null) return;
            float off = transform.position.y + 0.0f - chest.position.y;
            body.position += Vector3.up * off;
        }

        private void OnDestroy()
        {
            if (skin != null) Destroy(skin);
            if (pits != null) Destroy(pits);
        }
    }
}
