using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>
    /// Тесты пути к концовке «Некого» и подбора реплик: тон смещается по поступкам
    /// (темнее/мягче), реплики берутся из пула пути, поддержка/двойные/финал/мультиплеер.
    /// </summary>
    public class NekoPathVoiceTests
    {
        // ---------- путь ----------

        [Test]
        public void Path_DriftWhenNoSignal()
        {
            Assert.AreEqual(NekoPath.Drift, NekoPath.Estimate(new NekoState()));
        }

        [Test]
        public void Path_GoodWhenKindDominates()
        {
            Assert.AreEqual(NekoPath.Good, NekoPath.Estimate(new NekoState { kind = 4, dark = 0 }));
        }

        [Test]
        public void Path_BadWhenCrueltyOrDarkDominates()
        {
            Assert.AreEqual(NekoPath.Bad, NekoPath.Estimate(new NekoState { cruel = 2, dark = 2 }));
            Assert.AreEqual(NekoPath.Bad, NekoPath.Estimate(new NekoState { dark = 7 }));
        }

        [Test]
        public void Path_MiddleWhenMixed()
        {
            Assert.AreEqual(NekoPath.Middle, NekoPath.Estimate(new NekoState { kind = 2, cruel = 1, dark = 1 }));
        }

        [Test]
        public void Darkness_RisesWithDarkAndBadPath()
        {
            float light = NekoPath.DarknessLevel(new NekoState { kind = 4 });
            float dark = NekoPath.DarknessLevel(new NekoState { cruel = 3, dark = 8 });
            Assert.Less(light, dark);
            Assert.GreaterOrEqual(dark, 0f);
            Assert.LessOrEqual(dark, 1f);
        }

        [Test]
        public void RecordChoice_ShiftsTowardGoodOrBad()
        {
            var s = new NekoState();
            for (int i = 0; i < 5; i++) NekoPath.RecordChoice(s, true);
            Assert.AreEqual(NekoPath.Good, s.endingPath);

            var b = new NekoState();
            for (int i = 0; i < 4; i++) NekoPath.RecordChoice(b, false);
            Assert.AreEqual(NekoPath.Bad, b.endingPath);
        }

        // ---------- финал ----------

        [Test]
        public void Final_MatchesPath()
        {
            Assert.AreEqual(NekoLines.FinalGood, NekoPath.Final(new NekoState { kind = 5 }));
            Assert.AreEqual(NekoLines.FinalBad, NekoPath.Final(new NekoState { dark = 9 }));
            Assert.AreEqual(NekoLines.FinalMiddle, NekoPath.Final(new NekoState { kind = 2, cruel = 1 }));
        }

        // ---------- подбор реплик ----------

        [Test]
        public void Spontaneous_NonEmpty_AllPaths()
        {
            var rng = new Random(3);
            foreach (var s in new[]
            {
                new NekoState(),
                new NekoState { kind = 5 },
                new NekoState { dark = 9 },
                new NekoState { kind = 2, cruel = 1, dark = 1 },
            })
                for (int i = 0; i < 20; i++)
                    Assert.IsFalse(string.IsNullOrEmpty(NekoVoice.Spontaneous(s, null, rng)));
        }

        [Test]
        public void Support_LightForGood_SadForBad()
        {
            var rng = new Random(5);
            var good = new List<string>(NekoLines.SupportLight);
            var sad = new List<string>(NekoLines.SupportSad);
            for (int i = 0; i < 30; i++)
            {
                Assert.Contains(NekoVoice.Support(new NekoState { kind = 5 }, null, rng), good);
                Assert.Contains(NekoVoice.Support(new NekoState { dark = 9 }, null, rng), sad);
            }
        }

        [Test]
        public void Multiplayer_NeverEmpty_AndTeamWhenGood()
        {
            var rng = new Random(6);
            var warm = new List<string>(NekoLines.MpWarm);
            for (int i = 0; i < 30; i++)
            {
                string g = NekoVoice.Multiplayer(new NekoState { kind = 5 }, null, rng, 2);
                Assert.Contains(g, warm);
                Assert.IsFalse(string.IsNullOrEmpty(NekoVoice.Multiplayer(new NekoState { dark = 9 }, null, rng, 2)));
            }
        }

        [Test]
        public void TryDouble_ReturnsValidPairWhenTrue()
        {
            var rng = new Random(2);
            int hits = 0;
            for (int i = 0; i < 200; i++)
            {
                if (NekoVoice.TryDouble(new NekoState { dark = 4 }, rng, out string a, out string b))
                {
                    hits++;
                    Assert.IsFalse(string.IsNullOrEmpty(a));
                    Assert.IsFalse(string.IsNullOrEmpty(b));
                }
            }
            Assert.Greater(hits, 0, "двойные реплики должны иногда выпадать");
        }

        [Test]
        public void Spontaneous_AvoidsRecent()
        {
            var rng = new Random(9);
            var recent = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                string line = NekoVoice.Spontaneous(new NekoState { dark = 9 }, recent, rng);
                recent.Add(line);
            }
            // при непустом «недавнем» подряд одинаковую не возвращает (пока есть свежие)
            for (int i = 0; i < 20; i++)
            {
                string line = NekoVoice.Spontaneous(new NekoState { dark = 9 }, recent, rng);
                Assert.IsFalse(recent.Contains(line) && recent.Count < NekoLines.Bad.Length);
            }
        }
    }
}
