using UnityEditor;

namespace StillWater.Editor
{
    /// <summary>
    /// Import rules for Art/Player (Quaternius Universal Base Characters, CC0). The two bodies are Humanoid, so the
    /// Universal Animation Library clips in Art/Listener retarget onto them. The hairstyles ("rigged to head bone") keep
    /// their skinned meshes; PlayerBodyBuilder re-binds them to the body's bones. We set our own materials, none from the
    /// FBX. Normal maps are marked as normal maps.
    /// </summary>
    public class PlayerImport : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Art/Player/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            bool body = assetPath.Contains("FullBody");
            importer.animationType = body ? ModelImporterAnimationType.Human : ModelImporterAnimationType.None;
            if (body) importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.Contains("_Normal")) return;
            ((TextureImporter)assetImporter).textureType = TextureImporterType.NormalMap;
        }
    }
}
