using System.Collections.Generic;
using UnityEngine;

namespace StillWater.Level
{
    /// <summary>
    /// Water and Hall Look, applied when a level loads (so no level needs rebuilding): re-maps the UVs of every tiled
    /// box to its real size so pool tiles stay square on a 70 m floor and a 2 m platform alike, and hangs a lamp
    /// fixture (glowing shade + cable) on each hall light. Added by <see cref="PoolWater"/>; materials from
    /// Resources/Hall (Still Water → Art Spike → Set Up Hall).
    /// </summary>
    public class HallDressing : MonoBehaviour
    {
        // Looks, not gameplay: metres per texture repeat (Tiles020 has ~16 tiles across: ~10 cm mosaic).
        private const float TileMetres = 1.5f, CableLength = 7f;
        private static readonly Vector3 ShadeSize = new Vector3(0.7f, 0.12f, 0.7f);

        private readonly List<Mesh> made = new List<Mesh>();

        private void Start()
        {
            var shade = Resources.Load<Material>("Hall/LampShade");
            var cable = Resources.Load<Material>("Hall/LampCable");
            if (shade == null || cable == null)
            {
                Debug.LogWarning("HallDressing: run Still Water → Art Spike → Set Up Hall (Resources/Hall missing). Plain hall.");
                return;
            }
            RetileBoxes();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Point && l.name == "HallLight") HangFixture(l.transform.position, shade, cable);
        }

        // Box projection: each face gets UVs from the two world axes it spans, in TileMetres. Boxes of the same size
        // share one mesh (a level has ~10 distinct sizes, not ~130).
        private void RetileBoxes()
        {
            var bySize = new Dictionary<Vector3Int, Mesh>();
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                string mat = r.sharedMaterial != null ? r.sharedMaterial.name : "";
                if (mat != "Tile" && mat != "Wall") continue;
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue; // phone: unreadable mesh = keep plain
                Vector3 s = r.transform.lossyScale;
                var key = Vector3Int.RoundToInt(s * 100f);
                if (!bySize.TryGetValue(key, out var mesh))
                {
                    mesh = Instantiate(filter.sharedMesh);
                    var v = mesh.vertices;
                    var n = mesh.normals;
                    var uv = new Vector2[v.Length];
                    for (int i = 0; i < v.Length; i++)
                    {
                        Vector3 p = Vector3.Scale(v[i], s) / TileMetres, a = new Vector3(Mathf.Abs(n[i].x), Mathf.Abs(n[i].y), Mathf.Abs(n[i].z));
                        uv[i] = a.y >= a.x && a.y >= a.z ? new Vector2(p.x, p.z) : a.x >= a.z ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                    }
                    mesh.uv = uv;
                    mesh.RecalculateTangents(); // the normal map follows the new UVs
                    bySize[key] = mesh;
                    made.Add(mesh);
                }
                filter.sharedMesh = mesh;
            }
        }

        private void HangFixture(Vector3 at, Material shade, Material cable)
        {
            var lamp = new GameObject("LampFixture");
            lamp.transform.SetParent(transform.parent, true); // beside the water, never under its 70 m scale
            Part(PrimitiveType.Cube, lamp, at + Vector3.up * (ShadeSize.y / 2f + 0.05f), ShadeSize, shade);
            // The light hangs well below the ceiling: the cable runs up into it (the excess hides in the slab).
            Part(PrimitiveType.Cylinder, lamp, at + Vector3.up * (CableLength / 2f), new Vector3(0.025f, CableLength / 2f, 0.025f), cable);
        }

        private static void Part(PrimitiveType type, GameObject parent, Vector3 at, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent.transform, false);
            go.transform.position = at;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void OnDestroy()
        {
            foreach (var m in made) Destroy(m);
        }
    }
}
