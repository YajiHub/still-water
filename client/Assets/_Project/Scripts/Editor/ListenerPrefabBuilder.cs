using System.Linq;
using StillWater.Monster;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace StillWater.Editor
{
    /// <summary>
    /// Builds Art/Listener/Listener.prefab from Listener_Custom.fbx (original model, made by art/listener/build_listener.py
    /// in Blender): a Generic rig with its own clips, wet pale skin + raw red mouth/claws, an Animator, no ListenerBody
    /// (the proportions are modelled, not stretched in code). Re-run it any time: it overwrites the prefab, the controller
    /// and both materials, and keeps the prefab's GUID, so every level's Listener updates.
    /// </summary>
    public static class ListenerPrefabBuilder
    {
        private const string Dir = "Assets/_Project/Art/Listener";
        private const string BodyPath = Dir + "/" + ListenerImport.CustomModel + ".fbx";
        // First = default state (ListenerLook's clip names): roam/hunt, linger, catch lunge.
        private static readonly string[] Clips = { ListenerLook.Swim, ListenerLook.Tread, ListenerLook.Lunge };

        [MenuItem("Still Water/Art Spike/Build Listener Prefab")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(BodyPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<Avatar>().FirstOrDefault();
            if (model == null || avatar == null)
            {
                Debug.LogError($"Listener: {BodyPath} missing or imported without an avatar (see ListenerImport).");
                return;
            }

            // The FBX faces Unity -Z (Blender's +Y): a Generic rig keeps the file's facing, so the model sits turned around
            // under an unrotated root. ListenerAI faces the root along +Z.
            var go = new GameObject("Listener");
            var modelGo = Object.Instantiate(model);
            modelGo.name = "Model";
            modelGo.transform.SetParent(go.transform, false);
            modelGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            // Slot order comes from the Blender file (MATS in build_listener.py). ListenerAI's look glow edits slot 0 and
            // pulses the material named ListenerGlow.
            // Baked by build_listener.py over one UV atlas: colour (A = smoothness), tangent normals, the vein glow mask.
            var colour = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/T_Listener_Skin.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/T_Listener_Normal.png");
            var veins = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/T_Listener_Veins.png");
            var slots = new[]
            {
                Material("ListenerSkin", Color.white, 1f, Color.black, colour, normal, veins),     // emission colour: ListenerAI
                Material("ListenerMouth", Color.white, 1f, new Color(0.12f, 0.01f, 0.01f), colour, normal), // raw flesh, claws
                Material("ListenerGlow", new Color(0.45f, 1f, 0.85f), 0.5f, new Color(0.45f, 1f, 0.85f) * 4f),     // sound pits
                Material("ListenerThroat", new Color(0.3f, 0.03f, 0.01f), 0.6f, new Color(1f, 0.16f, 0.04f) * 3f),  // the ember
                Material("ListenerHair", new Color(0.015f, 0.015f, 0.02f), 0.88f, Color.black),                    // wet black
            };
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select((_, i) => slots[Mathf.Min(i, slots.Length - 1)]).ToArray();
            // A skinned mesh is culled by its rest-pose bounds. This body bends and rears far outside them, so parts
            // popped out or froze near the camera. One renderer, so computing real bounds each frame is cheap.
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;

            var animator = modelGo.GetComponent<Animator>();
            if (animator == null) animator = modelGo.AddComponent<Animator>(); // not ??: Unity's fake null
            animator.avatar = avatar;
            animator.runtimeAnimatorController = Controller();
            animator.applyRootMotion = false; // ListenerAI moves it
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms; // under the opaque water most of the time

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Dir}/Listener.prefab");
            Object.DestroyImmediate(go);
            Selection.activeObject = prefab;
            Debug.Log($"Built {Dir}/Listener.prefab: drag it into a scene and press Play.");
        }

        private static Material Material(string name, Color color, float smoothness, Color emission, Texture2D baseMap = null,
                                         Texture2D normal = null, Texture2D emissionMap = null)
        {
            string path = $"{Dir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", baseMap);
            mat.SetFloat("_Smoothness", smoothness);
            // With a baked map, smoothness comes from its alpha (wet veins and blood, dull rot); _Smoothness scales it.
            bool alphaSmooth = baseMap != null;
            mat.SetFloat("_SmoothnessTextureChannel", alphaSmooth ? 1f : 0f);
            if (alphaSmooth) mat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            else mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            if (normal != null) mat.EnableKeyword("_NORMALMAP");
            else mat.DisableKeyword("_NORMALMAP");
            mat.SetTexture("_EmissionMap", emissionMap);
            mat.SetColor("_EmissionColor", emission); // HDR: above 1 blooms in the Look profiles
            if (emission.maxColorComponent > 0f || emissionMap != null) mat.EnableKeyword("_EMISSION");
            else mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static AnimatorController Controller()
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(BodyPath).OfType<AnimationClip>()
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
                var clip = all.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name)); // FBX names are "ListenerRig|Swim_Fwd_Loop"
                if (clip == null) { Debug.LogWarning($"Listener: clip {name} not found in {BodyPath}"); continue; }
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
