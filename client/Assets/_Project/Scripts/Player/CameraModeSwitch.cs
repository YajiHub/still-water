using UnityEngine;

namespace StillWater.Player
{
    /// <summary>Phase 1 debug: switch between the two camera rigs on the phone (the Camera Style test).</summary>
    public class CameraModeSwitch : MonoBehaviour
    {
        [SerializeField] private FirstPersonLook firstPerson;
        [SerializeField] private ThirdPersonLook thirdPerson;
        [SerializeField] private bool startInFirstPerson = true;

        public bool IsFirstPerson { get; private set; }

        private void Start() => Apply(startInFirstPerson);

        public void Toggle() => Apply(!IsFirstPerson);

        /// <summary>Both rigs off, so another script (the Reveal) can drive the camera.</summary>
        public void DisableAll()
        {
            firstPerson.enabled = false;
            thirdPerson.enabled = false;
        }

        /// <summary>Hand the camera back to the current rig.</summary>
        public void Restore() => Apply(IsFirstPerson);

        private void Apply(bool firstPersonOn)
        {
            IsFirstPerson = firstPersonOn;
            // Disable first so the newly enabled rig's OnEnable wins (it sets FOV and body turning).
            firstPerson.enabled = false;
            thirdPerson.enabled = false;
            if (firstPersonOn) firstPerson.enabled = true;
            else thirdPerson.enabled = true;
        }
    }
}
