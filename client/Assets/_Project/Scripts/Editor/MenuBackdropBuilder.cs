using StillWater.Level;
using StillWater.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace StillWater.Editor
{
    /// <summary>
    /// Dresses the Menu scene as a dark poolroom seen from the edge of the water: tiled walls, a ceiling, deep water
    /// (PoolWater: waves, ripples and lamps come with it), a few glowing platforms out in the dark, flickering lamps,
    /// Look C's post-processing, and the Listener circling under the surface (MenuListener). The camera drifts over it
    /// all (MenuAtmosphere). It reuses the level art (Art/GreyBox materials, Listener.prefab, Look_C), so run Build Level
    /// and Build Listener Prefab first. Re-run any time: it replaces the "Backdrop" object and saves the scene.
    /// </summary>
    public static class MenuBackdropBuilder
    {
        private const string MenuScene = "Assets/_Project/Scenes/UI/Menu.unity";
        private const string Mats = "Assets/_Project/Art/GreyBox";
        private const float WaterY = 0f, Top = 0.55f, Floor = -3f, Ceiling = 9f, HalfWidth = 13f, Far = 40f;

        [MenuItem("Still Water/Art Spike/Build Menu Backdrop")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            var tile = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/Tile.mat");
            var wall = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/Wall.mat");
            var waterMat = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/Water.mat");
            var glow = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/Glow.mat");
            var lookC = AssetDatabase.LoadAssetAtPath<LookProfile>("Assets/_Project/Art/Looks/Look_C.asset");
            var listenerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Listener/Listener.prefab");
            if (tile == null || wall == null || waterMat == null || glow == null || lookC == null || listenerPrefab == null)
            {
                Debug.LogError("Build Menu Backdrop: level art missing (Art/GreyBox materials, Look_C, Listener.prefab). Run Build Level and Build Listener Prefab first.");
                return;
            }

            var old = GameObject.Find("Backdrop");
            if (old != null) Object.DestroyImmediate(old);
            var oldLook = GameObject.Find("Look");
            if (oldLook != null) Object.DestroyImmediate(oldLook);
            var root = new GameObject("Backdrop");

            // The pool: deep water from the near deck to the far wall, tiled all round.
            float midZ = Far / 2f;
            var water = Box("Water", root, new Vector3(0f, WaterY - 0.01f, midZ), new Vector3(2f * HalfWidth, 0.02f, Far), waterMat);
            water.AddComponent<PoolWater>();
            Box("PoolFloor", root, new Vector3(0f, Floor - 0.1f, midZ - 2f), new Vector3(2f * HalfWidth, 0.2f, Far + 4f), tile);
            Box("Deck", root, new Vector3(0f, (Floor + Top) / 2f, -2f), new Vector3(2f * HalfWidth, Top - Floor, 4f), tile);
            float wallY = (Floor + Ceiling) / 2f, wallH = Ceiling - Floor;
            Box("WallLeft", root, new Vector3(-HalfWidth - 0.25f, wallY, midZ - 2f), new Vector3(0.5f, wallH, Far + 4f), wall);
            Box("WallRight", root, new Vector3(HalfWidth + 0.25f, wallY, midZ - 2f), new Vector3(0.5f, wallH, Far + 4f), wall);
            Box("WallFar", root, new Vector3(0f, wallY, Far + 0.25f), new Vector3(2f * HalfWidth + 1f, wallH, 0.5f), wall);
            Box("WallBehind", root, new Vector3(0f, wallY, -4.25f), new Vector3(2f * HalfWidth + 1f, wallH, 0.5f), wall);
            Box("Ceiling", root, new Vector3(0f, Ceiling + 0.1f, midZ - 2f), new Vector3(2f * HalfWidth + 1f, 0.2f, Far + 5f), wall);

            // A few stepping platforms glowing out in the dark: what the game asks you to remember.
            var rng = new System.Random(7);
            foreach (var p in new[] { new Vector2(-2.5f, 4f), new Vector2(1.5f, 7f), new Vector2(-1f, 10.5f), new Vector2(3.5f, 13f),
                                      new Vector2(0.5f, 17f), new Vector2(-4f, 20f), new Vector2(2f, 24f), new Vector2(-1.5f, 29f) })
            {
                float size = 1.4f + (float)rng.NextDouble() * 0.5f;
                float h = Top - (WaterY - 0.3f);
                var plat = Box("Platform", root, new Vector3(p.x, Top - h / 2f, p.y), new Vector3(size, h, size), tile);
                plat.AddComponent<BoxCollider>(); // so the Listener dives under it (PlatformAvoidance)
                GlowStrips(plat, size, glow);
            }

            // Hanging lamps; two of them are dying.
            var lights = new GameObject("Lamps");
            lights.transform.SetParent(root.transform, false);
            var flicker = new System.Collections.Generic.List<Light>();
            int n = 0;
            foreach (var z in new[] { 3f, 13f, 25f, 36f })
            foreach (var x in new[] { -6f, 6f })
            {
                var go = new GameObject("HallLight");
                go.transform.SetParent(lights.transform, false);
                go.transform.position = new Vector3(x, Top + 3.1f, z);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
                l.range = 16f;
                if (n++ % 3 == 1) flicker.Add(l);
            }

            // Look C (fog, ambient, lamp colour, post-processing, headlamp), like a level.
            var look = new GameObject("Look");
            var volume = look.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = lookC.volume;
            var lookSwitch = look.AddComponent<LookSwitch>();
            var so = new SerializedObject(lookSwitch);
            var list = so.FindProperty("looks");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = lookC;
            so.ApplyModifiedPropertiesWithoutUndo();
            RenderSettings.fog = true; // saved on, so builds keep the fog shader variants
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.gameObject.SetActive(false);

            // The Listener, circling under the water.
            var listener = (GameObject)PrefabUtility.InstantiatePrefab(listenerPrefab, root.transform);
            listener.name = "MenuListener";
            var menuListener = listener.AddComponent<MenuListener>();
            var lso = new SerializedObject(menuListener);
            lso.FindProperty("centre").vector3Value = new Vector3(0f, 0f, 14f);
            lso.FindProperty("veinGlow").colorValue = lookC.listenerGlow;
            lso.ApplyModifiedPropertiesWithoutUndo();

            // Camera: standing at the edge of the deck, low, looking out over the water.
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.transform.SetPositionAndRotation(new Vector3(0f, Top + 1.25f, -1.2f), Quaternion.LookRotation(new Vector3(0f, -0.12f, 1f)));
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 80f;
            var atmosphere = root.AddComponent<MenuAtmosphere>();
            var aso = new SerializedObject(atmosphere);
            aso.FindProperty("cam").objectReferenceValue = cam.transform;
            aso.FindProperty("watch").objectReferenceValue = listener.transform;
            var fl = aso.FindProperty("flicker");
            fl.arraySize = flicker.Count;
            for (int i = 0; i < flicker.Count; i++) fl.GetArrayElementAtIndex(i).objectReferenceValue = flicker[i];
            aso.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log("Menu backdrop built and the Menu scene saved. Press Play in the Menu scene.");
        }

        private static GameObject Box(string name, GameObject parent, Vector3 center, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>()); // nothing walks here
            return go;
        }

        private static void GlowStrips(GameObject platform, float size, Material glow)
        {
            Vector3 c = platform.transform.position;
            float y = Top + 0.005f, half = size / 2f - 0.04f;
            foreach (var (offset, scale) in new[]
                     {
                         (new Vector3(0f, 0f, half), new Vector3(size, 0.02f, 0.08f)), (new Vector3(0f, 0f, -half), new Vector3(size, 0.02f, 0.08f)),
                         (new Vector3(half, 0f, 0f), new Vector3(0.08f, 0.02f, size)), (new Vector3(-half, 0f, 0f), new Vector3(0.08f, 0.02f, size)),
                     })
            {
                // Made beside the platform, then parented keeping its world size (the platform itself is scaled).
                var strip = Box("Glow", platform.transform.parent.gameObject, new Vector3(c.x, y, c.z) + offset, scale, glow);
                strip.transform.SetParent(platform.transform, true);
            }
        }
    }
}
