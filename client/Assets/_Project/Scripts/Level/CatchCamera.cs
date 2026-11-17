using System.Collections;
using StillWater.Monster;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// The catch, shown (Title and Monster Identity: "the one full look"). A close-up from just behind your shoulder,
    /// low at the water: the Listener rears up over you, splits its head open, screams and slams down at you, its open
    /// head filling the screen. It always faces you: ListenerAI squares it up and settles it at ListenerLook.LungeReach,
    /// where the slam ends just in front of the lens. The camera aims at its head (within limits, so it never tips over),
    /// shakes on the slam, and comes in closer if a wall is behind you. Looks only: the loss is already decided.
    /// (2026-11-17: a camera in front of the Listener, past you, made it lunge out of the screen past you; a side view
    /// was too far away. This is the close-up, with you in it.)
    /// </summary>
    public static class CatchCamera
    {
        // Looks: how far behind you, how far off to the side (over the shoulder), height above the water, how fast it
        // gets there, field of view, the closest it may stand to a wall.
        private const float Behind = 1.0f, Shoulder = 0.35f, Height = 0.5f, Settle = 0.25f, Fov = 80f, WallPadding = 0.25f;
        private const float ShakeFrom = 1.25f, ShakeRamp = 0.15f, Shake = 0.07f;
        // Looks: the aim point between its chest (0) and head (1); the aim's limits up/down (degrees).
        private const float HeadWeight = 0.8f, MaxPitchUp = 70f, MaxPitchDown = 30f;

        public static IEnumerator Play(Camera cam, Transform player, ListenerAI listener, float waterY, float seconds) =>
            Play(cam, player, listener.transform, listener.Chest, listener.Head, waterY, seconds);

        /// <summary>Any Listener body (the online ones have no AI): its root, chest and head bones.</summary>
        public static IEnumerator Play(Camera cam, Transform player, Transform monster, Transform chest, Transform head, float waterY, float seconds)
        {
            Transform t = cam.transform;
            Vector3 startPos = t.position;
            Quaternion startRot = t.rotation;
            float startFov = cam.fieldOfView;
            // Fixed at the moment of the catch, so the shot doesn't swing as the bodies move. "front" = Listener → you.
            Vector3 front = player.position - monster.position;
            front.y = 0f;
            if (front.sqrMagnitude < 0.0001f) front = monster.forward;
            front.y = 0f;
            front.Normalize();
            Vector3 you = new Vector3(player.position.x, waterY + Height, player.position.z);
            Vector3 back = (front + Vector3.Cross(Vector3.up, front) * (Shoulder / Behind)).normalized;
            float dist = Behind;
            if (Physics.SphereCast(you, WallPadding, back, out var hit, Behind, ~0, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.3f, hit.distance - WallPadding);
            Vector3 eye = you + back * dist;
            Quaternion aim = Quaternion.LookRotation(-front);

            for (float time = 0f; time < seconds; time += Time.deltaTime)
            {
                Vector3 look = head != null && chest != null
                    ? Vector3.Lerp(chest.position, head.position, HeadWeight)
                    : monster.position + Vector3.up * 2f;
                aim = Aim(look - eye, aim);
                float k = Mathf.SmoothStep(0f, 1f, time / Settle);
                float shake = Shake * Mathf.Clamp01((time - ShakeFrom) / ShakeRamp) * Mathf.Clamp01((seconds - time) / ShakeRamp);
                t.position = Vector3.Lerp(startPos, eye, k) + Random.insideUnitSphere * shake;
                t.rotation = Quaternion.Slerp(startRot, aim, k);
                cam.fieldOfView = Mathf.Lerp(startFov, Fov, k);
                yield return null;
            }
        }

        // Looks along d, the pitch kept between MaxPitchDown and MaxPitchUp. A target almost straight overhead keeps the
        // previous aim, so the view never flips.
        private static Quaternion Aim(Vector3 d, Quaternion previous)
        {
            Vector3 flat = new Vector3(d.x, 0f, d.z);
            if (flat.magnitude < 0.2f) return previous;
            float pitch = Mathf.Clamp(Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg, -MaxPitchDown, MaxPitchUp);
            return Quaternion.LookRotation(flat) * Quaternion.Euler(-pitch, 0f, 0f);
        }
    }
}
