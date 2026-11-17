using StillWater.Core;
using UnityEngine;

namespace StillWater.UI
{
    /// <summary>
    /// Where the touch controls sit, and how they're drawn. Shared by TouchControls (in a level) and the layout editor
    /// (Settings → Edit control layout), so the editor shows exactly what you'll play with.
    /// - Sizes come from Gui.Base (the safe area's height, capped on wide phones) × the Button size setting.
    /// - Positions: the default layout (stick bottom-left, JUMP bottom-right, PUSH up and in from JUMP), or the player's
    ///   own, saved 0..1 across the safe area so it fits any phone. Left-handed mirrors either one.
    /// - Screen coordinates here are touch coordinates: (0,0) = bottom-left.
    /// </summary>
    public static class ControlLayout
    {
        public enum Item { Stick, Jump, Push }

        // Sizes as a share of Gui.Base: big enough for thumbs.
        private const float StickRadiusOfBase = 0.15f, JumpRadiusOfBase = 0.125f, PushRadiusOfBase = 0.095f;
        private const float PauseOfBase = 0.11f, MarginOfBase = 0.05f;
        public const float KnobOfStick = 0.42f;

        public static readonly Color Glass = new Color(0.02f, 0.06f, 0.07f, 0.55f);
        public static readonly Color Cyan = new Color(0.45f, 0.95f, 0.95f, 1f);
        public static readonly Color Coral = new Color(1f, 0.5f, 0.38f, 1f);

        private static Settings S => GameSettings.Current;
        public static float Base => Gui.Base * Mathf.Clamp(S.controlsScale, GameSettings.MinControlsScale, GameSettings.MaxControlsScale);
        public static Rect Safe => Screen.safeArea;
        public static float Margin => Base * MarginOfBase;
        public static float StickRadius => Base * StickRadiusOfBase;
        public static bool FixedStick => S.fixedStick;

        public static float Radius(Item item) => item switch
        {
            Item.Stick => StickRadius,
            Item.Jump => Base * JumpRadiusOfBase,
            _ => Base * PushRadiusOfBase,
        };

        /// <summary>The control's centre on screen now (custom or default, mirrored when left-handed, kept on screen).</summary>
        public static Vector2 Center(Item item)
        {
            Vector2 p = S.customLayout ? FromSafe(Saved(item)) : Default(item);
            if (S.leftHanded) p.x = Safe.xMin + Safe.xMax - p.x;
            return Clamp(item, p);
        }

        /// <summary>Moves a control (layout editor). The first move turns the custom layout on, starting from the default.</summary>
        public static void Move(Item item, Vector2 screen)
        {
            if (!S.customLayout)
            {
                S.stickPos = ToSafe(Default(Item.Stick));
                S.jumpPos = ToSafe(Default(Item.Jump));
                S.pushPos = ToSafe(Default(Item.Push));
                S.customLayout = true;
            }
            Vector2 p = Clamp(item, screen);
            if (S.leftHanded) p.x = Safe.xMin + Safe.xMax - p.x; // saved right-handed
            Vector2 n = ToSafe(p);
            if (item == Item.Stick) S.stickPos = n;
            else if (item == Item.Jump) S.jumpPos = n;
            else S.pushPos = n;
        }

        public static void ResetLayout()
        {
            S.customLayout = false;
            S.fixedStick = false;
        }

        /// <summary>Which half of the screen starts the move stick: the half its home is on (the other half looks).</summary>
        public static bool OnStickSide(Vector2 p)
        {
            bool stickLeft = Center(Item.Stick).x < Screen.width * 0.5f;
            return stickLeft ? p.x < Screen.width * 0.5f : p.x >= Screen.width * 0.5f;
        }

        public static Rect PauseRect
        {
            get
            {
                float s = Base * PauseOfBase;
                return new Rect(Safe.xMin + Margin * 0.6f, Safe.yMax - s - Margin * 0.6f, s, s);
            }
        }

        public static Rect ToggleRect
        {
            get
            {
                float w = Base * 0.26f, h = Base * 0.075f;
                return new Rect(Safe.xMax - w - Margin * 0.6f, Safe.yMax - h - Margin * 0.6f, w, h);
            }
        }

        // Right-handed defaults: stick bottom-left, JUMP bottom-right, PUSH up and toward the middle from JUMP (a thumb
        // rolls between them).
        private static Vector2 Default(Item item)
        {
            float jr = Radius(Item.Jump);
            Vector2 jump = new Vector2(Safe.xMax - Margin - jr, Safe.yMin + Margin + jr);
            return item switch
            {
                Item.Stick => new Vector2(Safe.xMin + Margin + StickRadius, Safe.yMin + Margin + StickRadius),
                Item.Jump => jump,
                _ => jump + new Vector2(-jr * 1.15f, jr * 1.35f),
            };
        }

