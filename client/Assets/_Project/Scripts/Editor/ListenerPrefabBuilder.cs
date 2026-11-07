using System.Linq;
using StillWater.Monster;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StillWater.Editor
{
    /// <summary>
    /// Builds Art/Listener/Listener.prefab from the Quaternius body + animation libraries (Listener Asset Source):
    /// eyes and eyebrows removed, pale wet skin, an Animator with the Listener's clips, <see cref="ListenerBody"/>.
    /// Re-run it any time: it overwrites the prefab, the controller and the skin.
    /// </summary>
    public static class ListenerPrefabBuilder
    {
        private const string Dir = "Assets/_Project/Art/Listener";
        private const string BodyPath = Dir + "/Superhero_Male_FullBody.fbx";
        private static readonly string[] Libraries = { Dir + "/UAL1_Standard.fbx", Dir + "/UAL2_Standard.fbx" };
        // First = default state. Roam/hunt, linger, rise, lunge, arms-out hold (decision 20 clip map).
        private static readonly string[] Clips = { "Swim_Fwd_Loop", "Swim_Idle_Loop", "LayToIdle", "Zombie_Scratch", "Zombie_Idle_Loop" };

        [MenuItem("Still Water/Art Spike/Build Listener Prefab")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<Avatar>().FirstOrDefault();
            if (model == null || avatar == null || !avatar.isHuman)
            {
                Debug.LogError($"Listener: {BodyPath} missing or not imported as Humanoid (see ListenerImport).");
                return;
            }

            var go = Object.Instantiate(model);
            go.name = "Listener";
            foreach (var r in go.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Eye")).ToArray())
                Object.DestroyImmediate(r.gameObject); // "Eyes" and "Eyebrows": the Listener has none

            var skin = Skin();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = Enumerable.Repeat(skin, r.sharedMaterials.Length).ToArray();

            var animator = go.GetComponent<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>(); // not ??: Unity's fake null
            animator.avatar = avatar;
            animator.runtimeAnimatorController = Controller();
            animator.applyRootMotion = false; // ListenerAI moves it
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; // under the opaque water most of the time
            go.AddComponent<ListenerBody>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/Listener.prefab");
            Object.DestroyImmediate(go);
            Selection.activeObject = prefab;
            Debug.Log($"Built {Dir}/Listener.prefab: drag it into a scene and press Play.");
        }

        private static Material Skin()
        {
            string path = $"{Dir}/ListenerSkin.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(0.74f, 0.78f, 0.74f)); // drowned pale, a little green
            mat.SetFloat("_Smoothness", 0.75f); // wet
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/T_Superhero_Male_Normal.png");
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static AnimatorController Controller()
        {
            var all = Libraries.SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToList();
            string path = $"{Dir}/Listener.controller";
            // Reuse the asset (never delete it): deleting breaks every Animator already pointing at it (controller=none).
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null || controller.layers.Length == 0)
            {
                if (controller != null) AssetDatabase.DeleteAsset(path); // the old layer-less, invalid one
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            var machine = controller.layers[0].stateMachine;
            foreach (var old in machine.states) machine.RemoveState(old.state);
            machine.defaultState = null; // the first added clip becomes the default again
            foreach (string name in Clips)
            {
                var clip = all.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name)); // FBX names are "Armature|Swim_Fwd_Loop"
                if (clip == null) { Debug.LogWarning($"Listener: clip {name} not found in the libraries"); continue; }
                var state = machine.AddState(name);
                state.motion = clip;
                if (machine.defaultState == null) machine.defaultState = state;
            }
            // The states only exist in memory until saved; an unsaved controller is read from disk as layer-less and
            // rejected ("The Animator Controller (Listener) you have used is not valid").
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"Listener controller: {machine.states.Length} states, default {machine.defaultState?.name}");
            return controller;
        }
    }
}
