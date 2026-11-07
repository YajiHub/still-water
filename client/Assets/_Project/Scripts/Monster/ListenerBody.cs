using UnityEngine;

namespace StillWater.Monster
{
    /// <summary>
    /// Makes the Quaternius body read as the Listener (Title and Monster Identity), not a man: oversized, a broad back on thin legs,
    /// too-long thin forearms and claw hands, a long neck with the head cocked, and no face (a smooth skin mask).
    /// Runs after the Animator each frame (humanoid clips don't animate bone scale), so the FBX stays untouched.
    /// </summary>
    // shortcut: thinning a bone whose child bends (knee, shoulder) shears the mesh a little; fine at swim distance,
    // a Blender-sculpted body would fix it if the catch close-up shows it.
    [RequireComponent(typeof(Animator))]
    public class ListenerBody : MonoBehaviour
    {
        [Header("Arms")]
        [Tooltip("Forearm length × (the hand inherits it: longer fingers)")]
        [SerializeField, Range(1f, 2.5f)] private float forearmStretch = 1.8f;
        [Tooltip("Forearm and hand thickness × (lower = gaunter)")]
        [SerializeField, Range(0.4f, 1f)] private float forearmThickness = 0.65f;
        [Tooltip("Upper arm length × (moves the elbow out)")]
        [SerializeField, Range(1f, 2f)] private float upperArmStretch = 1.25f;

        [Header("Body")]
        [Tooltip("Overall size × (looks only: catching still uses LevelConfig's catch distance)")]
        [SerializeField, Range(0.8f, 2f)] private float bodySize = 1.35f;
        [Tooltip("Waist, chest, shoulders and arms width × (> 1 = a broad, hulking back; the head keeps its size)")]
        [SerializeField, Range(0.4f, 1.8f)] private float torsoThickness = 1.3f;
        [Tooltip("Thigh-down thickness ×")]
        [SerializeField, Range(0.4f, 1f)] private float legThickness = 0.7f;

        [Header("Head")]
        [Tooltip("Neck length × (moves the head up)")]
        [SerializeField, Range(1f, 3f)] private float neckStretch = 1.8f;
        [Tooltip("Head roll in degrees: a cocked, listening angle")]
        [SerializeField, Range(-45f, 45f)] private float headTilt = 18f;
        [Tooltip("Face mask size (width, height, depth) in metres: covers nose, mouth and eye sockets")]
        [SerializeField] private Vector3 maskSize = new Vector3(0.16f, 0.21f, 0.12f);
        [Tooltip("Face mask position from the head bone: up, forward (metres)")]
        [SerializeField] private Vector2 maskOffset = new Vector2(0.09f, 0.06f);

        private Transform[] forearms;
        private int[] forearmAlong;
        private Vector3[] elbowRest;
        private Transform spine, neck, head, mask;
        private Transform[] thighs;
        private int spineAlong;
        private int[] thighAlong;
        private Vector3 neckRest, headRest, headUp, headForward; // head-local directions of the body's up / facing
        private Quaternion lastTilted;

