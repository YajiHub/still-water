using System.IO;
using NUnit.Framework;
using StillWater.Level;
using UnityEngine;

namespace StillWater.Tests
{
    // Reads the same file the server validates, so a field renamed on one side turns this red.
    public class LevelLayoutTests
    {
        private static LevelLayout LoadLevel01()
        {
            string path = Path.Combine(Application.dataPath, "../../server/src/levels/level01.json");
            return LevelLayout.FromJson(File.ReadAllText(path));
        }

        [Test]
        public void Level03_FerriesParse_OthersDontMove()
        {
            string path = Path.Combine(Application.dataPath, "../../server/src/levels/level03.json");
            var layout = LevelLayout.FromJson(File.ReadAllText(path));
            var ferries = System.Array.FindAll(layout.platforms, p => p.Moves);
            CollectionAssert.AreEqual(new[] { 13, 39, 62, 95 }, System.Array.ConvertAll(ferries, p => p.id));
            Assert.AreEqual(2f, ferries[0].move.seconds);
            Assert.AreEqual(0.5f, ferries[0].move.pause);
            Assert.IsFalse(System.Array.Exists(LoadLevel01().platforms, p => p.Moves), "a missing move must not count as moving");
        }

        [Test]
        public void Level01_ParsesEveryField()
        {
            var layout = LoadLevel01();
            Assert.AreEqual("level01", layout.id);
            Assert.AreEqual(24, layout.platforms.Length);
            Assert.AreEqual(25.5f, layout.water.maxZ);
            Assert.AreEqual(0.5f, layout.platformTopY);
            Assert.AreEqual(1.1f, layout.exitTopY);
            Assert.AreEqual(30f, layout.start.width);
            Assert.AreEqual(27.5f, layout.exit.z);
            CollectionAssert.AreEqual(new[] { 2, 6, 11, 15, 20, 24 }, layout.safePath);
        }

        [Test]
        public void Level01_PlatformsHaveIdsAndSizes()
        {
            foreach (var p in LoadLevel01().platforms)
            {
                Assert.Greater(p.id, 0);
                Assert.Greater(p.size, 0f);
            }
        }
    }
}
