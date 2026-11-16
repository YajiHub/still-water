using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StillWater.Level;
using UnityEngine;

namespace StillWater.Tests
{
    /// <summary>
    /// The online circuit's Listeners must be exactly as dangerous as the solo levels'. The server reads
    /// server/src/logic/listenerTuning.json and the solo game reads each LevelConfig, so this compares them field by
    /// field. (On 2026-11-16 they had drifted: online used Level 1's speed in every room, 8 m/s instead of up to 20.)
    /// </summary>
    public class ListenerTuningParityTests
    {
        private static string TuningPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../server/src/logic/listenerTuning.json"));

        [Test]
        public void OnlineListenersMatchTheSoloLevels()
        {
            if (!File.Exists(TuningPath)) Assert.Ignore($"No server checkout next to the client ({TuningPath}).");
            string json = File.ReadAllText(TuningPath);
            var catalog = Resources.Load<LevelCatalog>("LevelCatalog");
            Assert.IsNotNull(catalog, "Resources/LevelCatalog");
            foreach (var level in catalog.levels)
            {
                if (level == null) continue;
                var block = Regex.Match(json, "\"" + Regex.Escape(level.id) + "\"\\s*:\\s*\\{([^}]*)\\}");
                Assert.IsTrue(block.Success, $"listenerTuning.json has no entry for {level.id}");
                string b = block.Groups[1].Value;
                Assert.AreEqual(level.listenerRoamSpeed, Field(b, "roamSpeed"), 1e-4f, $"{level.id} roamSpeed");
                Assert.AreEqual(level.listenerRoamPause, Field(b, "roamPauseSeconds"), 1e-4f, $"{level.id} roamPauseSeconds");
                Assert.AreEqual(level.listenerAlertDelay, Field(b, "alertDelaySeconds"), 1e-4f, $"{level.id} alertDelaySeconds");
                Assert.AreEqual(level.listenerHuntSpeed, Field(b, "huntSpeed"), 1e-4f, $"{level.id} huntSpeed");
                Assert.AreEqual(level.listenerSpeedPerSplash, Field(b, "speedPerSplash"), 1e-4f, $"{level.id} speedPerSplash");
                Assert.AreEqual(level.listenerLingerSeconds, Field(b, "lingerSeconds"), 1e-4f, $"{level.id} lingerSeconds");
                Assert.AreEqual(level.listenerCatchDistance, Field(b, "catchDistance"), 1e-4f, $"{level.id} catchDistance");
            }
        }

        private static float Field(string block, string name)
        {
            var m = Regex.Match(block, "\"" + name + "\"\\s*:\\s*(-?[0-9.]+)");
            Assert.IsTrue(m.Success, $"missing {name}");
            return float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }
    }
}
