using System.Collections;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// Something with an edge in the water. Climbable = you can climb onto it from the water (the Exit is not).
    /// A fake platform sinks a moment after you land on it (Core Loop Rules). Added by Build Level.
    /// </summary>
    public class Platform : MonoBehaviour
    {
        [SerializeField] private int id;
        [SerializeField] private bool climbable = true;
        [Tooltip("On the level's safe path (glows in the Reveal). Off-path platforms become fakes on levels with fakePlatforms on.")]
        [SerializeField] private bool onSafePath;

        private const float SinkDepth = 2.5f, SinkSeconds = 0.6f; // how it disappears (looks), not when (config)

        public int Id => id;
        public bool Climbable => climbable;
        public bool OnSafePath => onSafePath;
        public bool IsFake { get; private set; }
        public bool IsSinking { get; private set; }

        public void MakeFake() => IsFake = true;

        /// <summary>Sinking tiles switch this off while they're under water.</summary>
        public void SetClimbable(bool value) => climbable = value;

        /// <summary>The player landed here. A fake starts sinking after <paramref name="delay"/> seconds; safe ones ignore it.</summary>
        public void SteppedOn(float delay)
        {
            if (!IsFake || IsSinking) return;
            StartCoroutine(Sink(delay));
        }

        private IEnumerator Sink(float delay)
        {
            IsSinking = true;
            yield return new WaitForSeconds(delay);
            climbable = false;
            GetComponent<Collider>().enabled = false; // drop the player now; coyote time still allows a late jump
            Vector3 from = transform.position, to = from + Vector3.down * SinkDepth;
            for (float t = 0f; t < 1f; t += Time.deltaTime / SinkSeconds)
            {
                transform.position = Vector3.Lerp(from, to, t * t);
                yield return null;
            }
            transform.position = to;
        }
    }
}
