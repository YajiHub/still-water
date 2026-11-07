using UnityEngine;

namespace StillWater.Core
{
    /// <summary>
    /// Placeholder sounds generated in code, so the prototype needs no audio files or credits.
    /// </summary>
    // shortcut: synthesized placeholders; replace with real CC0 sounds (credited in CREDITS.md) in Phase 5.
    public static class ProceduralAudio
    {
        private const int Rate = 44100;

        /// <summary>A soft bell-like tone that fades out (the Reveal chime).</summary>
        public static AudioClip Chime(float frequency, float seconds = 0.6f) =>
            Make("chime", seconds, t => Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-5f * t) * 0.5f);

        /// <summary>
        /// Fallback splash if Resources/Audio/Splash is missing: a low "plop" dropping in pitch plus a muffled wash
        /// (low-passed noise; plain noise sounds like steam, synthesized bubbles sound fake).
        /// </summary>
        public static AudioClip Splash(float seconds = 0.9f)
        {
            var random = new System.Random(7);
            int count = Mathf.CeilToInt(seconds * Rate);
            var samples = new float[count];
            float wash = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                wash += 0.15f * (((float)random.NextDouble() * 2f - 1f) - wash); // one-pole low-pass ≈ 1.1 kHz
                float plop = Mathf.Sin(2f * Mathf.PI * (60f * t + 120f / 25f * (1f - Mathf.Exp(-25f * t)))) * Mathf.Exp(-22f * t); // 180 → 60 Hz
                samples[i] = plop * 0.5f + wash * Mathf.Min(1f, t * 200f) * Mathf.Exp(-6f * t) * 1.4f;
            }
            var clip = AudioClip.Create("splash", count, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Two low thumps (lub-dub) for the heartbeat loop.</summary>
        public static AudioClip Heartbeat(float seconds = 0.8f) =>
            Make("heartbeat", seconds, t => Thump(t) + 0.7f * Thump(t - 0.22f));

        /// <summary>A low underwater drone that swells and fades (the Listener's 3D loop). Whole cycles in 2 s, so it loops without a click.</summary>
        public static AudioClip Rumble(float seconds = 2f) =>
            Make("rumble", seconds, t => (Mathf.Sin(2f * Mathf.PI * 35f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 47f * t))
                * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.5f * t)) * 0.35f);

        private static float Thump(float t) => t < 0f ? 0f : Mathf.Sin(2f * Mathf.PI * 50f * t) * Mathf.Exp(-18f * t) * 0.9f;

        private static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
        {
            int count = Mathf.CeilToInt(seconds * Rate);
            var samples = new float[count];
            for (int i = 0; i < count; i++) samples[i] = wave(i / (float)Rate);
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
