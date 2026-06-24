using System.Collections.Generic;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>
    /// Тесты данных героев и прогресса сложностей / секретной способности.
    /// Чистый C# (EditMode).
    /// </summary>
    public class HeroAndProgressTests
    {
        // ---------- герои ----------

        [Test]
        public void Heroes_FiveWithFullData()
        {
            Assert.AreEqual(5, GameHeroes.Count);
            foreach (var h in GameHeroes.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(h.id), "пустой id");
                Assert.IsFalse(string.IsNullOrEmpty(h.name), "пустое имя");
                Assert.AreEqual(4, h.skills.Length, $"{h.id}: должно быть 4 навыка");
                Assert.IsNotNull(h.secret, $"{h.id}: нет секретной способности");
                Assert.Greater(h.pros.Length, 0, $"{h.id}: нет плюсов");
                Assert.Greater(h.cons.Length, 0, $"{h.id}: нет минусов");
                Assert.IsFalse(string.IsNullOrEmpty(h.story), $"{h.id}: нет предыстории");
                Assert.IsFalse(string.IsNullOrEmpty(h.arrival), $"{h.id}: нет прибытия");
                Assert.AreEqual("hero_" + h.id, h.portrait);
            }
        }

        [Test]
        public void Heroes_FourSkillKeys()
        {
            Assert.AreEqual(4, GameHeroes.SkillKeys.Length);
            CollectionAssert.AreEqual(new[] { "Q", "E", "R", "F" }, GameHeroes.SkillKeys);
        }

        [Test]
        public void Heroes_Lookup()
        {
            Assert.AreEqual("Тесси", GameHeroes.ById("tessi").name);
            Assert.AreEqual("Тесси", GameHeroes.ById("unknown").name); // фолбэк на первого
            Assert.AreEqual(0, GameHeroes.IndexOf("tessi"));
            Assert.AreEqual(4, GameHeroes.IndexOf("walter"));
            Assert.AreEqual(0, GameHeroes.IndexOf("nope"));
        }

        // ---------- прогресс ----------

        [Test]
        public void Progress_OnlyRealDifficultiesCount()
        {
            Assert.IsTrue(GameProgress.IsReal("easy"));
            Assert.IsTrue(GameProgress.IsReal("nightmare"));
            Assert.IsFalse(GameProgress.IsReal("creative"));
            Assert.IsFalse(GameProgress.IsReal("nope"));
        }

        [Test]
        public void Progress_MarkCleared_IgnoresCreativeAndDuplicates()
        {
            var cleared = new List<string>();
            Assert.IsTrue(GameProgress.MarkCleared(cleared, "easy"));
            Assert.IsFalse(GameProgress.MarkCleared(cleared, "easy"), "повтор не добавляется");
            Assert.IsFalse(GameProgress.MarkCleared(cleared, "creative"), "творческий не считается");
            Assert.AreEqual(1, cleared.Count);
        }

        [Test]
        public void Progress_ClearedCount_CountsRealOnly()
        {
            var cleared = new List<string> { "easy", "normal", "creative", "easy" };
            Assert.AreEqual(2, GameProgress.ClearedCount(cleared));
        }

        [Test]
        public void Progress_SecretUnlocked_NeedsAllFour()
        {
            var cleared = new List<string> { "easy", "normal", "hard" };
            Assert.IsFalse(GameProgress.SecretUnlocked(cleared));
            cleared.Add("nightmare");
            Assert.IsTrue(GameProgress.SecretUnlocked(cleared));
        }

        [Test]
        public void Progress_SecretIgnoresCreativeTowardUnlock()
        {
            var cleared = new List<string> { "easy", "normal", "hard", "creative" };
            Assert.IsFalse(GameProgress.SecretUnlocked(cleared), "творческий не заменяет хард");
        }
    }
}
