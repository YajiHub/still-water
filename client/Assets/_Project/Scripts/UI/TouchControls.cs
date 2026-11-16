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
    /// The touch layout (Control Scheme decision):
    /// - left half: a floating move stick;
    /// - right half: drag to look;
    /// - bottom-right: JUMP (CLIMB next to a safe edge in the water);
    /// - online only: PUSH, just above it;
    /// - top-left: pause;
    /// - top-right: camera toggle + FPS.
    /// Settings (save.json): control size, look sensitivity, and a left-handed layout (stick on the right,
    /// JUMP/PUSH on the left).
    /// Writes into StarterAssetsInputs, which PlayerMotor and the camera rigs read.
    /// Laptop: WASD move, Space jump, F push, hold right mouse (or arrow keys) to look, C switches camera.
    /// In the Editor a left-click also acts as a finger.
    /// Drawn in the game's style: dark glass discs with a glowing rim, like the platform strips. Sized from the
    /// safe area, so it's big enough for thumbs and isn't hidden under a notch.
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
        // Sizes as a share of Gui.Base (the safe area's height, capped on wide phones): big enough for thumbs.
        private const float StickRadiusOfBase = 0.15f, KnobOfStick = 0.42f, JumpRadiusOfBase = 0.125f, PushRadiusOfBase = 0.095f;
        private const float PauseOfBase = 0.11f, MarginOfBase = 0.05f;
        private const float DeadZone = 0.1f;
        private const float KeyLookDegreesPerSecond = 120f; // Editor arrow keys only

        private static readonly Color Glass = new Color(0.02f, 0.06f, 0.07f, 0.55f);
        private static readonly Color Cyan = new Color(0.45f, 0.95f, 0.95f, 1f);
        private static readonly Color Coral = new Color(1f, 0.5f, 0.38f, 1f);

        /// <summary>The pause button (top-left) or Esc was pressed.</summary>
        public event System.Action PausePressed;
        /// <summary>PUSH (online) or F was pressed.</summary>
        public event System.Action PushPressed;
        /// <summary>Show the PUSH button (the online circuit turns it on).</summary>
        public bool ShowPush { get; set; }

        private int moveId = None, lookId = None, jumpId = None, pushId = None;
        private Vector2 moveOrigin, moveCurrent, lookLast;
        private Texture2D disc, ring, glow;
        private GUIStyle labelStyle, smallStyle;
        private float smoothedFps = 60f, pushFlash;

        private static float Base => Gui.Base * Mathf.Clamp(GameSettings.Current.controlsScale, GameSettings.MinControlsScale, GameSettings.MaxControlsScale);
        private static bool LeftHanded => GameSettings.Current.leftHanded;
        private static Rect Safe => Screen.safeArea; // touch convention: (0,0) = bottom-left
        private float StickRadius => Base * StickRadiusOfBase;
        private float JumpRadius => Base * JumpRadiusOfBase;
        private float PushRadius => Base * PushRadiusOfBase;
        private float Margin => Base * MarginOfBase;

        // Right-handed: stick bottom-left, JUMP bottom-right. Left-handed mirrors them.
        private Vector2 JumpCenter => new Vector2(LeftHanded ? Safe.xMin + Margin + JumpRadius : Safe.xMax - Margin - JumpRadius, Safe.yMin + Margin + JumpRadius);
        // Up and toward the middle from JUMP: a thumb rolls between them.
        private Vector2 PushCenter => JumpCenter + new Vector2((LeftHanded ? 1f : -1f) * JumpRadius * 1.15f, JumpRadius * 1.35f);
        private Vector2 StickHome => new Vector2(LeftHanded ? Safe.xMax - Margin - StickRadius : Safe.xMin + Margin + StickRadius, Safe.yMin + Margin + StickRadius);
        // Which half of the screen starts the move stick (the other half looks around).
        private static bool OnStickSide(Vector2 p) => LeftHanded ? p.x >= Screen.width * 0.5f : p.x < Screen.width * 0.5f;

        private Rect PauseRect
        {
            get
            {
                float s = Base * PauseOfBase;
                return new Rect(Safe.xMin + Margin * 0.6f, Safe.yMax - s - Margin * 0.6f, s, s);
            }
        }

        private Rect ToggleRect
        {
            get
            {
                float w = Base * 0.26f, h = Base * 0.075f;
                return new Rect(Safe.xMax - w - Margin * 0.6f, Safe.yMax - h - Margin * 0.6f, w, h);
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

            foreach (Touch touch in Touch.activeTouches)
            {
                Vector2 p = touch.screenPosition;
                int id = touch.touchId;

                if (touch.phase == TouchPhase.Began)
                {
                    if (PauseRect.Contains(p)) PausePressed?.Invoke();
                    else if (jumpId == None && Vector2.Distance(p, JumpCenter) <= JumpRadius * 1.1f) jumpId = id;
                    else if (ShowPush && pushId == None && Vector2.Distance(p, PushCenter) <= PushRadius * 1.15f) { pushId = id; Push(); }
                    else if (ToggleRect.Contains(p)) { if (cameraSwitch != null) cameraSwitch.Toggle(); }
                    else if (OnStickSide(p) && moveId == None) { moveId = id; moveOrigin = p; moveCurrent = p; }
                    else if (!OnStickSide(p) && lookId == None) { lookId = id; lookLast = p; }
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
            if (disc == null) MakeTextures();
            labelStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            smallStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            labelStyle.normal.textColor = smallStyle.normal.textColor = new Color(0.9f, 0.97f, 0.98f);
            labelStyle.fontSize = Mathf.RoundToInt(Base * 0.034f);
            smallStyle.fontSize = Mathf.RoundToInt(Base * 0.026f);

            // Move stick: where the thumb landed; a faint hint at bottom-left otherwise.
            bool moving = moveId != None;
            Vector2 baseCenter = moving ? moveOrigin : StickHome;
            Vector2 knob = moving ? moveOrigin + Vector2.ClampMagnitude(moveCurrent - moveOrigin, StickRadius) : baseCenter;
            float a = moving ? 1f : 0.45f;
            Draw(glow, baseCenter, StickRadius * 1.25f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.10f * a));
            Draw(disc, baseCenter, StickRadius, new Color(Glass.r, Glass.g, Glass.b, Glass.a * a));
            Draw(ring, baseCenter, StickRadius, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f * a));
            Draw(glow, knob, StickRadius * KnobOfStick * 1.6f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.25f * a));
            Draw(disc, knob, StickRadius * KnobOfStick, new Color(0.85f, 0.97f, 0.98f, 0.85f * a));

            // JUMP: Jump on land, Climb at a safe edge in the water, dimmed in open water.
            bool swimming = motor.State == PlayerMotor.MotorState.Swimming;
            bool canClimb = swimming && motor.ClimbTarget != null;
            string label = canClimb ? "CLIMB" : swimming && motor.NearTooHighEdge ? "TOO HIGH" : "JUMP";
            bool usable = !swimming || canClimb;
            RoundButton(JumpCenter, JumpRadius, label, Cyan, jumpId != None, usable, canClimb);

            if (ShowPush) RoundButton(PushCenter, PushRadius, "PUSH", Coral, pushId != None || pushFlash > 0.5f, true, false);

            // Pause: a glass disc with "II".
            Rect pr = PauseRect;
            Vector2 pc = new Vector2(pr.center.x, pr.center.y);
            RoundButton(pc, pr.width / 2f, "II", Cyan, false, true, false);

            // Camera toggle + FPS (debug), as a small glass pill.
            Pill(ToggleRect, $"{(cameraSwitch != null && cameraSwitch.IsFirstPerson ? "1st" : "3rd")} · {smoothedFps:0} fps");
        }

        // A dark glass disc with a glowing rim; pressed = lit up; "highlight" pulses (CLIMB is there: take it).
        private void RoundButton(Vector2 center, float radius, string text, Color accent, bool pressed, bool usable, bool highlight)
        {
            float a = usable ? 1f : 0.35f;
            float pulse = highlight ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f) : 0f;
            Draw(glow, center, radius * 1.35f, new Color(accent.r, accent.g, accent.b, (pressed ? 0.45f : 0.12f + 0.25f * pulse) * a));
            Draw(disc, center, radius, pressed ? new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 0.85f) : new Color(Glass.r, Glass.g, Glass.b, Glass.a * a));
            Draw(ring, center, radius, new Color(accent.r, accent.g, accent.b, (pressed ? 1f : 0.75f) * a));
            var old = labelStyle.normal.textColor;
            labelStyle.normal.textColor = new Color(old.r, old.g, old.b, a);
            GUI.Label(ToGui(center, radius), text, labelStyle);
            labelStyle.normal.textColor = old;
        }

        private void Pill(Rect touchRect, string text)
        {
            var r = new Rect(touchRect.x, Screen.height - touchRect.yMax, touchRect.width, touchRect.height);
            GUI.color = Glass;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(r, text, smallStyle);
        }

        private static void Draw(Texture2D tex, Vector2 center, float radius, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(ToGui(center, radius), tex);
            GUI.color = Color.white;
        }

        // IMGUI uses (0,0) = top-left, touches use bottom-left: flip y.
        private static Rect ToGui(Vector2 center, float radius) =>
            new Rect(center.x - radius, Screen.height - center.y - radius, radius * 2f, radius * 2f);

        private void MakeTextures()
        {
            disc = Radial(128, d => Mathf.Clamp01((1f - d) * 64f));                                  // solid, 1 px soft edge
            ring = Radial(128, d => Mathf.Clamp01((1f - d) * 64f) * Mathf.Clamp01((d - 0.86f) * 40f)); // thin rim
            glow = Radial(128, d => Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f));                           // soft halo
        }

        /// <summary>A white texture whose alpha is f(distance from the centre, 0..1 at the edge).</summary>
        private static Texture2D Radial(int size, System.Func<float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = size / 2f;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                px[y * size + x] = new Color(1f, 1f, 1f, alpha(d));
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
