using StarterAssets;
using StillWater.Core;
using StillWater.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StillWater.UI
{
    /// <summary>
    /// The touch layout (Control Scheme decision). By default:
    /// - left half: a floating move stick;
    /// - right half: drag to look;
    /// - bottom-right: JUMP (CLIMB next to a safe edge in the water);
    /// - online only: PUSH, just above it;
    /// - top-left: pause;
    /// - top-right: camera toggle + FPS.
    /// The player can rearrange it (Settings → Edit control layout: drag the stick, JUMP and PUSH anywhere, a fixed or
    /// floating stick, button size) and mirror it (left-handed). Where everything sits, and the drawing, live in
    /// ControlLayout, which the layout editor shares. The stick works on the half of the screen its home is on; the
    /// other half looks around.
    /// Writes into StarterAssetsInputs, which PlayerMotor and the camera rigs read.
    /// Laptop: WASD move, Space jump, F push, hold right mouse (or arrow keys) to look, C switches camera.
    /// In the Editor a left-click also acts as a finger.
    /// </summary>
    // shortcut: drawn with IMGUI (OnGUI) to avoid UI assets during the prototype; rebuild in UI Toolkit in Phase 5 polish.
    public class TouchControls : MonoBehaviour
    {
        [SerializeField] private StarterAssetsInputs input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private MovementConfig config;
        [SerializeField] private CameraModeSwitch cameraSwitch;
        // (The Phase 1 "Look A/C" scenery toggle was removed: the game always uses Look C. The old scene field is ignored.)

        private const int None = -1;
        private const float DeadZone = 0.1f;
        private const float FixedStickReach = 1.6f;         // a fixed stick answers touches this many radii from its centre
        private const float KeyLookDegreesPerSecond = 120f; // Editor arrow keys only

        /// <summary>The pause button (top-left) or Esc was pressed.</summary>
        public event System.Action PausePressed;
        /// <summary>PUSH (online) or F was pressed.</summary>
        public event System.Action PushPressed;
        /// <summary>Show the PUSH button (the online circuit turns it on).</summary>
        public bool ShowPush { get; set; }

        private int moveId = None, lookId = None, jumpId = None, pushId = None;
        private Vector2 moveOrigin, moveCurrent, lookLast;
        private float smoothedFps = 60f, pushFlash;

        private static float StickRadius => ControlLayout.StickRadius;
        private static float JumpRadius => ControlLayout.Radius(ControlLayout.Item.Jump);
        private static float PushRadius => ControlLayout.Radius(ControlLayout.Item.Push);
        private static Vector2 JumpCenter => ControlLayout.Center(ControlLayout.Item.Jump);
        private static Vector2 PushCenter => ControlLayout.Center(ControlLayout.Item.Push);
        private static Vector2 StickHome => ControlLayout.Center(ControlLayout.Item.Stick);

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
        }

        private void OnDisable()
        {
            moveId = lookId = jumpId = pushId = None;
            if (input != null)
            {
                input.MoveInput(Vector2.zero);
                input.LookInput(Vector2.zero);
                input.JumpInput(false);
            }
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            Vector2 look = Vector2.zero;
            bool moveSeen = false, lookSeen = false, jumpSeen = false, pushSeen = false;
            bool fixedStick = ControlLayout.FixedStick;

            foreach (Touch touch in Touch.activeTouches)
            {
                Vector2 p = touch.screenPosition;
                int id = touch.touchId;

                if (touch.phase == TouchPhase.Began)
                {
                    if (ControlLayout.PauseRect.Contains(p)) PausePressed?.Invoke();
                    else if (jumpId == None && Vector2.Distance(p, JumpCenter) <= JumpRadius * 1.1f) jumpId = id;
                    else if (ShowPush && pushId == None && Vector2.Distance(p, PushCenter) <= PushRadius * 1.15f) { pushId = id; Push(); }
                    else if (ControlLayout.ToggleRect.Contains(p)) { if (cameraSwitch != null) cameraSwitch.Toggle(); }
                    else if (moveId == None && fixedStick && Vector2.Distance(p, StickHome) <= StickRadius * FixedStickReach)
                    {
                        moveId = id; moveOrigin = StickHome; moveCurrent = p; // fixed: the stick stays put
                    }
                    else if (moveId == None && !fixedStick && ControlLayout.OnStickSide(p)) { moveId = id; moveOrigin = p; moveCurrent = p; }
                    // Look: the other half of the screen; with a fixed stick, anywhere else.
                    else if (lookId == None && (fixedStick || !ControlLayout.OnStickSide(p))) { lookId = id; lookLast = p; }
                }

                bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                if (id == moveId)
                {
                    moveSeen = !ended;
                    moveCurrent = p;
                    // Floating: if the thumb drags past the rim, the stick follows it. A fixed stick just clamps the knob.
                    Vector2 offset = moveCurrent - moveOrigin;
                    if (!fixedStick && offset.magnitude > StickRadius) moveOrigin = moveCurrent - offset.normalized * StickRadius;
                }
                if (id == lookId)
                {
                    lookSeen = !ended;
                    look += (p - lookLast) * (config.lookDegreesPerScreen * LookSensitivity / Screen.height);
                    lookLast = p;
                }
                if (id == jumpId) jumpSeen = !ended;
                if (id == pushId) pushSeen = !ended;
            }

            // A finger that lifted (or vanished from the list) frees its control.
            if (!moveSeen) moveId = None;
            if (!lookSeen) lookId = None;
            if (!jumpSeen) jumpId = None;
            if (!pushSeen) pushId = None;

            Vector2 move = Vector2.zero;
            if (moveId != None)
            {
                move = Vector2.ClampMagnitude((moveCurrent - moveOrigin) / StickRadius, 1f);
                if (move.magnitude < DeadZone) move = Vector2.zero;
            }

            bool jump = jumpId != None;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Vector2 keys = new Vector2(
                    (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                    (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
                if (keys != Vector2.zero) move = keys.normalized;
                jump |= keyboard.spaceKey.isPressed;
                if (keyboard.cKey.wasPressedThisFrame && cameraSwitch != null) cameraSwitch.Toggle();
                if (keyboard.escapeKey.wasPressedThisFrame) PausePressed?.Invoke();
                if (ShowPush && keyboard.fKey.wasPressedThisFrame) Push();
                Vector2 arrows = new Vector2(
                    (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f),
                    (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f));
                look += arrows * (KeyLookDegreesPerSecond * Time.deltaTime);
            }

            // Laptop: hold the right mouse button and move to look.
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                look += mouse.delta.ReadValue() * (config.lookDegreesPerScreen * LookSensitivity / Screen.height);

            input.MoveInput(Vector2.ClampMagnitude(move, 1f));
            input.LookInput(look);
            input.JumpInput(jump);

            pushFlash = Mathf.Max(0f, pushFlash - Time.unscaledDeltaTime * 3f);
            smoothedFps = Mathf.Lerp(smoothedFps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);
        }

        private static float LookSensitivity => Mathf.Clamp(GameSettings.Current.lookSensitivity, GameSettings.MinLook, GameSettings.MaxLook);

        private void Push()
        {
            pushFlash = 1f;
            PushPressed?.Invoke();
        }

        private void OnGUI()
        {
            // Move stick: under the thumb (or at its fixed place); a faint hint at its home otherwise.
            bool moving = moveId != None;
            Vector2 baseCenter = moving ? moveOrigin : StickHome;
            Vector2 knob = moving ? moveOrigin + Vector2.ClampMagnitude(moveCurrent - moveOrigin, StickRadius) : baseCenter;
            ControlLayout.DrawStick(baseCenter, knob, moving ? 1f : ControlLayout.FixedStick ? 0.7f : 0.45f);

            // JUMP: Jump on land, Climb at a safe edge in the water, dimmed in open water.
            bool swimming = motor.State == PlayerMotor.MotorState.Swimming;
            bool canClimb = swimming && motor.ClimbTarget != null;
            string label = canClimb ? "CLIMB" : swimming && motor.NearTooHighEdge ? "TOO HIGH" : "JUMP";
            bool usable = !swimming || canClimb;
            ControlLayout.RoundButton(JumpCenter, JumpRadius, label, ControlLayout.Cyan, jumpId != None, usable, canClimb);

            if (ShowPush) ControlLayout.RoundButton(PushCenter, PushRadius, "PUSH", ControlLayout.Coral, pushId != None || pushFlash > 0.5f, true, false);

            // Pause: a glass disc with "II".
            Rect pr = ControlLayout.PauseRect;
            ControlLayout.RoundButton(pr.center, pr.width / 2f, "II", ControlLayout.Cyan, false, true, false);

            // Camera toggle + FPS (debug), as a small glass pill.
            ControlLayout.Pill(ControlLayout.ToggleRect, $"{(cameraSwitch != null && cameraSwitch.IsFirstPerson ? "1st" : "3rd")} · {smoothedFps:0} fps");
        }
    }
}
