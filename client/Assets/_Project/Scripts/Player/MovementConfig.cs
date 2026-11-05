using UnityEngine;

namespace StillWater.Player
{
    /// <summary>
    /// Every movement and camera number in one asset (DESIGN §3.2–3.3, all Draft: tune on the phone).
    /// Edit the asset in the Inspector, even during Play: changes apply live.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Still Water/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        [Header("Run")]
        [Tooltip("m/s")] public float runSpeed = 4.5f;
        [Tooltip("m/s² toward the target speed on the ground (higher = snappier starts and stops)")] public float groundAcceleration = 40f;
        [Tooltip("0–1: share of ground acceleration you keep mid-air")] [Range(0f, 1f)] public float airControl = 0.6f;

        [Header("Jump")]
        [Tooltip("metres")] public float jumpHeight = 1.2f;
        [Tooltip("m/s² (negative = down)")] public float gravity = -20f;
        [Tooltip("Extra gravity while rising after Jump is released (variable jump height)")] public float lowJumpMultiplier = 1.5f;
        [Tooltip("seconds you can still jump after walking off an edge")] public float coyoteTime = 0.10f;
        [Tooltip("seconds a Jump press is remembered before landing")] public float jumpBuffer = 0.10f;
        [Tooltip("m/s")] public float maxFallSpeed = 20f;

        [Header("Level geometry")]
        [Tooltip("metres the platform collider sticks out past the visual, per side (applied by Build Level)")] public float edgeForgiveness = 0.10f;

        [Header("Water")]
        [Tooltip("m/s")] public float swimSpeed = 2.0f;
        [Tooltip("how far the feet float below the surface (head stays above)")] public float swimDepth = 1.4f;
        [Tooltip("metres from the player to a platform edge for Climb to appear")] public float climbReach = 0.9f;
        [Tooltip("seconds the climb-out takes")] public float climbDuration = 0.35f;
        [Tooltip("degrees: the edge must be within this angle of where you face to climb it")] [Range(0f, 180f)] public float climbMaxAngle = 60f;

        [Header("Look")]
        [Tooltip("degrees turned by dragging the full screen height")] public float lookDegreesPerScreen = 180f;
        [Tooltip("first person field of view, degrees")] public float firstPersonFov = 80f;
        [Tooltip("first person: degrees the view tilts down while falling")] public float airTiltDegrees = 12f;
        [Tooltip("first person: metres the view dips on a hard landing")] public float landingDip = 0.12f;
        [Tooltip("third person field of view, degrees")] public float thirdPersonFov = 65f;
        [Tooltip("third person camera distance, metres")] public float thirdPersonDistance = 4f;
    }
}
