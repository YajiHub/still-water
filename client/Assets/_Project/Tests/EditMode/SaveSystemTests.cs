using System;
using System.IO;
using NUnit.Framework;
using StillWater.Core;

namespace StillWater.Tests
{
    // Each test gets its own empty temp folder, so the real save on this machine is never touched.
    public class SaveSystemTests
    {
        private string folder;
        private string SavePath => Path.Combine(folder, SaveSystem.FileName);

        [SetUp]
        public void CreateFolder()
        {
            folder = Path.Combine(Path.GetTempPath(), "stillwater-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void DeleteFolder() => Directory.Delete(folder, true);

        [Test]
        public void Load_NoFile_ReturnsDefaults()
        {
            var data = SaveSystem.Load(folder);
            Assert.AreEqual(1, data.version);
            Assert.IsNotNull(data.levels);
            Assert.AreEqual(0, data.levels.Count);
            Assert.IsNotNull(data.settings);
        }

        [Test]
        public void SaveThenLoad_KeepsEverything()
        {
            var data = new SaveData();
            data.levels.Add(new LevelRecord { id = "level01", cleared = true, bestStars = 2, bestTime = 48.5f });
            data.settings.lookSensitivity = 1.7f;
            data.seenTutorial = true;

            SaveSystem.Save(data, folder);
            var loaded = SaveSystem.Load(folder);

            Assert.AreEqual(1, loaded.levels.Count);
            Assert.AreEqual("level01", loaded.levels[0].id);
            Assert.IsTrue(loaded.levels[0].cleared);
            Assert.AreEqual(2, loaded.levels[0].bestStars);
            Assert.AreEqual(48.5f, loaded.levels[0].bestTime);
            Assert.AreEqual(1.7f, loaded.settings.lookSensitivity);
            Assert.IsTrue(loaded.seenTutorial);
        }

        [Test]
        public void Save_Twice_SecondOverwritesFirst()
        {
            var first = new SaveData();
            first.levels.Add(new LevelRecord { id = "level01", bestStars = 1 });
            SaveSystem.Save(first, folder);

            var second = new SaveData();
            second.levels.Add(new LevelRecord { id = "level01", bestStars = 3 });
            SaveSystem.Save(second, folder);

            Assert.AreEqual(3, SaveSystem.Load(folder).levels[0].bestStars);
        }

        [Test]
        public void Save_LeavesNoTempFileBehind()
        {
            SaveSystem.Save(new SaveData(), folder);
            Assert.IsTrue(File.Exists(SavePath));
            Assert.AreEqual(1, Directory.GetFiles(folder).Length);
        }

        [Test]
        public void Load_CorruptFile_ReturnsDefaultsInsteadOfCrashing()
        {
            File.WriteAllText(SavePath, "{ this is not json");
            var data = SaveSystem.Load(folder);
            Assert.AreEqual(0, data.levels.Count);
        }

        [Test]
        public void Load_EmptyFile_ReturnsDefaults()
        {
            File.WriteAllText(SavePath, "");
            Assert.AreEqual(0, SaveSystem.Load(folder).levels.Count);
        }

        [Test]
        public void Load_UnknownVersion_ReturnsDefaults()
        {
            File.WriteAllText(SavePath, "{\"version\":99,\"levels\":[{\"id\":\"level01\",\"cleared\":true}]}");
            Assert.AreEqual(0, SaveSystem.Load(folder).levels.Count);
        }
    }
}
