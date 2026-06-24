using System;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>
    /// Тесты «досье» игрока и генератора «красного кода». Чистый C# (EditMode).
    /// </summary>
    public class ScanAndCodeTests
    {
        private static ScanInput Sample() => new ScanInput
        {
            name = "Тесси",
            now = new DateTime(2026, 6, 24, 9, 5, 7),
            city = "Moscow",
            region = "Europe",
            os = "Windows",
            device = "Desktop",
            entry = "напрямую",
            screenW = 1920,
            screenH = 1080,
            lang = "Russian",
            cpuThreads = 8,
        };

        // ---------- досье ----------

        [Test]
        public void Profile_HasKeyRowsAndFormatsTime()
        {
            var rows = PlayerScan.Profile(Sample());
            string Get(string k) { foreach (var r in rows) if (r.Key == k) return r.Value; return null; }
            Assert.AreEqual("Тесси", Get("имя"));
            Assert.AreEqual("09:05:07", Get("время"));
            Assert.AreEqual("Moscow · Europe", Get("откуда"));
            Assert.AreEqual("напрямую", Get("вход"));
            Assert.AreEqual("Windows · Desktop", Get("система"));
            Assert.AreEqual("1920×1080", Get("экран"));
        }

        [Test]
        public void Profile_NoName_SaysNotNamed()
        {
            var s = Sample(); s.name = null;
            var rows = PlayerScan.Profile(s);
            foreach (var r in rows) if (r.Key == "имя") Assert.AreEqual("не назвал", r.Value);
        }

        [Test]
        public void Profile_OmitsMissingOptionalRows()
        {
            var s = new ScanInput { now = DateTime.Now }; // только время гарантированно
            var rows = PlayerScan.Profile(s);
            bool hasScreen = false, hasLang = false, hasCores = false;
            foreach (var r in rows)
            {
                if (r.Key == "экран") hasScreen = true;
                if (r.Key == "язык") hasLang = true;
                if (r.Key == "ядра") hasCores = true;
            }
            Assert.IsFalse(hasScreen); Assert.IsFalse(hasLang); Assert.IsFalse(hasCores);
        }

        [Test]
        public void Hits_IncludePersonalFragmentsWhenNamed()
        {
            var hits = PlayerScan.Hits(Sample());
            CollectionAssert.Contains(hits, "soul.bind(\"Тесси\")");
            CollectionAssert.Contains(hits, "who_are_you :: Тесси");
            CollectionAssert.Contains(hits, "mem.scan(os)=Windows");
            CollectionAssert.Contains(hits, "rift.open(target=YOU)");
        }

        [Test]
        public void Readout_HasHeaderAndFooter()
        {
            string txt = PlayerScan.ReadoutText(Sample());
            StringAssert.Contains("СКАНИРОВАНИЕ ОБЪЕКТА", txt);
            StringAssert.Contains("ИДЕНТИФИКАЦИЯ ЗАВЕРШЕНА", txt);
            StringAssert.Contains("Тесси", txt);
        }

        // ---------- триггеры скана ----------

        [TestCase("ты меня видишь?", true)]
        [TestCase("видишь меня вообще", true)]
        [TestCase("кто я такой", true)]
        [TestCase("откуда я", true)]
        [TestCase("который час", true)]
        [TestCase("что ты обо мне знаешь", true)]
        [TestCase("ты за мной следишь", true)]
        [TestCase("какая у меня система", true)]
        [TestCase("где взять воду", false)]
        [TestCase("расскажи про острова", false)]
        [TestCase("", false)]
        public void WantsScan_DetectsMoments(string input, bool expected)
        {
            Assert.AreEqual(expected, PlayerScan.WantsScan(input), $"«{input}»");
        }

        // ---------- генератор кода ----------

        [Test]
        public void CodeLine_HasExactWidth()
        {
            var s = new CodeStream(new Random(1));
            for (int w = 1; w <= 120; w += 7)
                Assert.AreEqual(w, s.Line(w).Length, $"ширина {w}");
        }

        [Test]
        public void CodeRnd_UsesOnlyGlyphAlphabet()
        {
            var s = new CodeStream(new Random(3));
            string r = s.Rnd(200);
            Assert.AreEqual(200, r.Length);
            foreach (char ch in r)
                Assert.IsTrue(CodeStream.Glyphs.IndexOf(ch) >= 0, $"символ '{ch}' вне алфавита");
        }

        [Test]
        public void CodeStream_DeterministicWithSeed()
        {
            var a = new CodeStream(new Random(7));
            var b = new CodeStream(new Random(7));
            Assert.AreEqual(a.Line(64), b.Line(64));
            Assert.AreEqual(a.Segment(), b.Segment());
        }

        [Test]
        public void CodeLine_ZeroWidthIsEmpty()
        {
            Assert.AreEqual("", new CodeStream(new Random(1)).Line(0));
        }
    }
}
