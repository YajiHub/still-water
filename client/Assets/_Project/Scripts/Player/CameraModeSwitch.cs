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
        /// <summary>The first person rig is driving the camera right now (not the Reveal or the catch camera).</summary>
        public bool FirstPersonActive => firstPerson != null && firstPerson.enabled;

        // The Settings page's Camera choice; the in-game 1st/3rd pill changes it too, and it's remembered.
        private void Start() => Apply(Core.GameState.Save != null ? Core.GameSettings.Current.firstPerson : startInFirstPerson);

        public void Toggle()
        {
            Apply(!IsFirstPerson);
            Core.GameSettings.Current.firstPerson = IsFirstPerson;
            Core.GameSettings.Changed();
        }

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
