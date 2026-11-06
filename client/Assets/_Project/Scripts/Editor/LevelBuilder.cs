using System.IO;
using StarterAssets;
using System.Collections.Generic;
using StillWater.Level;
using StillWater.Monster;
using StillWater.Player;
using StillWater.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StillWater.Editor
{
    /// <summary>
    /// Decision 16: the grey-box level is built from server/src/levels/*.json, never placed by hand.
    /// Still Water → Build Level replaces the "Level" object in the open scene.
    /// Still Water → Create Player Rig adds the player, camera rigs and touch controls.
    /// Both are undoable (Ctrl+Z). Save the scene afterwards.
    /// </summary>
    public static class LevelBuilder
    {
        private const string LevelsSourceDir = "../server/src/levels";
        private const string LevelsCopyDir = "Assets/_Project/Configs/Levels";
        private const string MaterialsDir = "Assets/_Project/Art/GreyBox";
        private const string MovementConfigPath = "Assets/_Project/Configs/MovementConfig.asset";
        private const string LooksDir = "Assets/_Project/Art/Looks";
        private const string CatalogPath = "Assets/_Project/Resources/LevelCatalog.asset";
        private const string MenuScenePath = "Assets/_Project/Scenes/UI/Menu.unity";
        private const string LevelScenesDir = "Assets/_Project/Scenes/Levels";

        // Grey-box geometry only (looks, not gameplay): pool depth, wall height, slab thickness.
        private const float PoolDepth = 3f, WallHeight = 10f, LightHeight = 3.1f, WallThickness = 0.5f, SlabBelowWater = 0.3f, GlowWidth = 0.08f, LampSpacing = 12f, MaxLamps = 32f;

        [MenuItem("Still Water/Build Level...")]
        private static void BuildLevelMenu()
        {
            string path = EditorUtility.OpenFilePanel("Level layout", Path.GetFullPath(LevelsSourceDir), "json");
            if (!string.IsNullOrEmpty(path)) BuildLevel(path);
        }

        public static void BuildLevel(string jsonPath)
        {
            if (!InLevelScene("Build Level")) return;
            var layout = LevelLayout.FromJson(File.ReadAllText(jsonPath));
            var config = GetOrCreateMovementConfig();

            var old = GameObject.Find("Level");
            if (old != null) Undo.DestroyObjectImmediate(old);
            var root = new GameObject("Level");
            Undo.RegisterCreatedObjectUndo(root, "Build Level");

            var tile = GetMaterial("Tile", new Color(0.78f, 0.80f, 0.78f), 0.4f);
            var wall = GetMaterial("Wall", new Color(0.62f, 0.68f, 0.66f), 0.5f);
            var waterMat = GetMaterial("Water", new Color(0.05f, 0.22f, 0.26f), 0.95f);
            // Faint on purpose: below the bloom threshold, so the Phase 2 pattern reveal can be the bright one.
            var glow = GetMaterial("Glow", new Color(0.6f, 0.9f, 1f), 0.5f, new Color(0.6f, 0.9f, 1f) * 0.7f);
            var door = GetMaterial("Door", new Color(0.12f, 0.10f, 0.09f), 0.2f);
            var ferryGlow = GetMaterial("GlowFerry", new Color(1f, 0.75f, 0.4f), 0.5f, new Color(1f, 0.7f, 0.35f) * 0.7f); // warm: "this one moves"

            var w = layout.water;
            float top = layout.platformTopY;
            float floorY = w.y - PoolDepth;
            float minZ = layout.start.z - layout.start.depth / 2f;
            float maxZ = layout.exit.z + layout.exit.depth / 2f;
            float midX = (w.minX + w.maxX) / 2f, widthX = w.maxX - w.minX;

            // Water: a thin slab with no collider (you fall through it and swim).
            var water = Box("Water", root, new Vector3(midX, w.y - 0.01f, (w.minZ + w.maxZ) / 2f), new Vector3(widthX, 0.02f, w.maxZ - w.minZ), waterMat, false);
            water.AddComponent<PoolWater>();

            Box("PoolFloor", root, new Vector3(midX, floorY - 0.1f, (minZ + maxZ) / 2f), new Vector3(widthX, 0.2f, maxZ - minZ), tile, true);
            float wallY = (floorY + top + WallHeight) / 2f, wallH = top + WallHeight - floorY;
            Box("WallLeft", root, new Vector3(w.minX - WallThickness / 2f, wallY, (minZ + maxZ) / 2f), new Vector3(WallThickness, wallH, maxZ - minZ), wall, true);
            Box("WallRight", root, new Vector3(w.maxX + WallThickness / 2f, wallY, (minZ + maxZ) / 2f), new Vector3(WallThickness, wallH, maxZ - minZ), wall, true);
            Box("WallBack", root, new Vector3(midX, wallY, minZ - WallThickness / 2f), new Vector3(widthX + 2f * WallThickness, wallH, WallThickness), wall, true);
            // Indoors: a ceiling, so the hall is lit only by its own lights (the Art Style looks set their colour).
            Box("Ceiling", root, new Vector3(midX, top + WallHeight + 0.1f, (minZ + maxZ) / 2f), new Vector3(widthX + 2f * WallThickness, 0.2f, maxZ - minZ + 2f * WallThickness), wall, true);
            var lights = new GameObject("HallLights");
            lights.transform.SetParent(root.transform, false);
            // A lamp about every LampSpacing metres, so big halls have no black zones. Needs the Forward+ renderer:
            // plain Forward lights each object (the one big water plane too) with only 4 extra lights.
            // Huge halls space them out instead (about MaxLamps in total), so phones keep their fps.
            float spacing = Mathf.Max(LampSpacing, Mathf.Sqrt(widthX * (maxZ - minZ) / MaxLamps));
            int lampsX = Mathf.Max(2, Mathf.RoundToInt(widthX / spacing));
            int lampsZ = Mathf.Max(2, Mathf.RoundToInt((maxZ - minZ) / spacing));
            for (int ix = 0; ix < lampsX; ix++)
            for (int iz = 0; iz < lampsZ; iz++)
            {
                var go = new GameObject("HallLight");
                go.transform.SetParent(lights.transform, false);
                go.transform.position = new Vector3(w.minX + widthX * (ix + 0.5f) / lampsX, top + LightHeight, minZ + (maxZ - minZ) * (iz + 0.5f) / lampsZ); // hanging lamps; the tall hall leaves room for the Reveal camera
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
                l.range = 16f;
            }

            Box("WallFront", root, new Vector3(midX, wallY, maxZ + WallThickness / 2f), new Vector3(widthX + 2f * WallThickness, wallH, WallThickness), wall, true);

            // Start deck: solid down to the floor, and climbable from the water.
            var start = Deck("Start", root, layout.start, floorY, top, tile, config.edgeForgiveness);
            SetFields(start.AddComponent<Platform>(), ("id", 0), ("climbable", true));

            // Exit deck: raised above the platforms and marked not climbable, so you must jump onto it (edge case 5).
            var exit = Deck("Exit", root, layout.exit, floorY, layout.exitTopY, tile, 0f);
            SetFields(exit.AddComponent<Platform>(), ("id", -1), ("climbable", false));
            Box("ExitDoor", exit, new Vector3(layout.exit.x, layout.exitTopY + 1.1f, maxZ - 0.15f), new Vector3(1.2f, 2.2f, 0.3f), door, false).AddComponent<ExitDoor>();

            foreach (var spot in layout.platforms)
            {
                float h = top - (w.y - SlabBelowWater);
                var p = Box($"Platform_{spot.id:00}", root, new Vector3(spot.x, top - h / 2f, spot.z), new Vector3(spot.size, h, spot.size), tile, true);
                Widen(p, spot.size, spot.size, config.edgeForgiveness);
                SetFields(p.AddComponent<Platform>(), ("id", spot.id), ("climbable", true), ("onSafePath", System.Array.IndexOf(layout.safePath, spot.id) >= 0));
                AddGlowStrips(p, spot.size, top, spot.Moves ? ferryGlow : glow);
                if (spot.Moves)
                    SetFields(p.AddComponent<PlatformMover>(), ("pointA", p.transform.position), ("travel", new Vector3(spot.move.dx, 0f, spot.move.dz)),
                        ("seconds", spot.move.seconds), ("pause", spot.move.pause), ("offset", spot.move.offset));
            }

            CopyLayoutIntoClient(jsonPath);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log($"Built level '{layout.name}' with {layout.platforms.Length} platforms. Save the scene (Ctrl+S).");
        }

        // Hand-edited a level in the scene (moved, copied or resized platforms)? Write it back to the server's JSON, which
        // stays the one source of truth. Then run `npm run level:route -- --id levelNN` (sets the route, server tests
        // check it) and Build Level again from the JSON.
        [MenuItem("Still Water/Export Level to JSON")]
        private static void ExportLevel()
        {
            if (!InLevelScene("Export Level")) return;
            string id = "level" + EditorSceneManager.GetActiveScene().name.Replace("Level", "");
            string path = Path.Combine(Path.GetFullPath(LevelsSourceDir), id + ".json");
            if (!File.Exists(path))
            {
                Debug.LogError($"No {path}: export only works for a level built from a JSON.");
                return;
            }
            var layout = LevelLayout.FromJson(File.ReadAllText(path));
            var original = new Dictionary<int, PlatformSpot>();
            int nextId = 1;
            foreach (var spot in layout.platforms)
            {
                original[spot.id] = spot;
                nextId = Mathf.Max(nextId, spot.id + 1);
            }

            // The copy nearest its JSON spot keeps the id; other copies (Ctrl+D) get new ids.
            var scenePlatforms = new List<Platform>();
            foreach (var p in Object.FindObjectsByType<Platform>(FindObjectsSortMode.None))
                if (p.Id > 0) scenePlatforms.Add(p); // 0 = start deck, -1 = exit deck
            float FromOriginal(Platform p) => original.TryGetValue(p.Id, out var s)
                ? Vector2.Distance(new Vector2(p.transform.position.x, p.transform.position.z), new Vector2(s.x, s.z)) : 0f;
            scenePlatforms.Sort((a, b) => FromOriginal(a).CompareTo(FromOriginal(b)));

            var used = new HashSet<int>();
            var spots = new List<PlatformSpot>();
            int copies = 0;
            foreach (var p in scenePlatforms)
            {
                var t = p.transform;
                if (!Mathf.Approximately(t.localScale.x, t.localScale.z))
                    Debug.LogWarning($"{p.name} is not square ({t.localScale.x} × {t.localScale.z}): exported as {t.localScale.x} m.", p);
                int newId = used.Add(p.Id) ? p.Id : nextId++;
                if (newId != p.Id) copies++;
                original.TryGetValue(p.Id, out var src);
                spots.Add(new PlatformSpot
                {
                    id = newId,
                    x = Round(t.position.x),
                    z = Round(t.position.z),
                    size = Round(t.localScale.x),
                    move = p.GetComponent<PlatformMover>() != null && src != null && src.Moves ? src.move : null,
                });
            }
            layout.platforms = spots.ToArray();
            File.WriteAllText(path, JsonUtility.ToJson(layout, true));
            Debug.Log($"Exported {spots.Count} platforms ({copies} copies got new ids) to {path}. Next: npm run level:route -- --id {id}, then Build Level from the JSON.");
        }

        private static float Round(float v) => Mathf.Round(v * 20f) / 20f; // 5 cm grid, like the generator

        [MenuItem("Still Water/Create Player Rig")]
        private static void CreatePlayerRig()
        {
            if (!InLevelScene("Create Player Rig")) return;
            var config = GetOrCreateMovementConfig();
            var old = GameObject.Find("Player");
            if (old != null) Undo.DestroyObjectImmediate(old);

            var player = new GameObject("Player");
            Undo.RegisterCreatedObjectUndo(player, "Create Player Rig");
            var startDeck = GameObject.Find("Level/Start");
            player.transform.position = startDeck != null
                ? new Vector3(startDeck.transform.position.x, startDeck.GetComponent<Collider>().bounds.max.y + 0.05f, startDeck.transform.position.z)
                : Vector3.zero;

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;

            var input = player.AddComponent<StarterAssetsInputs>();
            SetFields(input, ("cursorLocked", false), ("cursorInputForLook", false));

            // Body: a capsule plus a "nose" so you can see which way it faces in third person.
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = GetMaterial("Player", new Color(0.85f, 0.55f, 0.25f), 0.3f);
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 1.5f, 0.35f);
            nose.transform.localScale = new Vector3(0.2f, 0.12f, 0.2f);

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "BlobShadow";
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.transform.SetParent(player.transform, false);
            shadow.transform.localScale = new Vector3(0.6f, 0.005f, 0.6f);
            var shadowRenderer = shadow.GetComponent<Renderer>();
            shadowRenderer.sharedMaterial = GetMaterial("BlobShadow", new Color(0.02f, 0.02f, 0.02f), 0f);
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var water = Object.FindFirstObjectByType<PoolWater>();
            var motor = player.AddComponent<PlayerMotor>();
            SetFields(motor, ("config", config), ("input", input), ("water", water), ("blobShadow", shadow.transform));

            // Camera: reuse the scene's Main Camera, or make one.
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                Undo.RegisterCreatedObjectUndo(camGo, "Create Player Rig");
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            Undo.RecordObject(cam, "Create Player Rig");
            cam.nearClipPlane = 0.05f;
            var first = GetOrAdd<FirstPersonLook>(cam.gameObject);
            var third = GetOrAdd<ThirdPersonLook>(cam.gameObject);
            var camSwitch = GetOrAdd<CameraModeSwitch>(cam.gameObject);
            SetFields(first, ("motor", motor), ("input", input), ("config", config));
            SetFields(third, ("motor", motor), ("input", input), ("config", config));
            SetFields(camSwitch, ("firstPerson", first), ("thirdPerson", third));
            third.enabled = false;

            var touch = player.AddComponent<TouchControls>();
            SetFields(touch, ("input", input), ("motor", motor), ("config", config), ("cameraSwitch", camSwitch));

            if (water == null) Debug.LogWarning("No PoolWater in the scene: run Still Water → Build Level first, then Create Player Rig again.");
            EditorSceneManager.MarkSceneDirty(player.scene);
            Selection.activeGameObject = player;
            Debug.Log("Player rig created. Press Play: WASD/mouse in the Editor, C switches camera. Save the scene (Ctrl+S).");
        }

        [MenuItem("Still Water/Art Spike/Set Up Looks")]
        private static void SetUpLooks()
        {
            var a = GetOrCreateLook("A", "Clean liminal", profile =>
            {
                profile.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
                var bloom = profile.Add<Bloom>();
                bloom.threshold.Override(0.9f);
                bloom.intensity.Override(0.6f);
                var color = profile.Add<ColorAdjustments>();
                color.postExposure.Override(0.3f);
                color.saturation.Override(-10f);
                profile.Add<Vignette>().intensity.Override(0.2f);
            }, look =>
            {
                look.fogColor = new Color(0.60f, 0.70f, 0.72f);
                look.fogDensity = 0.025f;
                look.ambientColor = new Color(0.42f, 0.47f, 0.50f);
                look.reflectionIntensity = 0.6f;
                look.lightColor = new Color(1f, 0.96f, 0.88f);
                look.lightIntensity = 3f;
                look.lightRange = 20f;
                look.renderScale = 0.8f;
            });
            var c = GetOrCreateLook("C", "Hybrid horror", profile =>
            {
                profile.Add<Tonemapping>().mode.Override(TonemappingMode.ACES);
                var bloom = profile.Add<Bloom>();
                bloom.threshold.Override(0.8f);
                bloom.intensity.Override(0.8f);
                var color = profile.Add<ColorAdjustments>();
                color.postExposure.Override(0.3f);
                color.contrast.Override(15f);
                color.saturation.Override(-30f);
                profile.Add<Vignette>().intensity.Override(0.4f);
                var grain = profile.Add<FilmGrain>();
                grain.type.Override(FilmGrainLookup.Medium1);
                grain.intensity.Override(0.35f);
            }, look =>
            {
                // Tuned after the first phone test: the original was too dark to read the walls and water.
                look.fogColor = new Color(0.04f, 0.08f, 0.09f);
                look.fogDensity = 0.045f;
                look.ambientColor = new Color(0.13f, 0.16f, 0.18f);
                look.reflectionIntensity = 0.3f;
                look.lightColor = new Color(0.75f, 0.90f, 1f);
                look.renderScale = 0.6f;
                DarkLookReadability(look);
            });
            // Look_C made before the headlamp existed: give it the readability values once.
            if (c.headlampIntensity <= 0f)
            {
                DarkLookReadability(c);
                EditorUtility.SetDirty(c);
                AssetDatabase.SaveAssets();
            }

            var lookGo = GameObject.Find("Look");
            if (lookGo == null)
            {
                lookGo = new GameObject("Look");
                Undo.RegisterCreatedObjectUndo(lookGo, "Set Up Looks");
            }
            var volume = GetOrAdd<Volume>(lookGo);
            volume.isGlobal = true;
            volume.sharedProfile = a.volume;
            var lookSwitch = GetOrAdd<LookSwitch>(lookGo);
            var so = new SerializedObject(lookSwitch);
            var list = so.FindProperty("looks");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = a;
            list.GetArrayElementAtIndex(1).objectReferenceValue = c;
            so.ApplyModifiedPropertiesWithoutUndo();

            // The ceiling blocks the sun anyway; switching it off saves its shadow pass.
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                Undo.RecordObject(l.gameObject, "Set Up Looks");
                l.gameObject.SetActive(false);
            }

            // Saved with fog on so the build keeps the fog shader variants (Unity strips fog a scene never uses).
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;

            var touch = Object.FindFirstObjectByType<TouchControls>();
            if (touch != null) SetFields(touch, ("lookSwitch", lookSwitch));
            else Debug.LogWarning("No TouchControls in the scene: run Create Player Rig, then Set Up Looks again.");

            EditorSceneManager.MarkSceneDirty(lookGo.scene);
            Debug.Log("Looks A and C set up. Play: L (or the 'Look' box) switches. Tune them in Assets/_Project/Art/Looks. Save the scene (Ctrl+S).");
        }

        [MenuItem("Still Water/Set Up Level Loop")]
        private static void SetUpLevelLoop()
        {
            if (!InLevelScene("Set Up Level Loop")) return;
            var scene = EditorSceneManager.GetActiveScene();
            string number = scene.name.Replace("Level", "");                 // "Level01" → "01"
            string configPath = $"{LevelsCopyDir}/LevelConfig_{number}.asset";
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<LevelConfig>();
                config.id = $"level{number}";
                config.sceneName = scene.name;
                config.layout = AssetDatabase.LoadAssetAtPath<TextAsset>($"{LevelsCopyDir}/level{number}.json");
                if (config.layout == null) Debug.LogError($"No {LevelsCopyDir}/level{number}.json: run Build Level first.");
                AssetDatabase.CreateAsset(config, configPath);
            }

            EnsureFolder(Path.GetDirectoryName(CatalogPath).Replace('\\', '/'));
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalog>();
                catalog.levels = new LevelConfig[0];
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            var list = new List<LevelConfig>(catalog.levels);
            int before = list.Count;
            list.RemoveAll(l => l == null || l.sceneName == "Menu"); // clean deleted or wrong entries
            if (!list.Contains(config) || list.Count != before)
            {
                if (!list.Contains(config)) list.Add(config);
                list.Sort((a, b) => string.CompareOrdinal(a.id, b.id)); // level01, level02, … = play order
                catalog.levels = list.ToArray();
                EditorUtility.SetDirty(catalog);
            }
            AssetDatabase.SaveAssets();

            var brightGlow = GetMaterial("GlowBright", new Color(0.6f, 0.9f, 1f), 0.5f, new Color(0.6f, 0.9f, 1f) * 4f);
            var controllerGo = GameObject.Find("LevelController");
            if (controllerGo == null)
            {
                controllerGo = new GameObject("LevelController");
                Undo.RegisterCreatedObjectUndo(controllerGo, "Set Up Level Loop");
            }
            var controller = GetOrAdd<LevelController>(controllerGo);
            SetFields(controller, ("config", config));
            SetFields(GetOrAdd<PatternRevealer>(controllerGo), ("brightGlow", brightGlow));
            SetFields(GetOrAdd<LevelHud>(controllerGo), ("level", controller));

            // The Listener: a dark, long shape with a ridge, mostly under the surface (grey box until Phase 5 art).
            var old = GameObject.Find("Listener");
            if (old != null) Undo.DestroyObjectImmediate(old);
            var listener = new GameObject("Listener");
            Undo.RegisterCreatedObjectUndo(listener, "Set Up Level Loop");
            var body = new GameObject("Body");
            body.transform.SetParent(listener.transform, false);
            var dark = GetMaterial("ListenerPale", new Color(0.78f, 0.80f, 0.77f), 0.85f); // pale and wet (DESIGN §5.1), readable in the dark look
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(torso.GetComponent<Collider>());
            torso.transform.SetParent(body.transform, false);
            torso.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            torso.transform.localScale = new Vector3(0.8f, 1.3f, 0.5f);
            torso.GetComponent<Renderer>().sharedMaterial = dark;
            var ridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(ridge.GetComponent<Collider>());
            ridge.transform.SetParent(body.transform, false);
            ridge.transform.localPosition = new Vector3(0f, 0.35f, 0.2f);
            ridge.transform.localScale = new Vector3(0.08f, 0.45f, 1.2f);
            ridge.GetComponent<Renderer>().sharedMaterial = dark;
            SetFields(listener.AddComponent<ListenerAI>(), ("body", body));

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = config;
            Debug.Log($"Level loop set up for {scene.name} (config {configPath}). Save the scene, then run Update Build List (or Create Menu Scene if there is no Menu yet).");
        }

        [MenuItem("Still Water/Create Menu Scene")]
        private static void CreateMenuScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.05f, 0.06f);
            new GameObject("Menu").AddComponent<MainMenu>();
            EnsureFolder(Path.GetDirectoryName(MenuScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            UpdateBuildList();
        }

        // Run after saving a new level scene. Leaves the Menu scene itself alone.
        [MenuItem("Still Water/Update Build List")]
        private static void UpdateBuildList()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) == null)
            {
                Debug.LogError($"No {MenuScenePath}: run Create Menu Scene first.");
                return;
            }
            // Menu first (the app opens on it), then every level scene in name order (Level01, Level02, …).
            var levels = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { LevelScenesDir }))
                levels.Add(AssetDatabase.GUIDToAssetPath(guid));
            levels.Sort(string.CompareOrdinal);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(MenuScenePath, true) };
            foreach (string path in levels) scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"Build list = Menu + {levels.Count} level scene(s): {string.Join(", ", levels.ConvertAll(p => Path.GetFileNameWithoutExtension(p)))}.");
        }

        // Existing assets are reused, so your tuning in the Inspector survives running this again.
        private static LookProfile GetOrCreateLook(string label, string title, System.Action<VolumeProfile> fillVolume, System.Action<LookProfile> fillLook)
        {
            EnsureFolder(LooksDir);
            string lookPath = $"{LooksDir}/Look_{label}.asset";
            var look = AssetDatabase.LoadAssetAtPath<LookProfile>(lookPath);
            if (look != null) return look;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, $"{LooksDir}/Volume_{label}.asset");
            fillVolume(profile);
            foreach (var component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile); // overrides live inside the profile file
            }

            look = ScriptableObject.CreateInstance<LookProfile>();
            look.label = $"{label} · {title}";
            look.volume = profile;
            fillLook(look);
            AssetDatabase.CreateAsset(look, lookPath);
            AssetDatabase.SaveAssets();
            return look;
        }

        // Readability in the dark look (Phase 3): more, smaller lamp pools (the grid in Build Level), a headlamp where
        // you look, and a faint glow on the Listener. All tunable on Look_C afterwards.
        private static void DarkLookReadability(LookProfile look)
        {
            look.lightIntensity = 2.5f;
            look.lightRange = 13f;
            look.headlampIntensity = 4f;
            look.headlampColor = new Color(0.8f, 0.92f, 1f);
            look.headlampRange = 16f;
            look.headlampAngle = 60f;
            look.listenerGlow = new Color(0.30f, 0.36f, 0.38f);
        }

        // ---------- helpers ----------

        // The level menus only make sense in a level scene; running them in the Menu scene once filled it with a pool.
        private static bool InLevelScene(string action)
        {
            string path = EditorSceneManager.GetActiveScene().path;
            if (path.StartsWith(LevelScenesDir + "/")) return true;
            EditorUtility.DisplayDialog("Still Water", $"{action} only works in a level scene ({LevelScenesDir}/LevelNN.unity).\nOpen Level01 first.", "OK");
            return false;
        }

        private static GameObject Box(string name, GameObject parent, Vector3 center, Vector3 size, Material mat, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = center;
            go.transform.localScale = size;
            go.transform.SetParent(parent.transform, true); // keeps the world size even under a scaled parent
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Deck(string name, GameObject parent, RectArea r, float floorY, float top, Material mat, float forgiveness)
        {
            float h = top - floorY;
            var go = Box(name, parent, new Vector3(r.x, floorY + h / 2f, r.z), new Vector3(r.width, h, r.depth), mat, true);
            Widen(go, r.width, r.depth, forgiveness);
            return go;
        }

        // Edge forgiveness: the collider sticks out past the visual by `extra` metres per side.
        private static void Widen(GameObject go, float width, float depth, float extra)
        {
            var box = go.GetComponent<BoxCollider>();
            box.size = new Vector3((width + 2f * extra) / width, 1f, (depth + 2f * extra) / depth);
        }

        private static void AddGlowStrips(GameObject platform, float size, float top, Material glow)
        {
            Vector3 c = platform.transform.position;
            float half = size / 2f - GlowWidth / 2f;
            var strips = new[]
            {
                (new Vector3(0f, 0f, half), new Vector3(size, 0.02f, GlowWidth)),
                (new Vector3(0f, 0f, -half), new Vector3(size, 0.02f, GlowWidth)),
                (new Vector3(half, 0f, 0f), new Vector3(GlowWidth, 0.02f, size)),
                (new Vector3(-half, 0f, 0f), new Vector3(GlowWidth, 0.02f, size)),
            };
            foreach (var (offset, scale) in strips)
                Box("Glow", platform, new Vector3(c.x, top + 0.01f, c.z) + offset, scale, glow, false);
        }

        private static Material GetMaterial(string name, Color color, float smoothness, Color? emission = null)
        {
            EnsureFolder(MaterialsDir);
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static MovementConfig GetOrCreateMovementConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<MovementConfig>(MovementConfigPath);
            if (config != null) return config;
            EnsureFolder(Path.GetDirectoryName(MovementConfigPath).Replace('\\', '/'));
            config = ScriptableObject.CreateInstance<MovementConfig>();
            AssetDatabase.CreateAsset(config, MovementConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        // The phone can't read ../server, so the layout ships inside the client as a TextAsset.
        private static void CopyLayoutIntoClient(string jsonPath)
        {
            EnsureFolder(LevelsCopyDir);
            string target = $"{LevelsCopyDir}/{Path.GetFileName(jsonPath)}";
            File.Copy(jsonPath, target, true);
            AssetDatabase.ImportAsset(target);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(assetPath));
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component =>
            go.TryGetComponent<T>(out var existing) ? existing : Undo.AddComponent<T>(go);

        // Sets [SerializeField] private fields by name, the same way the Inspector does (so it's saved and undoable).
        private static void SetFields(Object target, params (string field, object value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in values)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'");
                    continue;
                }
                switch (value)
                {
                    case bool b: prop.boolValue = b; break;
                    case int i: prop.intValue = i; break;
                    case float f: prop.floatValue = f; break;
                    case Vector3 v: prop.vector3Value = v; break;
                    case Object o: prop.objectReferenceValue = o; break;
                    case null: prop.objectReferenceValue = null; break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
