using UnityEditor;

namespace StillWater.Editor
{
    /// <summary>
    /// Import rules for the Listener's Quaternius files in Art/Listener (Listener Asset Source): Humanoid rig so the
    /// animation library retargets onto the body, our own materials (none from the FBX), looping clips by name,
    /// normal maps marked as normal maps. Code instead of Inspector clicks, so a re-import can't lose them.
    /// </summary>
    public class ListenerImport : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Art/Listener/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = !assetPath.Contains("FullBody"); // the body has no clips; the libraries do
        }

        private void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips) clip.loopTime = clip.name.EndsWith("_Loop"); // Quaternius names loops "*_Loop"
            importer.clipAnimations = clips;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.Contains("_Normal")) return;
            ((TextureImporter)assetImporter).textureType = TextureImporterType.NormalMap;
        }
    }
}
