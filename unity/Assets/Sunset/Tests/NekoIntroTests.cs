using System;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>Тесты скриптового интро «Некого»: тон, глитч, подглядывание, «дальше».</summary>
    public class NekoIntroTests
    {
        [Test]
        public void Tone_ByTrustAndDark()
        {
            Assert.AreEqual("cold", NekoIntro.Tone(new NekoState { trust = -2 }));
            Assert.AreEqual("cold", NekoIntro.Tone(new NekoState { trust = 1, dark = 6 }));
            Assert.AreEqual("warm", NekoIntro.Tone(new NekoState { trust = 3, dark = 0 }));
            Assert.AreEqual("neutral", NekoIntro.Tone(new NekoState { trust = 1, dark = 0 }));
        }

        [Test]
        public void Greeting_SubstitutesName_FromCorrectPool()
        {
            var warm = new NekoState { knownName = "Тесси", trust = 4 };
            string g = NekoIntro.Greeting(warm, new Random(1));
            StringAssert.DoesNotContain("{n}", g);
            // тёплая реплика должна прийти из тёплого пула
            CollectionAssert.Contains(System.Array.ConvertAll(NekoIntro.GreetWarm, x => x.Replace("{n}", "Тесси")), g);
        }

        [Test]
        public void ShouldGlitch_NeedsRepeatVisits()
        {
            // первый визит — никогда
            Assert.IsFalse(NekoIntro.ShouldGlitch(new NekoState { visits = 1, trust = 0 }, new Random(1)));
        }

        [Test]
        public void Glitch_PicksWrongNameAndCorrects()
        {
            var s = new NekoState { knownName = "Тесси", visits = 3 };
            NekoIntro.Glitch(s, new Random(5), out string wrong, out string corr);
            Assert.AreNotEqual("Тесси", wrong);
            StringAssert.Contains("Тесси", corr);
            StringAssert.DoesNotContain("{n}", corr);
        }

        [Test]
        public void AwayLine_PeekAndDays()
        {
            Assert.IsNull(NekoIntro.AwayLine("played", 1000));
            StringAssert.Contains("заглянул", NekoIntro.AwayLine("peek", 1000));
            string d = NekoIntro.AwayLine("played", MenuPresence.DayMs * 3 + 5);
            StringAssert.Contains("3 дня", d);
        }

        [Test]
        public void Plural_RussianForms()
        {
            Assert.AreEqual("день", NekoIntro.Plural(1, "день", "дня", "дней"));
            Assert.AreEqual("дня", NekoIntro.Plural(3, "день", "дня", "дней"));
            Assert.AreEqual("дней", NekoIntro.Plural(5, "день", "дня", "дней"));
            Assert.AreEqual("дней", NekoIntro.Plural(11, "день", "дня", "дней"));
            Assert.AreEqual("дня", NekoIntro.Plural(22, "день", "дня", "дней"));
        }

        [Test]
        public void PeekLines_BuildFromProbe()
        {
            var s = new ScanInput
            {
                now = new DateTime(2026, 6, 24, 2, 7, 0),
                city = "Moscow", region = "Europe", os = "Windows",
            };
            var lines = NekoIntro.PeekLines(s);
            StringAssert.Contains("вижу", lines[0]);
            CollectionAssert.Contains(lines, "Часовой пояс — Europe/Moscow.");
            // 02:07 → «Глубокая ночь»
            Assert.IsTrue(lines.Exists(l => l.Contains("02:07") && l.Contains("Глубокая ночь")));
            CollectionAssert.Contains(lines, "И ты пришёл с Windows.");
        }

        [TestCase("дальше", true)]
        [TestCase("Дальше!", true)]
        [TestCase("давай начнём", true)]
        [TestCase("начнём уже", true)]
        [TestCase("го", true)]
        [TestCase("поехали", true)]
        [TestCase("расскажи про воду", false)]
        [TestCase("", false)]
        public void WantsAdvance_Detects(string input, bool expected)
        {
            Assert.AreEqual(expected, NekoIntro.WantsAdvance(input), $"«{input}»");
        }
    }
}