        // Awake runs on the prefab's stored bind pose, before any animation: the only time "up" and "facing" are clean.
        private void Awake()
        {
            var a = GetComponent<Animator>();
            forearms = new[] { a.GetBoneTransform(HumanBodyBones.LeftLowerArm), a.GetBoneTransform(HumanBodyBones.RightLowerArm) };
            var hands = new[] { a.GetBoneTransform(HumanBodyBones.LeftHand), a.GetBoneTransform(HumanBodyBones.RightHand) };
            forearmAlong = new int[2];
            elbowRest = new Vector3[2];
            for (int i = 0; i < 2; i++)
            {
                if (forearms[i] == null || hands[i] == null) continue;
                forearmAlong[i] = LongestAxis(hands[i].localPosition); // the forearm points at the hand: its length axis
                elbowRest[i] = forearms[i].localPosition; // the forearm bone's position *is* the elbow
            }

            spine = a.GetBoneTransform(HumanBodyBones.Spine);
            neck = a.GetBoneTransform(HumanBodyBones.Neck);
            head = a.GetBoneTransform(HumanBodyBones.Head);
            var chest = a.GetBoneTransform(HumanBodyBones.Chest);
            if (spine != null && chest != null) spineAlong = LongestAxis(chest.localPosition);
            thighs = new[] { a.GetBoneTransform(HumanBodyBones.LeftUpperLeg), a.GetBoneTransform(HumanBodyBones.RightUpperLeg) };
            var calves = new[] { a.GetBoneTransform(HumanBodyBones.LeftLowerLeg), a.GetBoneTransform(HumanBodyBones.RightLowerLeg) };
            thighAlong = new int[2];
            for (int i = 0; i < 2; i++) if (thighs[i] != null && calves[i] != null) thighAlong[i] = LongestAxis(calves[i].localPosition);
            if (neck != null) neckRest = neck.localPosition;
            if (head != null) headRest = head.localPosition;

            var hips = a.GetBoneTransform(HumanBodyBones.Hips);
            var lShoulder = a.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            var rShoulder = a.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (head != null && hips != null && lShoulder != null && rShoulder != null)
            {
                Vector3 up = (head.position - hips.position).normalized;
                Vector3 facing = Vector3.Cross(rShoulder.position - lShoulder.position, up).normalized;
                headUp = head.InverseTransformDirection(up);
                headForward = head.InverseTransformDirection(facing);
                mask = MakeMask();
            }
        }

        private void LateUpdate()
        {
            transform.localScale = Vector3.one * bodySize;
            for (int i = 0; i < 2; i++)
            {
                if (forearms[i] == null) continue;
                forearms[i].localScale = Thin(forearmThickness, forearmAlong[i], forearmStretch); // leaf chain: claws
                forearms[i].localPosition = elbowRest[i] * upperArmStretch;
                if (thighs[i] != null) thighs[i].localScale = Thin(legThickness, thighAlong[i], 1f);
            }
            if (spine != null)
            {
                spine.localScale = Thin(torsoThickness, spineAlong, 1f);
                // Undo it at the neck (roughly the same axes along the spine chain), so the head keeps its size.
                if (neck != null) neck.localScale = Thin(1f / torsoThickness, spineAlong, 1f);
            }
            if (neck != null) neck.localPosition = neckRest * Mathf.Sqrt(neckStretch); // half the length below the neck,
            if (head != null)                                                            // half above it
            {
                head.localPosition = headRest * Mathf.Sqrt(neckStretch);
                // Tilt only a fresh pose: when culled off-screen the Animator skips the head and it would keep turning.
                if (headForward != Vector3.zero && head.localRotation != lastTilted)
                {
                    head.localRotation *= Quaternion.AngleAxis(headTilt, headForward);
                    lastTilted = head.localRotation;
                }
            }
            if (mask != null)
            {
                mask.localPosition = headUp * maskOffset.x + headForward * maskOffset.y;
                mask.localRotation = Quaternion.LookRotation(headForward, headUp);
                mask.localScale = maskSize;
            }
        }

        // A smooth skin dome over the face: no nose, mouth or eye sockets. Uses the body's skin, so glow changes apply.
        private Transform MakeMask()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "FaceMask";
            Destroy(go.GetComponent<Collider>());
            var skinned = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned != null) go.GetComponent<Renderer>().sharedMaterial = skinned.sharedMaterial;
            go.transform.SetParent(head, false);
            return go.transform;
        }

        private static Vector3 Thin(float thickness, int along, float length)
        {
            var s = Vector3.one * thickness;
            s[along] = length;
            return s;
        }

        private static int LongestAxis(Vector3 p)
        {
            float x = Mathf.Abs(p.x), y = Mathf.Abs(p.y), z = Mathf.Abs(p.z);
            return x >= y && x >= z ? 0 : y >= z ? 1 : 2;
        }
    }
}
