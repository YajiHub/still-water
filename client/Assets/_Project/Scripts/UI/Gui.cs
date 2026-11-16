using UnityEngine;

namespace StillWater.UI
{
    /// <summary>
    /// Phone-sized IMGUI helpers for every screen (menu, HUD, results), sized from the screen height. The style is
    /// "a dark pool at night": dark glass panels with a thin cold-cyan accent bar like the platform glow strips; a
    /// spaced-out title that glows and sometimes flickers like a dying tube; a vignette and fade-ins.
    /// </summary>
    // shortcut: IMGUI screens; rebuild in UI Toolkit with real art in Phase 5 polish.
    public static class Gui
    {
        private static readonly Color Ink = new Color(0.86f, 0.95f, 0.96f);
        private static readonly Color InkDim = new Color(0.62f, 0.74f, 0.76f);
        private static readonly Color Panel = new Color(0.01f, 0.04f, 0.05f, 0.62f);
        private static readonly Color PanelPressed = new Color(0.10f, 0.26f, 0.29f, 0.85f);
        private static readonly Color Accent = new Color(0.45f, 0.95f, 0.95f, 0.9f);
        private static readonly Color Glow = new Color(0.35f, 0.9f, 0.95f, 0.08f);

        private static GUIStyle label, title, button;
        private static Texture2D panel, pressed, vignette;

        /// <summary>The usable screen (inside notches and rounded corners), in GUI coordinates (top-left origin).</summary>
        public static Rect Safe
        {
            get
            {
                Rect s = Screen.safeArea;
                return new Rect(s.x, Screen.height - s.yMax, s.width, s.height);
            }
        }

        // Sizes follow the safe area's height, but never more than a width-based limit: on a wide phone in landscape,
        // height alone made everything huge.
        /// <summary>The size everything scales from: the safe area's height, capped by its width (wide phones).</summary>
        public static float Base => Mathf.Min(Safe.height, Safe.width * 0.5f);
        public static float Unit => Base / 20f; // one "row" of UI

        /// <summary>Lay rows out as a column on the left (the title page) instead of centred.</summary>
        public static bool LeftColumn { get; set; }

        public static GUIStyle Label => Align(Style(ref label, 0.032f, FontStyle.Normal, false));
        public static GUIStyle Title => Align(Style(ref title, 0.085f, FontStyle.Bold, false));
        public static GUIStyle ButtonStyle => Align(Style(ref button, 0.03f, FontStyle.Bold, true), true);

        private static GUIStyle Align(GUIStyle s, bool button = false)
        {
            s.alignment = LeftColumn ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            s.padding = new RectOffset(LeftColumn && button ? Mathf.RoundToInt(Unit * 0.6f) : 0, 0, 0, 0);
            return s;
        }

        /// <summary>A button centred horizontally at row <paramref name="row"/> (0 = top, 20 rows per screen).</summary>
        public static bool Button(float row, string text, float widthOfScreen = 0.3f)
        {
            Rect r = Row(row, widthOfScreen, 1.2f);
            float a = GUI.enabled ? 1f : 0.25f;
            bool clicked = GUI.Button(r, Spaced(text.ToUpperInvariant(), 1), ButtonStyle);
            // An accent bar on the left and a hairline underneath, like the glow strips on the platforms.
            GUI.color = new Color(Accent.r, Accent.g, Accent.b, Accent.a * a);
            GUI.DrawTexture(new Rect(r.x, r.y, Mathf.Max(3f, Unit * 0.08f), r.height), Texture2D.whiteTexture);
            GUI.color = new Color(Accent.r, Accent.g, Accent.b, 0.35f * a);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            return clicked;
        }

        private static GUIStyle track, thumb, sliderLabel;

        /// <summary>
        /// A labelled slider row: name on the left, a cyan-thumbed track, the value on the right. Returns the new value.
        /// </summary>
        public static float Slider(float row, string name, float value, float min, float max, string valueText, float widthOfScreen = 0.62f)
        {
            Rect r = Row(row, widthOfScreen, 1.2f);
            panel ??= Solid(Panel);
            sliderLabel ??= new GUIStyle { alignment = TextAnchor.MiddleLeft, wordWrap = false };
            sliderLabel.normal.textColor = Ink;
            sliderLabel.fontSize = Mathf.RoundToInt(Base * 0.03f);
            track ??= new GUIStyle { normal = { background = Solid(new Color(0.12f, 0.28f, 0.3f, 0.9f)) } };
            thumb ??= new GUIStyle { normal = { background = Solid(Accent) }, active = { background = Solid(Color.white) } };
            float h = r.height;
            GUI.DrawTexture(r, panel);
            GUI.Label(new Rect(r.x + h * 0.4f, r.y, r.width * 0.36f, h), name, sliderLabel);
            var bar = new Rect(r.x + r.width * 0.4f, r.y + h * 0.42f, r.width * 0.44f, h * 0.16f);
            track.fixedHeight = bar.height;
            thumb.fixedWidth = h * 0.36f;
            thumb.fixedHeight = h * 0.62f;
            thumb.margin = new RectOffset(0, 0, Mathf.RoundToInt(-h * 0.23f), 0);
            float v = GUI.HorizontalSlider(bar, value, min, max, track, thumb);
            sliderLabel.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(r.xMax - r.width * 0.14f - h * 0.3f, r.y, r.width * 0.14f, h), valueText, sliderLabel);
            sliderLabel.alignment = TextAnchor.MiddleLeft;
            GUI.color = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            return v;
        }

