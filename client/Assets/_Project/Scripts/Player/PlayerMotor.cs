using System;
using StarterAssets;
using StillWater.Level;
using UnityEngine;

namespace StillWater.Player
{
    /// <summary>
    /// The one movement script for both cameras (decision 15): run, jump with the feel helpers,
    /// air control, gravity, deep-water swimming at the surface, and Climb onto a safe platform.
    /// Reads StarterAssetsInputs (filled by TouchControls); every number comes from MovementConfig.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        public enum MotorState { Grounded, Airborne, Swimming, Climbing }

        [SerializeField] private MovementConfig config;
        [SerializeField] private StarterAssetsInputs input;
        [SerializeField] private PoolWater water;
        [Tooltip("Optional dark disc that shows where you'll land")]
        [SerializeField] private Transform blobShadow;

        public MotorState State { get; private set; } = MotorState.Grounded;
        public float VerticalVelocity => verticalVelocity;
        /// <summary>Non-null while swimming next to a platform you can climb onto.</summary>
        public Platform ClimbTarget { get; private set; }
        /// <summary>True while swimming next to an edge you can't climb (the Exit), so the UI can say "too high".</summary>
        public bool NearTooHighEdge { get; private set; }
        /// <summary>Third person turns the body toward the move direction; first person lets the camera turn it.</summary>
        public bool FaceMoveDirection { get; set; }
        /// <summary>Off during the Reveal and after the level ends: no moving or jumping, gravity still works.</summary>
        public bool InputEnabled { get; set; } = true;
        /// <summary>The platform under your feet this frame (null in the air, in the water, or on non-platform ground).</summary>
        public Platform GroundPlatform { get; private set; }
        /// <summary>Fired when the player falls into the water (the Listener hears this in Phase 2).</summary>
        public event Action Splashed;
        /// <summary>The landing disc (not part of the body, so first person keeps it visible).</summary>
        public Transform BlobShadow => blobShadow;

