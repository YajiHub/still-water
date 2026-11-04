using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;

namespace StillWater.Player
{
    /// <summary>
    /// First person camera rig: eyes on the player, drag to look, the body turns with the view.
    /// Feel helpers: the view tilts down while falling (see the landing spot) and dips on landing.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private StarterAssetsInputs input;
        [SerializeField] private MovementConfig config;
        [Tooltip("metres above the feet")]
        [SerializeField] private float eyeHeight = 1.6f;

        private const float PitchLimit = 80f;

        private float yaw, pitch, tilt, dip, dipVelocity, lastAirVelocity;
        private Renderer[] body;
        private bool wasGrounded = true;

        private void OnEnable()
        {
            yaw = motor.transform.eulerAngles.y;
            pitch = 0f;
            GetComponent<Camera>().fieldOfView = config.firstPersonFov;
            motor.FaceMoveDirection = false;
            // Hide your own body from your eyes, but keep its shadow on the ground (the usual first-person trick).
            body = System.Array.FindAll(motor.GetComponentsInChildren<Renderer>(), r => motor.BlobShadow == null || !r.transform.IsChildOf(motor.BlobShadow));
            foreach (var r in body) r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }

        private void OnDisable()
        {
            if (body == null) return;
            foreach (var r in body) r.shadowCastingMode = ShadowCastingMode.On;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            yaw += input.look.x;
            pitch = Mathf.Clamp(pitch - input.look.y, -PitchLimit, PitchLimit);
            motor.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            bool falling = motor.State == PlayerMotor.MotorState.Airborne && motor.VerticalVelocity < 0f;
            tilt = Mathf.MoveTowards(tilt, falling ? config.airTiltDegrees : 0f, 60f * dt);

            bool grounded = motor.State == PlayerMotor.MotorState.Grounded;
            if (!grounded) lastAirVelocity = motor.VerticalVelocity;
            if (grounded && !wasGrounded) dip = -config.landingDip * Mathf.Clamp01(-lastAirVelocity / 10f);
            wasGrounded = grounded;
            dip = Mathf.SmoothDamp(dip, 0f, ref dipVelocity, 0.12f);

            transform.SetPositionAndRotation(
                motor.transform.position + Vector3.up * (eyeHeight + dip),
                Quaternion.Euler(pitch + tilt, yaw, 0f));
        }
    }
}
