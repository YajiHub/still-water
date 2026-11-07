using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// The pool's water surface. Deep: below it the player swims, never stands. Also moves the water: two wave
    /// normal layers drifting different ways (Water and Hall Look; textures set by Still Water → Art Spike → Set Up Water).
    /// </summary>
    public class PoolWater : MonoBehaviour
    {
        // Looks, not gameplay: metres per wave-texture repeat, and drift speeds in texture repeats per second.
        private const float BigWaveMetres = 7f, SmallWaveMetres = 2.5f;
        private static readonly Vector2 BigDrift = new Vector2(0.012f, 0.02f), SmallDrift = new Vector2(-0.03f, 0.018f);

        public float SurfaceY => transform.position.y;
        /// <summary>The water's footprint (the Listener spawns at its far corners).</summary>
        public Bounds Area => GetComponent<Renderer>().bounds;

        private Material water; // runtime copy: scrolling never touches the Water.mat asset

        /// <summary>Splash, ripples and foam (null-safe callers: it disables itself if its materials are missing).</summary>
        public WaterFx Fx { get; private set; }

        // Every level has exactly one PoolWater, so the Visual Pass hooks in here instead of needing a level rebuild.
        private void Awake()
        {
            Fx = gameObject.AddComponent<WaterFx>(); // before any Start, so ListenerAI can use it
            gameObject.AddComponent<HallDressing>();
        }

        private void Start()
        {
            var r = GetComponent<Renderer>();
            water = r.material;
            Vector3 size = r.bounds.size; // the box's UVs stretch 0..1 over the whole pool: tile by its real size
            water.SetTextureScale("_BumpMap", new Vector2(size.x / BigWaveMetres, size.z / BigWaveMetres));
            water.SetTextureScale("_DetailAlbedoMap", new Vector2(size.x / SmallWaveMetres, size.z / SmallWaveMetres)); // the detail normal uses this tiling
        }

        private void Update()
        {
            if (water == null) return;
            water.SetTextureOffset("_BumpMap", BigDrift * Time.time);
            water.SetTextureOffset("_DetailAlbedoMap", SmallDrift * Time.time);
        }

        private void OnDestroy()
        {
            if (water != null) Destroy(water);
        }
    }
}