        private const float Never = float.NegativeInfinity;
        private const float TurnSpeedDegrees = 720f;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float lastGroundedTime = Never;
        private float lastJumpPressedTime = Never;
        private bool jumpWasHeld;
        private Vector3 climbFrom, climbTo;
        private float climbTime;
        private readonly Collider[] overlapHits = new Collider[8];
        private Collider groundHit;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            // Rebuilding the level replaces the Water object and empties this link, so find it again.
            if (water == null) water = FindFirstObjectByType<PoolWater>();
        }

        private void Update()
        {
            float now = Time.time;
            float dt = Time.deltaTime;

            bool jumpHeld = InputEnabled && input.jump;
            bool jumpPressed = jumpHeld && !jumpWasHeld;
            jumpWasHeld = jumpHeld;
            if (jumpPressed) lastJumpPressedTime = now;

            switch (State)
            {
                case MotorState.Climbing:
                    UpdateClimb(dt);
                    return;
                case MotorState.Swimming:
                    UpdateSwim(dt, jumpPressed);
                    break;
                default:
                    UpdateLand(now, dt, jumpHeld);
                    break;
            }

            if (FaceMoveDirection && horizontalVelocity.sqrMagnitude > 0.01f)
            {
                var target = Quaternion.LookRotation(horizontalVelocity);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnSpeedDegrees * dt);
            }
        }

        private void UpdateLand(float now, float dt, bool jumpHeld)
        {
            // Standing on a ferry: it already moved this frame (it runs first), so move with it.
            if (GroundPlatform != null && GroundPlatform.TryGetComponent(out PlatformMover ferry)) controller.Move(ferry.Delta);
            bool grounded = controller.isGrounded;
            if (grounded) lastGroundedTime = now;

            float accel = config.groundAcceleration * (grounded ? 1f : config.airControl);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, WishDirection() * config.runSpeed, accel * dt);

            if (JumpRules.CanJump(now, lastGroundedTime, lastJumpPressedTime, config.coyoteTime, config.jumpBuffer))
            {
                verticalVelocity = JumpRules.JumpVelocity(config.jumpHeight, config.gravity);
                // Use up both the ground and the press, so one press = one jump.
                lastGroundedTime = Never;
                lastJumpPressedTime = Never;
                grounded = false;
            }
            else if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f; // small push down keeps isGrounded true on flat ground
            }

            verticalVelocity += config.gravity * JumpRules.GravityScale(verticalVelocity, jumpHeld, config.lowJumpMultiplier) * dt;
            verticalVelocity = Mathf.Max(verticalVelocity, -config.maxFallSpeed);

            groundHit = null;
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            GroundPlatform = groundHit != null ? groundHit.GetComponentInParent<Platform>() : null;
            State = grounded || controller.isGrounded ? MotorState.Grounded : MotorState.Airborne;

            if (water != null && transform.position.y < water.SurfaceY)
            {
                GroundPlatform = null;
                State = MotorState.Swimming;
                verticalVelocity *= 0.3f; // the water brakes the fall
                Splashed?.Invoke();
            }
        }

        private void UpdateSwim(float dt, bool jumpPressed)
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, WishDirection() * config.swimSpeed, config.groundAcceleration * dt);

            // Float: ease the feet toward swimDepth below the surface. Deep water, so there's no floor to stand on.
            float targetY = water.SurfaceY - config.swimDepth;
            verticalVelocity = Mathf.Lerp(verticalVelocity, (targetY - transform.position.y) * 3f, 5f * dt);
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);

            ClimbTarget = FindClimbTarget();
            if (jumpPressed && ClimbTarget != null) StartClimb(ClimbTarget);
        }

        private Platform FindClimbTarget()
        {
            Vector3 atSurface = new Vector3(transform.position.x, water.SurfaceY, transform.position.z);
            int count = Physics.OverlapSphereNonAlloc(atSurface, config.climbReach, overlapHits, ~0, QueryTriggerInteraction.Ignore);
            Platform best = null;
            float bestDistance = float.MaxValue;
            NearTooHighEdge = false;
            for (int i = 0; i < count; i++)
            {
                var platform = overlapHits[i].GetComponentInParent<Platform>();
                if (platform == null) continue;
                Vector3 closest = overlapHits[i].ClosestPoint(atSurface);
                if (!IsFacing(closest)) continue; // you climb what's in front of you, not behind or beside
                if (!platform.Climbable)
                {
                    NearTooHighEdge = true;
                    continue;
                }
                float d = (closest - atSurface).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = platform;
                }
            }
            return best;
        }

        private bool IsFacing(Vector3 point)
        {
            Vector3 toEdge = Vector3.ProjectOnPlane(point - transform.position, Vector3.up);
            if (toEdge.sqrMagnitude < 0.0001f) return true; // already touching it
            return Vector3.Angle(transform.forward, toEdge) <= config.climbMaxAngle;
        }

        private void StartClimb(Platform platform)
        {
            Bounds top = platform.GetComponent<Collider>().bounds;
            const float inset = 0.5f; // land a little way in from the edge, not on it
            climbFrom = transform.position;
            climbTo = new Vector3(
                Mathf.Clamp(transform.position.x, top.min.x + inset, top.max.x - inset),
                top.max.y + 0.05f,
                Mathf.Clamp(transform.position.z, top.min.z + inset, top.max.z - inset));
            climbTime = 0f;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            ClimbTarget = null;
            NearTooHighEdge = false;
            controller.enabled = false; // move through the platform's edge without colliding
            State = MotorState.Climbing;
        }

        private void UpdateClimb(float dt)
        {
            climbTime += dt;
            float t = Mathf.Clamp01(climbTime / config.climbDuration);
            // Up first, then forward: feels like pulling yourself over the edge.
            var p = Vector3.Lerp(climbFrom, climbTo, t * t);
            p.y = Mathf.Lerp(climbFrom.y, climbTo.y, Mathf.Sqrt(t));
            transform.position = p;
            if (t >= 1f)
            {
                controller.enabled = true;
                State = MotorState.Grounded;
            }
        }

        /// <summary>Stick input turned into a world direction, relative to where the camera looks.</summary>
        private Vector3 WishDirection()
        {
            if (!InputEnabled) return Vector3.zero;
            Transform view = Camera.main != null ? Camera.main.transform : transform;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            return Vector3.ClampMagnitude(forward * input.move.y + right * input.move.x, 1f);
        }

        // Unity calls this during controller.Move for everything the capsule touches.
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f) groundHit = hit.collider; // mostly upward = something we stand on
        }

        private void LateUpdate()
        {
            if (blobShadow == null) return;
            bool show = State == MotorState.Airborne || State == MotorState.Grounded;
            if (show && Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 spot = hit.point;
                // Over water the ray hits the pool floor; show the disc on the surface instead: "you'll land in the water".
                if (water != null && spot.y < water.SurfaceY) spot.y = water.SurfaceY;
                blobShadow.SetPositionAndRotation(spot + Vector3.up * 0.02f, Quaternion.identity);
                blobShadow.gameObject.SetActive(true);
            }
            else
            {
                blobShadow.gameObject.SetActive(false);
            }
        }
    }
}
