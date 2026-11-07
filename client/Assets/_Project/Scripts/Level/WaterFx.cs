using StillWater.Player;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// Water and Hall Look: a splash when the player falls in, ripple rings (swimming player, the Listener's wake),
    /// and still foam rings around platform bases. Three particle systems built in code, all driven by Emit, so it is
    /// a few draw calls for the whole pool. Materials come from Resources/WaterFx (Still Water → Art Spike → Set Up Water).
    /// Added by <see cref="PoolWater"/>, so no level needs rebuilding.
    /// </summary>
    public class WaterFx : MonoBehaviour
    {
        // Looks, not gameplay.
        private const float SwimRippleEvery = 0.3f, SwimRippleSize = 1.6f, SplashRippleSize = 3.5f, FoamPadding = 1.35f;

        private ParticleSystem drops, ripples, foam;
        private PlayerMotor player;
        private float swimTimer;

        private void Awake()
        {
            var ring = Resources.Load<Material>("WaterFx/Ripple");
            var dot = Resources.Load<Material>("WaterFx/Drop");
            if (ring == null || dot == null)
            {
                Debug.LogWarning("WaterFx: run Still Water → Art Spike → Set Up Water (Resources/WaterFx missing). No splashes.");
                enabled = false;
                return;
            }
            drops = Make("Splash drops", dot, ParticleSystemRenderMode.Billboard, 300, gravity: 1f);
            ripples = Make("Ripples", ring, ParticleSystemRenderMode.HorizontalBillboard, 200, gravity: 0f);
            var grow = ripples.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.25f, 1f, 1f));
            var fade = ripples.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            foam = Make("Foam", ring, ParticleSystemRenderMode.HorizontalBillboard, 1000, gravity: 0f);
        }

        private void Start()
        {
            if (!enabled) return;
            player = FindFirstObjectByType<PlayerMotor>();
            if (player != null) player.Splashed += () => Splash(player.transform.position);

            // Still foam where each platform meets the water (not on ferries: they move away from it).
            float y = SurfaceY + 0.02f;
            foreach (var p in FindObjectsByType<Platform>(FindObjectsSortMode.None))
            {
                if (p.GetComponent<PlatformMover>() != null) continue;
                Vector3 at = p.transform.position;
                Emit(foam, new Vector3(at.x, y, at.z), p.transform.lossyScale.x * FoamPadding, 1e6f, new Color(0.8f, 0.9f, 0.9f, 0.35f));
            }
        }

        private void Update()
        {
            if (player == null || player.State != PlayerMotor.MotorState.Swimming) return;
            if ((swimTimer -= Time.deltaTime) > 0f) return;
            swimTimer = SwimRippleEvery;
            Ripple(player.transform.position, SwimRippleSize);
        }

        private float SurfaceY => transform.position.y;

        /// <summary>A growing, fading ring on the surface at <paramref name="where"/> (x/z; y is the water's).</summary>
        public void Ripple(Vector3 where, float size, float life = 1.6f)
        {
            if (!enabled) return;
            Emit(ripples, new Vector3(where.x, SurfaceY + 0.03f, where.z), size, life, Color.white);
        }

        /// <summary>Falling in: a burst of drops plus two rings.</summary>
        public void Splash(Vector3 where)
        {
            if (!enabled) return;
            var at = new Vector3(where.x, SurfaceY + 0.05f, where.z);
            for (int i = 0; i < 40; i++)
            {
                Vector2 side = Random.insideUnitCircle * 1.6f;
                var p = new ParticleSystem.EmitParams
                {
                    position = at,
                    velocity = new Vector3(side.x, Random.Range(2.5f, 5f), side.y),
                    startSize = Random.Range(0.06f, 0.16f),
                    startLifetime = Random.Range(0.6f, 1.1f),
                    startColor = new Color(0.85f, 0.95f, 0.95f, 0.85f),
                };
                drops.Emit(p, 1);
            }
            Ripple(at, SplashRippleSize, 2.2f);
            Ripple(at, SplashRippleSize * 0.6f, 1.4f);
        }

        private static void Emit(ParticleSystem ps, Vector3 at, float size, float life, Color color) =>
            ps.Emit(new ParticleSystem.EmitParams { position = at, velocity = Vector3.zero, startSize = size, startLifetime = life, startColor = color }, 1);

        // One world-space system that only emits when told to.
        private ParticleSystem Make(string name, Material material, ParticleSystemRenderMode mode, int max, float gravity)
        {
            var go = new GameObject(name); // not under the water box: its 70 m scale would leak into the particles
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true; // keeps simulating forever (emission is off: only Emit adds particles)
            main.maxParticles = max;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.scalingMode = ParticleSystemScalingMode.Shape; // the water box's scale must not stretch the particles
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = mode;
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }
    }
}
