using System.Collections;
using StillWater.Monster;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// The catch, shown (Title and Monster Identity: "the one full look"). The camera drops to just above the water in
    /// front of the Listener, behind you, and looks up at it as it rears, splits its head open and falls on you. It shakes
    /// on the slam. The shot is laid along the Listener's own facing: ListenerAI squares it up to you on the catch, so the
    /// open head always faces the lens. If a wall or pillar is in the way, the camera comes closer. Looks only: the loss
    /// is already decided.
    /// </summary>
    public static class CatchCamera
    {
        // Looks: distance in front of the Listener, height above the water, how fast it gets there, field of view.
        private const float Distance = 4.2f, Height = 0.35f, Settle = 0.3f, Fov = 74f, WallPadding = 0.3f;
        private const float ShakeFrom = 1.25f, ShakeRamp = 0.15f, Shake = 0.06f, HeadWeight = 0.75f;

        public static IEnumerator Play(Camera cam, Transform player, ListenerAI listener, float waterY, float seconds) =>
            Play(cam, player, listener.transform, listener.Chest, listener.Head, waterY, seconds);

        /// <summary>Any Listener body (the online ones have no AI): its root, chest and head bones.</summary>
        public static IEnumerator Play(Camera cam, Transform player, Transform monster, Transform chest, Transform head, float waterY, float seconds)
        {
            Transform t = cam.transform;
            Vector3 startPos = t.position;
            Quaternion startRot = t.rotation;
            float startFov = cam.fieldOfView;
            // Fixed at the moment of the catch, so the shot doesn't swing as the body moves.
            Vector3 front = monster.forward;
            front.y = 0f;
            if (front.sqrMagnitude < 0.0001f) front = player.position - monster.position;
            front.y = 0f;
            front.Normalize();
            Vector3 anchor = new Vector3(monster.position.x, waterY + Height, monster.position.z);
            Vector3 eye = anchor + front * Distance;
            if (Physics.SphereCast(anchor, WallPadding, front, out var hit, Distance, ~0, QueryTriggerInteraction.Ignore))
                eye = anchor + front * Mathf.Max(1.2f, hit.distance - WallPadding);

            for (float time = 0f; time < seconds; time += Time.deltaTime)
            {
                Vector3 look = head != null && chest != null
                    ? Vector3.Lerp(chest.position, head.position, HeadWeight)
                    : monster.position + Vector3.up * 2f;
                float k = Mathf.SmoothStep(0f, 1f, time / Settle);
                float shake = Shake * Mathf.Clamp01((time - ShakeFrom) / ShakeRamp) * Mathf.Clamp01((seconds - time) / ShakeRamp);
                t.position = Vector3.Lerp(startPos, eye, k) + Random.insideUnitSphere * shake;
                t.rotation = Quaternion.Slerp(startRot, Quaternion.LookRotation(look - t.position), k);
                cam.fieldOfView = Mathf.Lerp(startFov, Fov, k);
                yield return null;
            }
        }
    }
}
