using UnityEditor;

namespace StillWater.Editor
{
    /// <summary>
    /// Import rules for the Listener's files in Art/Listener: our own materials (none from the FBX), looping clips by
    /// name ("*_Loop"), normal maps marked as normal maps. Code instead of Inspector clicks, so a re-import can't lose them.
    /// The custom Listener (Listener_Custom.fbx, built by art/listener/build_listener.py) is a Generic rig: an eel tail,
    /// four arms and a splitting head don't fit Humanoid. The Quaternius files (Listener Asset Source) stay Humanoid.
    /// </summary>
    public class ListenerImport : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Art/Listener/";
        public const string CustomModel = "Listener_Custom";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            bool custom = assetPath.Contains(CustomModel);
            importer.animationType = custom ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = custom || !assetPath.Contains("FullBody"); // the Quaternius body has no clips; the libraries do
        }

        private void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips) clip.loopTime = clip.name.EndsWith("_Loop"); // loops are named "*_Loop"
            importer.clipAnimations = clips;
        }

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.Contains("_Normal")) return;
            ((TextureImporter)assetImporter).textureType = TextureImporterType.NormalMap;
        }
    }
}
