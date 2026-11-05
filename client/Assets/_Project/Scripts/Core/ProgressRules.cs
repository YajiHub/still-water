using System;
using System.Collections.Generic;

namespace StillWater.Core
{
    /// <summary>
    /// Unlocks, best results and stars (Core Loop Rules + Save Data decisions) as pure functions,
    /// so EditMode tests can check them without files or scenes.
    /// </summary>
    public static class ProgressRules
    {
        /// <summary>
        /// A level is open if it's the first one in <paramref name="levelOrder"/>,
        /// or the level just before it has a record with cleared = true.
        /// A level id that isn't in the order is never open.
        /// </summary>
        public static bool IsUnlocked(string levelId, IReadOnlyList<string> levelOrder, IReadOnlyList<LevelRecord> records)
        {
            // IReadOnlyList has no IndexOf, so find the position with a loop.
            int index = -1;
            for (int i = 0; i < levelOrder.Count; i++)
            {
                if (levelOrder[i] == levelId)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1) return false;
            if (index == 0) return true;
            var previousRecord = FindRecord(records, levelOrder[index - 1]);
            return previousRecord != null && previousRecord.cleared;
        }

        /// <summary>
        /// The record after a win: cleared, the higher star count, and the faster time.
        /// <paramref name="old"/> is null if the level was never played. Never lowers a best.
        /// </summary>
        public static LevelRecord MergeBest(LevelRecord old, string levelId, int stars, float time)
        {
            if (old == null)
                return new LevelRecord { id = levelId, cleared = true, bestStars = stars, bestTime = time };

            return new LevelRecord
            {
                id = levelId,
                cleared = true,
                bestStars = Math.Max(old.bestStars, stars),
                bestTime = old.bestTime == 0f ? time : Math.Min(old.bestTime, time), // 0 = "no time yet"
            };
        }

        /// <summary>
        /// Stars for a win: 1 for clearing, +1 if at most 1 splash, +1 if 0 splashes AND time ≤ par.
        /// </summary>
        public static int Stars(int splashes, float time, float parTime)
        {
            int stars = 1;
            if (splashes <= 1) stars++;
            if (splashes == 0 && time <= parTime) stars++;
            return stars;
        }

        /// <summary>The record with this id, or null. (Already written: use it in IsUnlocked.)</summary>
        public static LevelRecord FindRecord(IReadOnlyList<LevelRecord> records, string levelId)
        {
            foreach (var r in records)
                if (r.id == levelId) return r;
            return null;
        }
    }
}
