using StarterAssets;
using StillWater.Level;
using StillWater.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StillWater.UI
{
    /// <summary>
    /// Phase 1 touch layout (Control Scheme decision): left half = floating move stick, right half = drag to look,
    /// bottom-right = Jump (becomes Climb next to a safe edge in the water), top-left = pause, top-right = camera toggle + FPS.
    /// Writes into StarterAssetsInputs, which PlayerMotor and the camera rigs read.
    /// Laptop: WASD move, Space jump, hold right mouse (or arrow keys) to look, C switches camera, L switches look.
    /// In the Editor a left-click also acts as a finger.
    /// </summary>
    // shortcut: drawn with IMGUI (OnGUI) to avoid UI assets during the prototype; rebuild in UI Toolkit in Phase 5 polish.
    public class TouchControls : MonoBehaviour
    {
        [SerializeField] private StarterAssetsInputs input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private MovementConfig config;
        [SerializeField] private CameraModeSwitch cameraSwitch;
        [Tooltip("Optional: Phase 1 art spike look toggle")]
        [SerializeField] private LookSwitch lookSwitch;

        private const int None = -1;
        private const float StickRadiusOfScreen = 0.10f; // of screen height
        private const float ButtonRadiusOfScreen = 0.09f;
        private const float DeadZone = 0.1f;
        private const float KeyLookDegreesPerSecond = 120f; // Editor arrow keys only

        /// <summary>The pause button (top-left) or Esc was pressed.</summary>
        public event System.Action PausePressed;

        private int moveId = None, lookId = None, jumpId = None;
        private Vector2 moveOrigin, moveCurrent, lookLast;
        private Texture2D circle;
        private GUIStyle labelStyle;
        private float smoothedFps = 60f;

        private float StickRadius => Screen.height * StickRadiusOfScreen;
        private float ButtonRadius => Screen.height * ButtonRadiusOfScreen;

        // Screen positions here use Unity's touch convention: (0,0) = bottom-left.
        private Vector2 ButtonCenter
        {
            get
            {
                Rect safe = Screen.safeArea;
                return new Vector2(safe.xMax - ButtonRadius * 1.6f, safe.yMin + ButtonRadius * 1.6f);
            }
        }

        private Rect ToggleRect
        {
            get
            {
                Rect safe = Screen.safeArea;
                float w = Screen.height * 0.22f, h = Screen.height * 0.08f;
                return new Rect(safe.xMax - w - 16f, safe.yMax - h - 16f, w, h);
            }
        }

        private Rect PauseRect
        {
            get
            {
                Rect safe = Screen.safeArea;
                float s = Screen.height * 0.1f;
                return new Rect(safe.xMin + 16f, safe.yMax - s - 16f, s, s);
            }
        }

        // Just below the camera toggle.
        private Rect LookRect
        {
            get
            {
                Rect r = ToggleRect;
                return new Rect(r.x, r.y - r.height - 12f, r.width, r.height);
            }
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
        }

        private void OnDisable()
        {
            moveId = lookId = jumpId = None;
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
            bool moveSeen = false, lookSeen = false, jumpSeen = false;

            foreach (Touch touch in Touch.activeTouches)
            {
                Vector2 p = touch.screenPosition;
                int id = touch.touchId;

                if (touch.phase == TouchPhase.Began)
                {
                    if (PauseRect.Contains(p)) PausePressed?.Invoke();
                    else if (jumpId == None && Vector2.Distance(p, ButtonCenter) <= ButtonRadius) jumpId = id;
                    else if (ToggleRect.Contains(p)) { if (cameraSwitch != null) cameraSwitch.Toggle(); }
                    else if (lookSwitch != null && LookRect.Contains(p)) lookSwitch.Next();
                    else if (p.x < Screen.width * 0.5f && moveId == None) { moveId = id; moveOrigin = p; moveCurrent = p; }
                    else if (p.x >= Screen.width * 0.5f && lookId == None) { lookId = id; lookLast = p; }
                }

                bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                if (id == moveId)
                {
                    moveSeen = !ended;
                    moveCurrent = p;
                    // Floating: if the thumb drags past the rim, the stick follows it.
                    Vector2 offset = moveCurrent - moveOrigin;
                    if (offset.magnitude > StickRadius) moveOrigin = moveCurrent - offset.normalized * StickRadius;
                }
                if (id == lookId)
                {
                    lookSeen = !ended;
                    look += (p - lookLast) * (config.lookDegreesPerScreen / Screen.height);
                    lookLast = p;
                }
                if (id == jumpId) jumpSeen = !ended;
            }

            // A finger that lifted (or vanished from the list) frees its control.
            if (!moveSeen) moveId = None;
            if (!lookSeen) lookId = None;
            if (!jumpSeen) jumpId = None;

            Vector2 move = Vector2.zero;
            if (moveId != None)
            {
                move = (moveCurrent - moveOrigin) / StickRadius;
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
                if (keyboard.lKey.wasPressedThisFrame && lookSwitch != null) lookSwitch.Next();
                if (keyboard.escapeKey.wasPressedThisFrame) PausePressed?.Invoke();
                Vector2 arrows = new Vector2(
                    (keyboard.rightArrowKey.isPressed ? 1f : 0f) - (keyboard.leftArrowKey.isPressed ? 1f : 0f),
                    (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f));
                look += arrows * (KeyLookDegreesPerSecond * Time.deltaTime);
            }

            // Laptop: hold the right mouse button and move to look.
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                look += mouse.delta.ReadValue() * (config.lookDegreesPerScreen / Screen.height);

            input.MoveInput(Vector2.ClampMagnitude(move, 1f));
            input.LookInput(look);
            input.JumpInput(jump);

            smoothedFps = Mathf.Lerp(smoothedFps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.05f);
        }

        private void OnGUI()
        {
            if (circle == null) circle = MakeCircle(128);
            if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            labelStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.035f);

            // Move stick: shown where the thumb landed; a faint hint at bottom-left otherwise.
            Vector2 baseCenter = moveId != None ? moveOrigin : new Vector2(Screen.safeArea.xMin + StickRadius * 1.8f, Screen.safeArea.yMin + StickRadius * 1.8f);
            Vector2 knob = moveId != None ? moveOrigin + Vector2.ClampMagnitude(moveCurrent - moveOrigin, StickRadius) : baseCenter;
            DrawCircle(baseCenter, StickRadius, new Color(1f, 1f, 1f, moveId != None ? 0.25f : 0.10f));
            DrawCircle(knob, StickRadius * 0.45f, new Color(1f, 1f, 1f, moveId != None ? 0.6f : 0.2f));

            // Action button: Jump on land, Climb at a safe edge in the water, dimmed in open water.
            bool swimming = motor.State == PlayerMotor.MotorState.Swimming;
            bool canClimb = swimming && motor.ClimbTarget != null;
            string label = canClimb ? "CLIMB" : swimming && motor.NearTooHighEdge ? "TOO HIGH" : "JUMP";
            float alpha = swimming && !canClimb ? 0.15f : (jumpId != None ? 0.7f : 0.4f);
            DrawCircle(ButtonCenter, ButtonRadius, new Color(1f, 1f, 1f, alpha));
            GUI.Label(ToGui(ButtonCenter, ButtonRadius), label, labelStyle);

            Rect pr = PauseRect;
            var pauseGui = new Rect(pr.x, Screen.height - pr.yMax, pr.width, pr.height);
            GUI.Box(pauseGui, GUIContent.none);
            GUI.Label(pauseGui, "II", labelStyle);

            // Debug: camera toggle + FPS (Phase 1 exit bar needs 30+).
            Rect r = ToggleRect;
            var guiRect = new Rect(r.x, Screen.height - r.yMax, r.width, r.height);
            GUI.Box(guiRect, GUIContent.none);
            string mode = cameraSwitch != null && cameraSwitch.IsFirstPerson ? "1st" : "3rd";
            GUI.Label(guiRect, $"{mode} · {smoothedFps:0} fps", labelStyle);

            if (lookSwitch != null)
            {
                Rect l = LookRect;
                var lookGui = new Rect(l.x, Screen.height - l.yMax, l.width, l.height);
                GUI.Box(lookGui, GUIContent.none);
                GUI.Label(lookGui, $"Look {lookSwitch.CurrentLabel}", labelStyle);
            }
        }

        private void DrawCircle(Vector2 center, float radius, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(ToGui(center, radius), circle);
            GUI.color = Color.white;
        }

        // IMGUI uses (0,0) = top-left, touches use bottom-left: flip y.
        private static Rect ToGui(Vector2 center, float radius) =>
            new Rect(center.x - radius, Screen.height - center.y - radius, radius * 2f, radius * 2f);

        private static Texture2D MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d))); // 1 px soft edge
            }
            tex.Apply();
            return tex;
        }
    }
}
