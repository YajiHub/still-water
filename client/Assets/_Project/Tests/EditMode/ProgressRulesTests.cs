using System.Collections.Generic;
using NUnit.Framework;
using StillWater.Core;

namespace StillWater.Tests
{
    public class ProgressRulesTests
    {
        private static readonly string[] Order = { "level01", "level02", "level03" };

        private static List<LevelRecord> Records(params LevelRecord[] records) => new List<LevelRecord>(records);
        private static LevelRecord Cleared(string id) => new LevelRecord { id = id, cleared = true, bestStars = 1, bestTime = 50f };

        [Test] public void IsUnlocked_FirstLevel_AlwaysOpen() =>
            Assert.IsTrue(ProgressRules.IsUnlocked("level01", Order, Records()));

        [Test] public void IsUnlocked_SecondLevel_LockedWhenFirstNotCleared() =>
            Assert.IsFalse(ProgressRules.IsUnlocked("level02", Order, Records()));

        [Test] public void IsUnlocked_SecondLevel_OpenWhenFirstCleared() =>
            Assert.IsTrue(ProgressRules.IsUnlocked("level02", Order, Records(Cleared("level01"))));

        [Test] public void IsUnlocked_ThirdLevel_NeedsSecondNotJustFirst() =>
            Assert.IsFalse(ProgressRules.IsUnlocked("level03", Order, Records(Cleared("level01"))));

        [Test] public void IsUnlocked_PlayedButNotCleared_StaysLocked() =>
            Assert.IsFalse(ProgressRules.IsUnlocked("level02", Order, Records(new LevelRecord { id = "level01", cleared = false })));

        [Test] public void IsUnlocked_UnknownLevel_Locked() =>
            Assert.IsFalse(ProgressRules.IsUnlocked("level09", Order, Records(Cleared("level01"))));

        [Test]
        public void MergeBest_FirstClear_RecordsEverything()
        {
            var r = ProgressRules.MergeBest(null, "level01", 2, 48f);
            Assert.AreEqual("level01", r.id);
            Assert.IsTrue(r.cleared);
            Assert.AreEqual(2, r.bestStars);
            Assert.AreEqual(48f, r.bestTime);
        }

        [Test]
        public void MergeBest_WorseReplay_KeepsOldBests()
        {
            var r = ProgressRules.MergeBest(new LevelRecord { id = "level01", cleared = true, bestStars = 3, bestTime = 40f }, "level01", 1, 70f);
            Assert.AreEqual(3, r.bestStars);
            Assert.AreEqual(40f, r.bestTime);
        }

        [Test]
        public void MergeBest_BetterReplay_TakesNewBests()
        {
            var r = ProgressRules.MergeBest(new LevelRecord { id = "level01", cleared = true, bestStars = 1, bestTime = 70f }, "level01", 3, 40f);
            Assert.AreEqual(3, r.bestStars);
            Assert.AreEqual(40f, r.bestTime);
        }

        [Test]
        public void MergeBest_PlayedButNeverCleared_ZeroTimeMeansNoTime()
        {
            // Lost before (record exists, bestTime 0): the first real time must win, not the 0.
            var r = ProgressRules.MergeBest(new LevelRecord { id = "level01", cleared = false }, "level01", 1, 55f);
            Assert.IsTrue(r.cleared);
            Assert.AreEqual(55f, r.bestTime);
        }

        [Test] public void Stars_TwoSplashes_One() => Assert.AreEqual(1, ProgressRules.Stars(2, 30f, 60f));
        [Test] public void Stars_OneSplash_Two() => Assert.AreEqual(2, ProgressRules.Stars(1, 30f, 60f));
        [Test] public void Stars_NoSplashUnderPar_Three() => Assert.AreEqual(3, ProgressRules.Stars(0, 30f, 60f));
        [Test] public void Stars_NoSplashOverPar_Two() => Assert.AreEqual(2, ProgressRules.Stars(0, 90f, 60f));
        [Test] public void Stars_NoSplashExactlyPar_Three() => Assert.AreEqual(3, ProgressRules.Stars(0, 60f, 60f));
    }
}
