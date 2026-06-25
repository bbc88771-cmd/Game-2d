using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>
    /// Тесты логики главного меню: присутствие «Некого», настройки, дневник и
    /// данные сложностей. Всё — чистый C# без зависимостей от сцены (EditMode).
    /// </summary>
    public class MenuTests
    {
        // ---------- присутствие «Некого» ----------

        [Test]
        public void Greeting_NoName_FirstVisit_IsSilent()
        {
            var s = new NekoState { knownName = null, visits = 1 };
            Assert.IsNull(MenuPresence.Greeting(s, null, 0));
        }

        [Test]
        public void Greeting_NoName_Returning_NudgesToEnter()
        {
            var s = new NekoState { knownName = null, visits = 2 };
            Assert.AreEqual("Ты опять здесь. Но так и не зашёл.", MenuPresence.Greeting(s, null, 0));
        }

        [Test]
        public void Greeting_PeekExit_BeatsEverything()
        {
            var s = new NekoState { knownName = "Тесси", visits = 5 };
            // даже при долгом отсутствии "peek" имеет приоритет (как в веб-версии)
            var line = MenuPresence.Greeting(s, "peek", MenuPresence.DayMs * 3);
            StringAssert.Contains("заглянул на пару секунд", line);
            StringAssert.Contains("Тесси", line);
        }

        [Test]
        public void Greeting_LongAway_CountsDays()
        {
            var s = new NekoState { knownName = "Тесси", visits = 5 };
            var line = MenuPresence.Greeting(s, "played", MenuPresence.DayMs + 1);
            StringAssert.Contains("давно не было", line);
        }

        [Test]
        public void Greeting_RecentReturn_SaysAgain()
        {
            var s = new NekoState { knownName = "Тесси", visits = 2 };
            Assert.AreEqual("Снова ты, Тесси.", MenuPresence.Greeting(s, "played", 1000));
        }

        [Test]
        public void Greeting_KnownName_FirstVisit_IsSilent()
        {
            var s = new NekoState { knownName = "Тесси", visits = 1 };
            Assert.IsNull(MenuPresence.Greeting(s, "played", 1000));
        }

        [Test]
        public void DwellLine_DependsOnName()
        {
            Assert.AreEqual("Ты просто стоишь здесь. Я вижу.",
                MenuPresence.DwellLine(new NekoState { knownName = null }));
            Assert.AreEqual("Чего ты ждёшь, Тесси?",
                MenuPresence.DwellLine(new NekoState { knownName = "Тесси" }));
        }

        // ---------- настройки ----------

        [Test]
        public void Settings_Defaults()
        {
            var s = new GameSettings();
            Assert.AreEqual(70, s.music);
            Assert.AreEqual(80, s.sfx);
            Assert.AreEqual("ru", s.lang);
            Assert.AreEqual("normal", s.difficulty);
            Assert.AreEqual("16", s.rating);
            Assert.IsFalse(s.fullscreen);
        }

        [Test]
        public void Settings_Clamp_FixesOutOfRange()
        {
            var s = new GameSettings { music = 250, sfx = -30, lang = "xx", difficulty = "wtf", rating = "21" };
            s.Clamp();
            Assert.AreEqual(100, s.music);
            Assert.AreEqual(0, s.sfx);
            Assert.AreEqual("ru", s.lang);
            Assert.AreEqual("normal", s.difficulty);
            Assert.AreEqual("16", s.rating);
        }

        [Test]
        public void Settings_Clamp_KeepsValidValues()
        {
            var s = new GameSettings { music = 33, sfx = 66, lang = "en", difficulty = "hard", rating = "18" };
            s.Clamp();
            Assert.AreEqual(33, s.music);
            Assert.AreEqual(66, s.sfx);
            Assert.AreEqual("en", s.lang);
            Assert.AreEqual("hard", s.difficulty);
            Assert.AreEqual("18", s.rating);
        }

        [Test]
        public void Settings_Clone_IsIndependent()
        {
            var s = new GameSettings { music = 10 };
            var c = s.Clone();
            c.music = 90;
            Assert.AreEqual(10, s.music);
            Assert.AreEqual(90, c.music);
        }

        // ---------- сложности ----------

        [Test]
        public void Difficulties_FiveAndLookup()
        {
            Assert.AreEqual(5, GameDifficulties.Count);
            Assert.AreEqual("Обычный", GameDifficulties.NameOf("normal"));
            Assert.AreEqual("—", GameDifficulties.NameOf("nope"));
            Assert.IsTrue(GameDifficulties.IsValid("nightmare"));
            Assert.IsFalse(GameDifficulties.IsValid("nope"));
        }
        // дневник вынесен в Diary — см. DiaryTests.
    }
}
