using StarterAssets;
using UnityEngine;

namespace StillWater.Player
{
    /// <summary>
    /// Third person camera rig: orbits behind the player, drag to look, pulls in when a wall is in the way.
    /// The body turns toward where you move. Our own small orbit instead of Cinemachine, so both
    /// cameras read input the exact same way (fair comparison for the Camera Style test).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ThirdPersonLook : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private StarterAssetsInputs input;
        [SerializeField] private MovementConfig config;
        [Tooltip("metres above the feet the camera orbits around")]
        [SerializeField] private float pivotHeight = 1.5f;

        private const float MinPitch = -20f, MaxPitch = 70f, StartPitch = 20f, WallPadding = 0.2f;

        private float yaw, pitch;

        private void OnEnable()
        {
            yaw = motor.transform.eulerAngles.y;
            pitch = StartPitch;
            GetComponent<Camera>().fieldOfView = config.thirdPersonFov;
            motor.FaceMoveDirection = true;
        }

        private void LateUpdate()
        {
            yaw += input.look.x;
            pitch = Mathf.Clamp(pitch - input.look.y, MinPitch, MaxPitch);

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = motor.transform.position + Vector3.up * pivotHeight;
            Vector3 back = rotation * Vector3.back;
            float distance = config.thirdPersonDistance;
            if (Physics.SphereCast(pivot, WallPadding, back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore))
                distance = hit.distance;

            transform.SetPositionAndRotation(pivot + back * distance, rotation);
        }
    }
}
