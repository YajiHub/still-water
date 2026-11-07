using System.IO;
using UnityEditor;
using UnityEngine;

namespace StillWater.Editor
{
    /// <summary>
    /// Water and Hall Look: turns Water.mat (URP Lit, shared by every level) into moving water. Bakes a tileable wave
    /// normal map from sine waves (no download), sets it as the main and detail normal, dark teal, glossy for lamp
    /// glints. <see cref="StillWater.Level.PoolWater"/> drifts the two layers at runtime. Re-run any time.
    /// </summary>
    // Plain URP Lit instead of a hand-written shader (by: delegated, 2026-11-07): Forward+ lamp glints and reflections
    // come for free and nothing can fail to compile on the phone.
    public static class WaterBuilder
    {
        private const string MaterialPath = "Assets/_Project/Art/GreyBox/Water.mat";
        private const string TexturePath = "Assets/_Project/Art/Water/WaterNormal.png";
        private const int Size = 256;

        [MenuItem("Still Water/Art Spike/Set Up Water")]
        public static void SetUp()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null) { Debug.LogError($"Set Up Water: {MaterialPath} missing: run Build Level once first."); return; }

            Directory.CreateDirectory(Path.GetDirectoryName(TexturePath));
            File.WriteAllBytes(TexturePath, BakeNormals().EncodeToPNG());
            AssetDatabase.ImportAsset(TexturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.NormalMap;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);

            mat.SetColor("_BaseColor", new Color(0.03f, 0.15f, 0.18f)); // dark teal: opaque, nothing seen below
            mat.SetFloat("_Smoothness", 0.85f); // lamp glints without mirroring the headlamp into a glare band
            // Indoors: don't mirror the scene's default sky (its horizon showed as a bright arc that moved with the
            // camera). Lamp glints are specular highlights from the lights themselves, so they stay.
            mat.SetFloat("_EnvironmentReflections", 0f);
            mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 0.6f);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_DetailNormalMap", normal);
            mat.SetFloat("_DetailNormalMapScale", 0.45f);
            mat.EnableKeyword("_DETAIL_MULX2"); // detail albedo stays the default grey (× 2 grey = no colour change)
            EditorUtility.SetDirty(mat);
            ParticleMaterial("Ripple", BakeSprite(128, r => Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.1f))); // thin soft ring
            ParticleMaterial("Drop", BakeSprite(32, r => Mathf.Clamp01(1f - r) * Mathf.Clamp01(1f - r)));     // soft dot
            AssetDatabase.SaveAssets();
            Debug.Log("Water set up: Water.mat now has drifting wave normals, and Resources/WaterFx has the splash/ripple/foam materials (every level). Press Play.");
        }

        // A copy of URP's own particle material (already alpha-blended, transparent) with our sprite, in Resources so
        // WaterFx can load it at runtime and the phone build always includes it.
        private static void ParticleMaterial(string name, Texture2D sprite)
        {
            const string dir = "Assets/_Project/Resources/WaterFx";
            Directory.CreateDirectory(dir);
            string texPath = $"{dir}/{name}.png", matPath = $"{dir}/{name}.mat";
            File.WriteAllBytes(texPath, sprite.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                if (!AssetDatabase.CopyAsset(UrpParticles, matPath)) { Debug.LogError($"Set Up Water: could not copy {UrpParticles}"); return; }
                mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_BaseColor", Color.white); // tint comes from each particle's colour
            EditorUtility.SetDirty(mat);
        }

        private const string UrpParticles = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat";

        // White sprite whose alpha is alpha(r), r = 0 at the centre, 1 at the edge.
        private static Texture2D BakeSprite(int size, System.Func<float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha(Mathf.Sqrt(dx * dx + dy * dy)));
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // Heights = a few sine waves with whole-number frequencies, so the texture repeats seamlessly;
        // the normal comes from their exact slopes.
        private static Texture2D BakeNormals()
        {
            var waves = new[] { (1, 2, 0.5f), (3, -1, 0.35f), (-2, 3, 0.3f), (5, 4, 0.15f), (-6, 5, 0.12f), (8, -7, 0.08f) };
            var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false, true);
            var pixels = new Color[Size * Size];
            const float strength = 0.9f;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (float)x / Size, v = (float)y / Size, dx = 0f, dy = 0f;
                for (int i = 0; i < waves.Length; i++)
                {
                    var (fx, fy, a) = waves[i];
                    float c = Mathf.Cos(2f * Mathf.PI * (fx * u + fy * v) + i * 1.7f) * 2f * Mathf.PI * a / 30f;
                    dx += c * fx;
                    dy += c * fy;
                }
                var n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                pixels[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
