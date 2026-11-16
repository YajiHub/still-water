using System.IO;
using UnityEditor;
using UnityEngine;

namespace StillWater.Editor
{
    /// <summary>
    /// Water and Hall Look: ambientCG Tiles020 (CC0) on Tile.mat (pool floor, platforms) and Wall.mat (walls,
    /// ceiling), shared by every level; plus the lamp fixture materials in Resources/Hall for
    /// <see cref="StillWater.Level.HallDressing"/>. Build Level only creates these materials when missing, so it
    /// never undoes this. Re-run any time.
    /// </summary>
    public static class HallBuilder
    {
        private const string Dir = "Assets/_Project/Art/Hall";
        private const string Materials = "Assets/_Project/Art/GreyBox";
        private const string ResourcesDir = "Assets/_Project/Resources/Hall";

        [MenuItem("Still Water/Art Spike/Set Up Hall")]
        public static void SetUp()
        {
            var color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/Tiles020_1K-JPG_Color.jpg");
            string normalPath = $"{Dir}/Tiles020_1K-JPG_NormalGL.jpg"; // GL = Unity's normal-map convention
            var importer = (TextureImporter)AssetImporter.GetAtPath(normalPath);
            if (color == null || importer == null) { Debug.LogError($"Set Up Hall: Tiles020 textures missing in {Dir}."); return; }
            if (importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            Tiled($"{Materials}/Tile.mat", color, normal, Color.white, 0.6f);                     // glazed pool tiles
            Tiled($"{Materials}/Wall.mat", color, normal, new Color(0.75f, 0.8f, 0.8f), 0.5f);    // a shade darker

            // Special edges (crumbling / one-way / ferry): a soft tint below Look C's bloom threshold (0.8), not neon
            // next to the plain cyan edges (user, 2026-11-07). Same values as LevelBuilder's defaults.
            Emission("GlowCrumble", new Color(1f, 0.92f, 0.5f) * 0.2f);
            Emission("GlowOneway", new Color(0.9f, 0.2f, 0.15f) * 0.2f);
            Emission("GlowFerry", new Color(1f, 0.7f, 0.35f) * 0.2f);

            Directory.CreateDirectory(ResourcesDir);
            Lit($"{ResourcesDir}/LampShade.mat", Color.white, 0.3f, new Color(0.9f, 0.95f, 1f) * 2.5f); // cold, blooms in Look C
            Lit($"{ResourcesDir}/LampCable.mat", new Color(0.05f, 0.05f, 0.05f), 0.2f, null);
            AssetDatabase.SaveAssets();
            Debug.Log("Hall set up: pool tiles on Tile/Wall materials, softer special edge glows (every level), lamp fixture materials in Resources/Hall. Press Play.");
        }

        private static void Tiled(string path, Texture2D color, Texture2D normal, Color tint, float smoothness)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { Debug.LogError($"Set Up Hall: {path} missing: run Build Level once first."); return; }
            mat.SetTexture("_BaseMap", color);
            mat.SetColor("_BaseColor", tint);
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
        }

        private static void Emission(string name, Color emission)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/{name}.mat");
            if (mat == null) return; // a level without that tile type was never built: LevelBuilder's default matches
            mat.SetColor("_EmissionColor", emission);
            EditorUtility.SetDirty(mat);
        }

        private static void Lit(string path, Color color, float smoothness, Color? emission)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission.Value);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);
        }
    }
}
