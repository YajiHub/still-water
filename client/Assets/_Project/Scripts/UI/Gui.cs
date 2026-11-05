using UnityEngine;

namespace StillWater.UI
{
    /// <summary>
    /// Phone-sized IMGUI helpers for the grey-box screens: flat dark-teal style, sizes scale with screen height.
    /// </summary>
    // shortcut: IMGUI screens for the prototype; rebuild in UI Toolkit with real art in Phase 5 polish.
    public static class Gui
    {
        private static readonly Color Ink = new Color(0.85f, 0.95f, 0.95f);
        private static readonly Color Panel = new Color(0.06f, 0.14f, 0.16f, 0.92f);
        private static readonly Color PanelPressed = new Color(0.16f, 0.32f, 0.35f, 0.95f);
        private static readonly Color Edge = new Color(0.35f, 0.75f, 0.8f, 0.6f);

        private static GUIStyle label, title, button;
        private static Texture2D panel, pressed;

        public static float Unit => Screen.height / 20f; // one "row" of UI

        public static GUIStyle Label => Style(ref label, 0.042f, FontStyle.Normal, false);
        public static GUIStyle Title => Style(ref title, 0.1f, FontStyle.Bold, false);
        public static GUIStyle ButtonStyle => Style(ref button, 0.045f, FontStyle.Bold, true);

        /// <summary>A button centred horizontally at row <paramref name="row"/> (0 = top, 20 rows per screen).</summary>
        public static bool Button(float row, string text, float widthOfScreen = 0.4f)
        {
            Rect r = Row(row, widthOfScreen, 1.6f);
            // A thin glowing edge, like the platform strips.
            GUI.color = GUI.enabled ? Edge : new Color(Edge.r, Edge.g, Edge.b, 0.15f);
            GUI.DrawTexture(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            return GUI.Button(r, text, ButtonStyle);
        }

        public static void Text(float row, string text, GUIStyle style = null, float height = 1.2f) =>
            GUI.Label(Row(row, 0.9f, height), text, style ?? Label);

        /// <summary>Darkens the whole screen behind a menu.</summary>
        public static void Dim(float alpha = 0.75f)
        {
            GUI.color = new Color(0f, 0.02f, 0.03f, alpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static Rect Row(float row, float widthOfScreen, float rows)
        {
            float w = Screen.width * widthOfScreen;
            return new Rect((Screen.width - w) / 2f, row * Unit, w, rows * Unit);
        }

        private static GUIStyle Style(ref GUIStyle cache, float sizeOfScreen, FontStyle font, bool isButton)
        {
            if (cache == null)
            {
                panel ??= Solid(Panel);
                pressed ??= Solid(PanelPressed);
                cache = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = font, wordWrap = true };
                cache.normal.textColor = cache.hover.textColor = cache.active.textColor = Ink;
                if (isButton)
                {
                    cache.normal.background = cache.hover.background = panel;
                    cache.active.background = pressed;
                }
            }
            cache.fontSize = Mathf.RoundToInt(Screen.height * sizeOfScreen);
            return cache;
        }

        private static Texture2D Solid(Color c)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }
    }
}
