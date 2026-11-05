using System.Collections;
using System.Collections.Generic;
using StillWater.Core;
using StillWater.Player;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// The Reveal (Core Loop Rules): the camera lifts above the start, the safe path glows tile by tile with a
    /// rising chime, then the camera glides back into the player's eyes. Callable again for Pattern Re-show.
    /// </summary>
    public class PatternRevealer : MonoBehaviour
    {
        [Tooltip("Bright glow swapped onto the path's edge strips while they're lit")]
        [SerializeField] private Material brightGlow;

        private const float InsetFromBackWall = 0.5f, CeilingGap = 0.5f, HoldSeconds = 0.8f, FirstNoteHz = 440f, EyeHeight = 1.6f;
        private const float SemitoneRatio = 1.0594631f; // each tile one note higher

        private AudioSource audioSource;
        private AudioClip chime;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; // the chime is "in your head", not in the room
            chime = ProceduralAudio.Chime(FirstNoteHz);
        }

        /// <summary>The overhead camera pose for this level (also used during the Intro).</summary>
        public void MoveToOverhead(LevelConfig config, Transform cam, Transform start, IReadOnlyList<Platform> path)
        {
            Vector3 center = Vector3.zero;
            foreach (var p in path) center += p.transform.position;
            center /= path.Count;

            // Inside the hall: above the back of the start deck, in line with the path, below the ceiling.
            Bounds deck = start.GetComponent<Collider>().bounds;
            Vector3 deckTop = new Vector3(center.x, deck.max.y, deck.min.z + InsetFromBackWall);
            float height = config.revealHeight;
            if (Physics.Raycast(deckTop + Vector3.up * 0.1f, Vector3.up, out var ceiling, 100f, ~0, QueryTriggerInteraction.Ignore))
                height = Mathf.Min(height, ceiling.distance - CeilingGap);
            Vector3 eye = deckTop + Vector3.up * height;
            cam.SetPositionAndRotation(eye, Quaternion.LookRotation(center - eye));
        }

        public IEnumerator Reveal(LevelConfig config, Transform cam, PlayerMotor player, IReadOnlyList<Platform> path)
        {
            float normalFog = RenderSettings.fogDensity;
            RenderSettings.fogDensity = normalFog * config.revealFogMultiplier; // the hall "lights up" to show the path

            // The camera stays in the overhead view; the path lights up tile by tile with a rising chime.
            var lit = new List<(Renderer renderer, Material original)>();
            float pitch = 1f;
            foreach (var platform in path)
            {
                foreach (var r in platform.GetComponentsInChildren<Renderer>())
                {
                    if (r.gameObject == platform.gameObject) continue; // only the Glow strips, not the slab
                    lit.Add((r, r.sharedMaterial));
                    r.sharedMaterial = brightGlow;
                }
                audioSource.pitch = pitch;
                audioSource.PlayOneShot(chime);
                pitch *= SemitoneRatio;
                yield return new WaitForSeconds(config.glowSecondsPerTile);
            }

            yield return new WaitForSeconds(HoldSeconds);
            foreach (var (r, original) in lit) r.sharedMaterial = original;
            RenderSettings.fogDensity = normalFog;

            // Back into the player's eyes, looking the way the body faces (where first person starts).
            Vector3 fromPos = cam.position;
            Quaternion fromRot = cam.rotation;
            Vector3 toPos = player.transform.position + Vector3.up * EyeHeight;
            Quaternion toRot = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
            for (float t = 0f; t < 1f; t += Time.deltaTime / config.revealGlideSeconds)
            {
                float s = Mathf.SmoothStep(0f, 1f, t);
                cam.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, s), Quaternion.Slerp(fromRot, toRot, s));
                yield return null;
            }
        }
    }
}
