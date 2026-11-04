using UnityEngine;

namespace StillWater.Level
{
    /// <summary>The pool's water surface. Deep: below it the player swims, never stands.</summary>
    public class PoolWater : MonoBehaviour
    {
        public float SurfaceY => transform.position.y;
    }
}
