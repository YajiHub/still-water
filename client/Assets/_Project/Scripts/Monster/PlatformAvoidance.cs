using StillWater.Level;
using UnityEngine;

namespace StillWater.Monster
{
    /// <summary>
    /// Keeps the Listener from swimming through platforms and decks like a ghost. It looks ahead at the waterline, turns
    /// toward the clearest of a few headings, and when there's no way round (or it's already under one) it dives
    /// beneath. Platforms are only slabs at the surface, so going under them is real, and the water is opaque.
    /// Used by ListenerAI, the online Listeners and the menu's Listener.
    /// </summary>
    public static class PlatformAvoidance
    {
        // Looks: steering clearance (about its shoulders: it fits the usual gaps between platforms), how far ahead it
        // looks, how fast it ducks (m/s). It ducks only when its chest is right inside a platform's footprint, and only
        // briefly: the water is opaque, so every dive makes it vanish. (Wider dive checks hid it most of the time in
        // the dense early levels: the 2026-11-16 "invisible" and "disappears for seconds" playtests.)
        public const float Radius = 0.7f, LookAhead = 3f, DiveSpeed = 3.5f, ChestRadius = 0.15f, DiveHold = 0.3f;
        private const float WaterlineDepth = 0.25f;
        private static readonly float[] Headings = { 0f, 30f, -30f, 60f, -60f, 95f, -95f };
        private static readonly RaycastHit[] Hits = new RaycastHit[16];
        private static readonly Collider[] Overlaps = new Collider[16];

        /// <summary>Platforms and decks: a Platform component (levels) or a "Platform" object (the menu backdrop).</summary>
        public static bool IsObstacle(Collider c) =>
            c != null && !c.isTrigger && (c.GetComponentInParent<Platform>() != null || c.name.StartsWith("Platform"));

        /// <summary>A platform within reach along <paramref name="dir"/>, or already over it.</summary>
        public static bool Blocked(Vector3 pos, float surfaceY, Vector3 dir, float reach = LookAhead)
        {
            if (Overlapping(pos, surfaceY)) return true;
            var origin = new Vector3(pos.x, surfaceY - WaterlineDepth, pos.z);
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) return false;
            int n = Physics.SphereCastNonAlloc(origin, Radius, dir.normalized, Hits, reach, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (IsObstacle(Hits[i].collider)) return true;
            return false;
        }

        /// <summary>Its chest is inside a platform's footprint right now: the only time it ducks under.</summary>
        public static bool UnderPlatform(Vector3 pos, float surfaceY) => Overlapping(pos, surfaceY, ChestRadius);

        public static bool Overlapping(Vector3 pos, float surfaceY, float radius = Radius)
        {
            var origin = new Vector3(pos.x, surfaceY - WaterlineDepth, pos.z);
            int n = Physics.OverlapSphereNonAlloc(origin, radius, Overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (IsObstacle(Overlaps[i])) return true;
            return false;
        }

        /// <summary>
        /// Which turn (degrees off the wanted heading) to swim on. Sticks with <paramref name="current"/> while it's
        /// still clear, so the choice doesn't flip back and forth frame to frame (that zig-zag looked glitchy). Back to
        /// straight (0) as soon as straight is clear. <paramref name="dive"/> = nothing is clear: go under.
        /// </summary>
        public static float SteerAngle(Vector3 pos, float surfaceY, Vector3 wanted, float current, out bool dive)
        {
            wanted.y = 0f;
            dive = false;
            if (wanted.sqrMagnitude < 1e-6f) return 0f;
            wanted.Normalize();
            if (Overlapping(pos, surfaceY)) { dive = true; return current; }
            if (!Blocked(pos, surfaceY, wanted)) return 0f;
            if (current != 0f && !Blocked(pos, surfaceY, Quaternion.Euler(0f, current, 0f) * wanted)) return current;
            foreach (float a in Headings)
            {
                if (a == 0f) continue;
                if (!Blocked(pos, surfaceY, Quaternion.Euler(0f, a, 0f) * wanted)) return a;
            }
            dive = true;
            return 0f;
        }

        /// <summary>
        /// The heading to swim on: the wanted one if it's clear, otherwise the nearest clear turn. If none is clear, the
        /// wanted one, and <paramref name="dive"/> says go under.
        /// </summary>
        public static Vector3 Steer(Vector3 pos, float surfaceY, Vector3 wanted, out bool dive)
        {
            wanted.y = 0f;
            dive = false;
            if (wanted.sqrMagnitude < 1e-6f) return wanted;
            wanted.Normalize();
            if (Overlapping(pos, surfaceY)) { dive = true; return wanted; }
            foreach (float a in Headings)
            {
                var d = Quaternion.Euler(0f, a, 0f) * wanted;
                if (!Blocked(pos, surfaceY, d)) return d;
            }
            dive = true;
            return wanted;
        }
    }
}