        private static Vector2 Saved(Item item) => item switch
        {
            Item.Stick => S.stickPos,
            Item.Jump => S.jumpPos,
            _ => S.pushPos,
        };

        private static Vector2 ToSafe(Vector2 p) =>
            new Vector2((p.x - Safe.x) / Mathf.Max(1f, Safe.width), (p.y - Safe.y) / Mathf.Max(1f, Safe.height));

        private static Vector2 FromSafe(Vector2 n) => new Vector2(Safe.x + n.x * Safe.width, Safe.y + n.y * Safe.height);

        // Whole control inside the safe area, and below the pause button / camera pill row.
        private static Vector2 Clamp(Item item, Vector2 p)
        {
            float r = Radius(item);
            float top = Safe.yMax - Base * PauseOfBase - Margin * 1.2f;
            return new Vector2(Mathf.Clamp(p.x, Safe.xMin + r, Safe.xMax - r), Mathf.Clamp(p.y, Safe.yMin + r, Mathf.Max(Safe.yMin + r, top - r)));
        }

        // ------------------------------------------------------------------ drawing (IMGUI, call from OnGUI)

        private static Texture2D disc, ring, glow;
        private static GUIStyle label, small;

        public static GUIStyle Label { get { Styles(); return label; } }
        public static GUIStyle Small { get { Styles(); return small; } }

        private static void Styles()
        {
            if (disc == null)
            {
                disc = Radial(128, d => Mathf.Clamp01((1f - d) * 64f));                                  // solid, 1 px soft edge
                ring = Radial(128, d => Mathf.Clamp01((1f - d) * 64f) * Mathf.Clamp01((d - 0.86f) * 40f)); // thin rim
                glow = Radial(128, d => Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f));                           // soft halo
            }
            label ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            small ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            label.normal.textColor = small.normal.textColor = new Color(0.9f, 0.97f, 0.98f);
            label.fontSize = Mathf.RoundToInt(Base * 0.034f);
            small.fontSize = Mathf.RoundToInt(Base * 0.026f);
        }

        /// <summary>The move stick: base disc and knob (alpha dims the idle hint).</summary>
        public static void DrawStick(Vector2 center, Vector2 knob, float alpha)
        {
            Styles();
            float r = StickRadius;
            Draw(glow, center, r * 1.25f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.10f * alpha));
            Draw(disc, center, r, new Color(Glass.r, Glass.g, Glass.b, Glass.a * alpha));
            Draw(ring, center, r, new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f * alpha));
            Draw(glow, knob, r * KnobOfStick * 1.6f, new Color(Cyan.r, Cyan.g, Cyan.b, 0.25f * alpha));
            Draw(disc, knob, r * KnobOfStick, new Color(0.85f, 0.97f, 0.98f, 0.85f * alpha));
        }

        /// <summary>A dark glass disc with a glowing rim; pressed = lit up; "highlight" pulses (CLIMB is there: take it).</summary>
        public static void RoundButton(Vector2 center, float radius, string text, Color accent, bool pressed, bool usable, bool highlight)
        {
            Styles();
            float a = usable ? 1f : 0.35f;
            float pulse = highlight ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f) : 0f;
            Draw(glow, center, radius * 1.35f, new Color(accent.r, accent.g, accent.b, (pressed ? 0.45f : 0.12f + 0.25f * pulse) * a));
            Draw(disc, center, radius, pressed ? new Color(accent.r * 0.35f, accent.g * 0.35f, accent.b * 0.35f, 0.85f) : new Color(Glass.r, Glass.g, Glass.b, Glass.a * a));
            Draw(ring, center, radius, new Color(accent.r, accent.g, accent.b, (pressed ? 1f : 0.75f) * a));
            var old = label.normal.textColor;
            label.normal.textColor = new Color(old.r, old.g, old.b, a);
            GUI.Label(ToGui(center, radius), text, label);
            label.normal.textColor = old;
        }

        /// <summary>A small glass pill with text (the camera toggle).</summary>
        public static void Pill(Rect touchRect, string text)
        {
            Styles();
            var r = new Rect(touchRect.x, Screen.height - touchRect.yMax, touchRect.width, touchRect.height);
            GUI.color = Glass;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(r, text, small);
        }

        private static void Draw(Texture2D tex, Vector2 center, float radius, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(ToGui(center, radius), tex);
            GUI.color = Color.white;
        }

        // IMGUI uses (0,0) = top-left, touches use bottom-left: flip y.
        public static Rect ToGui(Vector2 center, float radius) =>
            new Rect(center.x - radius, Screen.height - center.y - radius, radius * 2f, radius * 2f);

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
