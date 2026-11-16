using UnityEngine;

namespace StillWater.UI
{
    /// <summary>
    /// The menu backdrop's slow life: the camera drifts and bobs over the water and keeps half an eye on the Listener,
    /// and some lamps flicker like dying tubes. Looks only. Built by Still Water → Art Spike → Build Menu Backdrop.
    /// </summary>
    public class MenuAtmosphere : MonoBehaviour
    {
        [SerializeField] private Transform cam;
        [SerializeField] private Transform watch;   // the Listener
        [SerializeField] private Light[] flicker;

        // Looks: drift amplitude (m), bob (m), how much the gaze follows the Listener, flicker odds per second.
        private const float Drift = 0.6f, Bob = 0.06f, Follow = 0.18f, FlickerChance = 0.6f;

        private Vector3 basePos;
        private Quaternion baseRot;
        private float[] baseIntensity;
        private float[] flickerLeft;

        private void Start()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (cam != null) { basePos = cam.position; baseRot = cam.rotation; }
            flicker ??= new Light[0];
            baseIntensity = new float[flicker.Length];
            flickerLeft = new float[flicker.Length];
            for (int i = 0; i < flicker.Length; i++) if (flicker[i] != null) baseIntensity[i] = flicker[i].intensity;
        }

        private void LateUpdate()
        {
            float t = Time.time;
            if (cam != null)
            {
                cam.position = basePos + new Vector3(Mathf.Sin(t * 0.11f) * Drift, Mathf.Sin(t * 0.37f) * Bob, Mathf.Sin(t * 0.07f) * Drift * 0.5f);
                var rot = baseRot * Quaternion.Euler(Mathf.Sin(t * 0.23f) * 0.8f, Mathf.Sin(t * 0.13f) * 2f, Mathf.Sin(t * 0.17f) * 0.6f);
                if (watch != null)
                    rot = Quaternion.Slerp(rot, Quaternion.LookRotation(watch.position - cam.position), Follow);
                cam.rotation = rot;
            }
            for (int i = 0; i < flicker.Length; i++)
            {
                if (flicker[i] == null) continue;
                if (flickerLeft[i] > 0f) flickerLeft[i] -= Time.deltaTime;
                else if (Random.value < FlickerChance * Time.deltaTime) flickerLeft[i] = Random.Range(0.05f, 0.6f);
                bool off = flickerLeft[i] > 0f && Random.value < 0.6f;
                flicker[i].intensity = off ? baseIntensity[i] * 0.08f : baseIntensity[i];
            }
        }
    }
}
