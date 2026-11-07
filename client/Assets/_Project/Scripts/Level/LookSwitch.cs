using StillWater.Monster;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StillWater.Level
{
    /// <summary>
    /// Phase 1 art spike: applies one LookProfile at a time so the looks can be compared on the phone.
    /// Set up by Still Water → Art Spike → Set Up Looks.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class LookSwitch : MonoBehaviour
    {
        [SerializeField] private LookProfile[] looks;
        private Light[] hallLights;
        private Light headlamp;
        private ListenerAI listener;

        private int index;
        private float originalRenderScale = -1f;

        public string CurrentLabel => looks.Length > 0 ? looks[index].label : "-";

        private void Start()
        {
            // Found at runtime: rebuilding the level replaces the lights, so a saved link would go stale.
            hallLights = System.Array.FindAll(FindObjectsByType<Light>(FindObjectsSortMode.None), l => l.type == LightType.Point);
            var cam = Camera.main;
            if (cam != null)
            {
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
                headlamp = CreateHeadlamp(cam.transform);
            }
            listener = FindFirstObjectByType<ListenerAI>();
            Apply(looks.Length - 1); // the last look = C, the dark game look (A stays as the comparison)
        }

        public void Next() => Apply((index + 1) % looks.Length);

        private void Apply(int i)
        {
            if (looks.Length == 0) return;
            index = i;
            var look = looks[i];

            GetComponent<Volume>().sharedProfile = look.volume;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = look.fogColor;
            RenderSettings.fogDensity = look.fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = look.ambientColor;
            RenderSettings.reflectionIntensity = look.reflectionIntensity;

            foreach (var hallLight in hallLights)
            {
                hallLight.color = look.lightColor;
                hallLight.intensity = look.lightIntensity;
                hallLight.range = look.lightRange;
            }

            if (headlamp != null)
            {
                headlamp.enabled = look.headlampIntensity > 0f;
                headlamp.color = look.headlampColor;
                headlamp.intensity = look.headlampIntensity;
                headlamp.range = look.headlampRange;
                headlamp.spotAngle = look.headlampAngle;
                headlamp.innerSpotAngle = look.headlampAngle * 0.2f; // very soft edge: a hard one draws a bright arc on the glossy water
            }
            if (listener != null) listener.SetGlow(look.listenerGlow);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = look.fogColor; // far away fades into the fog, not a sky
            }

            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                if (originalRenderScale < 0f) originalRenderScale = urp.renderScale;
                urp.renderScale = look.renderScale;
            }
        }

        // Made at runtime (not by a menu), so every existing level scene gets it. It follows the camera, so it
        // also lights the Reveal's overhead view. No shadows: too costly on phones.
        private static Light CreateHeadlamp(Transform cam)
        {
            var go = new GameObject("Headlamp");
            go.transform.SetParent(cam, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.shadows = LightShadows.None;
            return light;
        }

        // In the Editor the pipeline asset is a real file: put its render scale back after Play.
        private void OnDestroy()
        {
            if (originalRenderScale >= 0f && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
                urp.renderScale = originalRenderScale;
        }
    }
}
