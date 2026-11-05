using UnityEngine;

namespace StillWater.Level
{
    /// <summary>The pool's water surface. Deep: below it the player swims, never stands.</summary>
    public class PoolWater : MonoBehaviour
    {
        public float SurfaceY => transform.position.y;
        /// <summary>The water's footprint (the Listener spawns at its far corners).</summary>
        public Bounds Area => GetComponent<Renderer>().bounds;
    }
}
