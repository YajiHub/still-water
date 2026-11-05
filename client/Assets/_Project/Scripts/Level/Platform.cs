using UnityEngine;

namespace StillWater.Level
{
    /// <summary>Something with an edge in the water. Climbable = you can climb onto it from the water (the Exit is not). Added by Build Level.</summary>
    public class Platform : MonoBehaviour
    {
        [SerializeField] private int id;
        [SerializeField] private bool climbable = true;

        public int Id => id;
        public bool Climbable => climbable;
    }
}
