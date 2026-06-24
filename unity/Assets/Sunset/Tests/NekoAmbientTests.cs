using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>Тесты присутствия «Некого» в игре: тон, триггеры, анти-повтор, паузы.</summary>
    public class NekoAmbientTests
    {
        [Test]
        public void Spontaneous_NonEmpty_ByTone()
        {
            var rng = new Random(1);
            foreach (var s in new[] { new NekoState { trust = -3 }, new NekoState(), new NekoState { trust = 4 } })
                Assert.IsFalse(string.IsNullOrEmpty(NekoAmbient.Spontaneous(s, null, rng)));
        }

        [Test]
        public void ForTrigger_KnownAndUnknownKeys()
        {
            var rng = new Random(2);
            foreach (var key in NekoAmbient.TriggerKeys)
                Assert.IsFalse(string.IsNullOrEmpty(NekoAmbient.ForTrigger(key, null, rng)), key);
            Assert.IsNull(NekoAmbient.ForTrigger("no_such_key", null, rng));
            Assert.IsNull(NekoAmbient.ForTrigger(null, null, rng));
        }

        [Test]
        public void HasExpectedTriggerKeys()
        {
            CollectionAssert.Contains(new List<string>(NekoAmbient.TriggerKeys), "night");
            CollectionAssert.Contains(new List<string>(NekoAmbient.TriggerKeys), "boss");
        }

        [Test]
        public void PickFresh_AvoidsRecentWhenPossible()
        {
            // забьём «недавними» все реплики триггера, кроме одной — должна вернуться она
            var rng = new Random(3);
            var all = new List<string>();
            for (int i = 0; i < 50; i++) all.Add(NekoAmbient.ForTrigger("night", null, rng));
            var distinct = new List<string>();
            foreach (var x in all) if (!distinct.Contains(x)) distinct.Add(x);
            Assert.GreaterOrEqual(distinct.Count, 2, "у триггера должно быть несколько вариантов");

            var recent = new List<string> { distinct[0] };
            for (int i = 0; i < 20; i++)
            {
                string got = NekoAmbient.ForTrigger("night", recent, rng);
                Assert.AreNotEqual(distinct[0], got, "не должен повторять недавнюю, пока есть свежие");
            }
        }

        [Test]
        public void Delays_AreWithinConfiguredBounds()
        {
            var rng = new Random(4);
            for (int i = 0; i < 100; i++)
            {
                float d = NekoAmbient.NextSpontaneousDelay(rng);
                Assert.GreaterOrEqual(d, NekoAmbient.SpontaneousMin);
                Assert.LessOrEqual(d, NekoAmbient.SpontaneousMax);
                float f = NekoAmbient.FirstDelay(rng);
                Assert.GreaterOrEqual(f, NekoAmbient.FirstDelayMin);
                Assert.LessOrEqual(f, NekoAmbient.FirstDelayMax);
            }
        }

        [Test]
        public void Spontaneous_IsRare_LongPauses()
        {
            // дизайн: «Некий» не назойлив — паузы крупные (минимум хотя бы минута)
            Assert.GreaterOrEqual(NekoAmbient.SpontaneousMin, 60f);
        }
    }
}
