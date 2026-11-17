using System.Linq;
using StillWater.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StillWater.Editor
{
    /// <summary>
    /// Builds Resources/Player/PlayerBody_Female.prefab and PlayerBody_Male.prefab from Art/Player/Player*_FullBody.fbx:
    /// the Quaternius bodies (CC0) dressed for the poolrooms by art/player/build_player_bodies.py, with a hairstyle merged
    /// onto their skeleton (re-binding hair at runtime left the player bald) and a "Clothes" mesh. Each gets its baked
    /// pale skin and clothes textures, brown eyes, dark wet hair, and an Animator with Player.controller. The clips are the Universal Animation Library ones in
    /// Art/Listener, retargeted through the Humanoid avatar. PlayerAvatar loads the chosen prefab at runtime.
    /// Re-run it any time: it overwrites the prefabs, the controller and the materials.
    /// </summary>
    public static class PlayerBodyBuilder
    {
        private const string Dir = "Assets/_Project/Art/Player";
        private const string OutDir = "Assets/_Project/Resources/Player";
        private static readonly string[] Libraries =
            { "Assets/_Project/Art/Listener/UAL1_Standard.fbx", "Assets/_Project/Art/Listener/UAL2_Standard.fbx" };

        private struct Body
        {
            public string id, fbx, normal, hairTexture;
            public Color hairTint;
        }

        // The horror look (2026-11-17): skin and clothes textures are baked by build_player_bodies.py
        // (T_Player<id>_Skin / _Clothes / _Clothes_Normal, smoothness in the alpha). Hair is tinted dark and wet.
        private static readonly Body[] Bodies =
        {
            new Body { id = "Female", fbx = "PlayerFemale_FullBody", normal = "T_Superhero_Female_Normal",
                       hairTexture = "T_Hair_2_BaseColor", hairTint = new Color(0.42f, 0.36f, 0.33f) },
            new Body { id = "Male", fbx = "PlayerMale_FullBody", normal = "T_Superhero_Male_Normal",
                       hairTexture = "T_Hair_1_BaseColor", hairTint = new Color(0.36f, 0.33f, 0.31f) },
        };

        [MenuItem("Still Water/Art Spike/Build Player Bodies")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/_Project/Resources", "Player");
            var controller = Controller();
            var eyes = Material("PlayerEyes", Tex("T_Eye_Brown"), null, 0.9f);
            foreach (var b in Bodies)
            {
                string path = $"{Dir}/{b.fbx}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                if (model == null || avatar == null || !avatar.isHuman)
                {
                    Debug.LogError($"Player: {path} missing or not imported as Humanoid (see PlayerImport).");
                    continue;
                }
                var skin = Material($"PlayerSkin_{b.id}", Tex($"T_Player{b.id}_Skin"), Tex(b.normal), 1f, alphaSmooth: true);
                var clothes = Material($"PlayerClothes_{b.id}", Tex($"T_Player{b.id}_Clothes"), Tex($"T_Player{b.id}_Clothes_Normal"),
                                       1f, alphaSmooth: true, twoSided: true); // torn sleeves and hems: see the inside
                var hair = Material($"PlayerHair_{b.id}", Tex(b.hairTexture), null, 0.7f, tint: b.hairTint); // wet

                var go = Object.Instantiate(model);
                go.name = $"PlayerBody_{b.id}";
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    // Eyes and Eyebrows have two slots (MI_Eyes, MI_Hair_n); the body, hair and clothes have one.
                    bool face = r.name.StartsWith("Eye");
                    bool isHair = r.name == "Hair";
                    bool isClothes = r.name == "Clothes";
                    r.sharedMaterials = r.sharedMaterials
                        .Select((_, i) => isClothes ? clothes : isHair ? hair : face ? (i == 0 ? eyes : hair) : skin).ToArray();
                }

                var animator = go.GetComponent<Animator>();
                if (animator == null) animator = go.AddComponent<Animator>(); // not ??: Unity's fake null
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false; // PlayerMotor moves it
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                PrefabUtility.SaveAsPrefabAsset(go, $"{OutDir}/{go.name}.prefab");
                Object.DestroyImmediate(go);
                Debug.Log($"Built {OutDir}/PlayerBody_{b.id}.prefab");
            }
        }

        private static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/{name}.png");

        /// <param name="smoothness">The smoothness, or with alphaSmooth the scale on the map's alpha (wet = smooth).</param>
        private static Material Material(string name, Texture2D baseMap, Texture2D normal, float smoothness,
                                         bool alphaSmooth = false, bool twoSided = false, Color? tint = null)
        {
            string path = $"{Dir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            if (baseMap == null) Debug.LogError($"Player: texture for {name} missing: run art/player/build_player_bodies.py (see its header).");
            mat.SetColor("_BaseColor", tint ?? Color.white);
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_SmoothnessTextureChannel", alphaSmooth ? 1f : 0f);
            if (alphaSmooth) mat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            else mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            mat.SetFloat("_Cull", twoSided ? 0f : 2f); // 0 = both faces, 2 = back faces culled (URP Lit's Render Face)
            mat.doubleSidedGI = twoSided;
            mat.SetTexture("_BumpMap", normal);
            if (normal != null) mat.EnableKeyword("_NORMALMAP");
            else mat.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static AnimatorController Controller()
        {
            var all = Libraries.SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();
            string path = $"{Dir}/Player.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null || controller.layers.Length == 0)
            {
                if (controller != null) AssetDatabase.DeleteAsset(path);
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            var machine = controller.layers[0].stateMachine;
            foreach (var old in machine.states) machine.RemoveState(old.state);
            machine.defaultState = null;
            foreach (string name in PlayerLook.AllClips)
            {
                var clip = all.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name));
                if (clip == null) { Debug.LogWarning($"Player: clip {name} not found in the animation libraries"); continue; }
                var state = machine.AddState(name);
                state.motion = clip;
                if (machine.defaultState == null) machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller); // unsaved states read back as an invalid, layer-less controller
            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}