        public static void Text(float row, string text, GUIStyle style = null, float height = 1.2f) =>
            GUI.Label(Row(row, 0.9f, height), text, style ?? Label);

        /// <summary>
        /// The game's big title: letters spaced out, a soft cold glow, and now and then a flicker like a failing
        /// fluorescent tube.
        /// </summary>
        public static void BigTitle(float row, string text, float height = 3f)
        {
            Rect r = Row(row, 0.95f, height);
            string spaced = Spaced(text, 2);
            float t = Time.unscaledTime;
            bool dip = Mathf.PerlinNoise(t * 3.1f, 0.5f) > 0.78f && Mathf.PerlinNoise(t * 23f, 1.7f) > 0.45f;
            float alpha = dip ? 0.25f : 0.9f + 0.1f * Mathf.Sin(t * 1.3f);
            var style = Title;
            var old = style.normal.textColor;
            float spread = Unit * 0.12f;
            style.normal.textColor = new Color(Glow.r, Glow.g, Glow.b, Glow.a * alpha);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI / 4f;
                GUI.Label(new Rect(r.x + Mathf.Cos(ang) * spread, r.y + Mathf.Sin(ang) * spread, r.width, r.height), spaced, style);
            }
            style.normal.textColor = new Color(Ink.r, Ink.g, Ink.b, alpha);
            GUI.Label(r, spaced, style);
            style.normal.textColor = old;
        }

        /// <summary>A quieter line of text (subtitles, hints).</summary>
        public static void Whisper(float row, string text, float alpha = 1f)
        {
            var style = Label;
            var old = style.normal.textColor;
            style.normal.textColor = new Color(InkDim.r, InkDim.g, InkDim.b, alpha);
            GUI.Label(Row(row, 0.9f, 1.2f), Spaced(text, 1), style);
            style.normal.textColor = old;
        }

        public static string Field(float row, string value, float widthOfScreen = 0.55f) =>
            GUI.TextField(Row(row, widthOfScreen, 1.15f), value ?? "", ButtonStyle);

        /// <summary>Darkens the whole screen behind a menu.</summary>
        public static void Dim(float alpha = 0.75f)
        {
            GUI.color = new Color(0f, 0.02f, 0.03f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            Vignette(0.8f);
        }

        /// <summary>Dark corners, so the eye goes to the middle.</summary>
        public static void Vignette(float strength = 1f)
        {
            vignette ??= MakeVignette();
            GUI.color = new Color(1f, 1f, 1f, strength);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignette, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }

        /// <summary>Black over everything, fading out over <paramref name="seconds"/> after <paramref name="since"/>.</summary>
        public static void FadeIn(float since, float seconds = 1.5f)
        {
            float a = 1f - Mathf.Clamp01((Time.unscaledTime - since) / seconds);
            if (a <= 0f) return;
            GUI.color = new Color(0f, 0f, 0f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static string Spaced(string text, int spaces)
        {
            if (string.IsNullOrEmpty(text) || spaces <= 0) return text;
            var sb = new System.Text.StringBuilder(text.Length * (spaces + 1));
            string gap = new string(' ', spaces);
            for (int i = 0; i < text.Length; i++)
            {
                sb.Append(text[i]);
                if (i < text.Length - 1 && text[i] != ' ' && text[i + 1] != ' ') sb.Append(gap);
                else if (text[i] == ' ') sb.Append(gap);
            }
            return sb.ToString();
        }

        private static Rect Row(float row, float widthOfScreen, float rows)
        {
            Rect safe = Safe;
            float w = safe.width * widthOfScreen;
            float x = LeftColumn ? safe.x + safe.width * 0.06f : safe.x + (safe.width - w) / 2f;
            float top = safe.y + (safe.height - 20f * Unit) / 2f; // the 20-row grid, centred vertically when width-limited
            return new Rect(x, top + row * Unit, w, rows * Unit);
        }

        private static GUIStyle Style(ref GUIStyle cache, float sizeOfScreen, FontStyle font, bool isButton)
        {
            if (cache == null)
            {
                panel ??= Solid(Panel);
                pressed ??= Solid(PanelPressed);
                cache = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = font, wordWrap = true, richText = false };
                cache.normal.textColor = cache.hover.textColor = cache.active.textColor = Ink;
                if (isButton)
                {
                    cache.normal.background = cache.hover.background = panel;
                    cache.active.background = pressed;
                    cache.active.textColor = Color.white;
                }
            }
            cache.fontSize = Mathf.RoundToInt(Base * sizeOfScreen);
            return cache;
        }

        private static Texture2D Solid(Color c)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeVignette()
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                px[y * size + x] = new Color(0f, 0.01f, 0.015f, Mathf.SmoothStep(0f, 0.85f, (d - 0.45f) / 0.9f));
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
