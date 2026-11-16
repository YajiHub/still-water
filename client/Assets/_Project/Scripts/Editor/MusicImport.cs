using UnityEditor;
using UnityEngine;

namespace StillWater.Editor
{
    /// <summary>
    /// Import rules for Resources/Music (art/music/compose.py). The long loops stream from disk, so a phone doesn't keep
    /// minutes of decoded audio in memory. The short stingers stay compressed in memory so they start instantly.
    /// All are Vorbis.
    /// </summary>
    public class MusicImport : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/_Project/Resources/Music/")) return;
            var importer = (AudioImporter)assetImporter;
            bool stinger = assetPath.Contains("stinger_") || assetPath.Contains("sfx_");
            var settings = importer.defaultSampleSettings;
            settings.loadType = stinger ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = !stinger;
        }
    }
}
