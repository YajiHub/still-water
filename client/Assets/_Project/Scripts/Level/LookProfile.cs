using UnityEngine;
using UnityEngine.Rendering;

namespace StillWater.Level
{
    /// <summary>One art-style look (Art Style decision, Phase 1 spike): post-processing, fog, ambient and hall lights.</summary>
    [CreateAssetMenu(fileName = "Look", menuName = "Still Water/Look Profile")]
    public class LookProfile : ScriptableObject
    {
        public string label = "A";
        public VolumeProfile volume;

        [Header("Fog (exponential squared)")]
        public Color fogColor = Color.gray;
        public float fogDensity = 0.03f;

        [Header("Lighting")]
        public Color ambientColor = Color.gray;
        [Range(0f, 1f)] public float reflectionIntensity = 0.5f;
        public Color lightColor = Color.white;
        public float lightIntensity = 2f;
        public float lightRange = 16f;

        [Header("Headlamp (a spot light from the player's eyes)")]
        [Tooltip("0 = off")] public float headlampIntensity;
        public Color headlampColor = new Color(0.8f, 0.92f, 1f);
        public float headlampRange = 16f;
        [Range(10f, 120f)] public float headlampAngle = 60f;

        [Header("Listener")]
        [Tooltip("Faint glow of its skin and wake, so it reads in the dark (keep below the bloom threshold, ~0.8)")]
        public Color listenerGlow = Color.black;

        [Header("Cost")]
        [Tooltip("Share of screen resolution rendered (lower = faster, softer)")]
        [Range(0.3f, 1f)] public float renderScale = 0.8f;
    }
}
