using System.Linq;
using StillWater.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StillWater.Editor
{
    /// <summary>
    /// Builds Resources/Player/PlayerBody_Female.prefab and PlayerBody_Male.prefab from Art/Player/Player*_FullBody.fbx:
    /// the Quaternius bodies (CC0) with a hairstyle already merged onto their skeleton by art/player/build_player_bodies.py
    /// (re-binding hair at runtime left the player bald). Each gets its light skin texture and normal map, brown eyes,
    /// hair, and an Animator with Player.controller. The clips are the Universal Animation Library ones in
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
            public string id, fbx, skin, normal, hairTexture;
        }

        private static readonly Body[] Bodies =
        {
            new Body { id = "Female", fbx = "PlayerFemale_FullBody", skin = "T_Superhero_Female_Light_BaseColor",
                       normal = "T_Superhero_Female_Normal", hairTexture = "T_Hair_2_BaseColor" },
            new Body { id = "Male", fbx = "PlayerMale_FullBody", skin = "T_Superhero_Male_Light_BaseColor",
                       normal = "T_Superhero_Male_Normal", hairTexture = "T_Hair_1_BaseColor" },
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
                var skin = Material($"PlayerSkin_{b.id}", Tex(b.skin), Tex(b.normal), 0.45f);
                var hair = Material($"PlayerHair_{b.id}", Tex(b.hairTexture), null, 0.55f);

                var go = Object.Instantiate(model);
                go.name = $"PlayerBody_{b.id}";
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    // Eyes and Eyebrows have two slots (MI_Eyes, MI_Hair_n); the body and the hair have one.
                    bool face = r.name.StartsWith("Eye");
                    bool isHair = r.name == "Hair";
                    r.sharedMaterials = r.sharedMaterials.Select((_, i) => isHair ? hair : face ? (i == 0 ? eyes : hair) : skin).ToArray();
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

        private static Material Material(string name, Texture2D baseMap, Texture2D normal, float smoothness)
        {
            string path = $"{Dir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetFloat("_Smoothness", smoothness);
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
