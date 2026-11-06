using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// Moves a ferry platform between end A and end B (positions from <see cref="FerryRules"/>). Added by Build Level.
    /// Runs before the player (execution order), so <see cref="Delta"/> is ready when PlayerMotor carries you.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlatformMover : MonoBehaviour
    {
        [Tooltip("World position of end A (where the level JSON puts it)")]
        [SerializeField] private Vector3 pointA;
        [Tooltip("From A to B, metres")]
        [SerializeField] private Vector3 travel;
        [Tooltip("seconds, one way")] [SerializeField] private float seconds = 3f;
        [Tooltip("seconds it waits at each end")] [SerializeField] private float pause = 1f;
        [Tooltip("seconds into the cycle at level start")] [SerializeField] private float offset;

        /// <summary>How far it moved this frame: whoever stands on it moves the same.</summary>
        public Vector3 Delta { get; private set; }

        private void Update()
        {
            var next = pointA + travel * FerryRules.Progress(Time.timeSinceLevelLoad, seconds, pause, offset);
            Delta = next - transform.position;
            transform.position = next;
        }
    }
}
