using System;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>Тесты процедурного синтеза: длина, диапазон и форма огибающих.</summary>
    public class AudioSynthTests
    {
        private const int Rate = 44100;

        private static void AssertInRange(float[] data)
        {
            foreach (var s in data)
                Assert.IsTrue(s >= -1.0001f && s <= 1.0001f, $"сэмпл {s} вне [-1,1]");
        }

        private static float Peak(float[] data)
        {
            float m = 0f;
            foreach (var s in data) m = Math.Max(m, Math.Abs(s));
            return m;
        }

        [Test]
        public void Bell_LengthAndRange()
        {
            var b = AudioSynth.Bell(Rate, 196f, 1.9f, 0.14f);
            Assert.AreEqual((int)Math.Round(Rate * 1.9), b.Length);
            AssertInRange(b);
        }

        [Test]
        public void Bell_Decays_TailQuieterThanHead()
        {
            var b = AudioSynth.Bell(Rate, 196f);
            float head = Peak(Slice(b, 0, Rate / 10));            // первые 0.1с
            float tail = Peak(Slice(b, b.Length - Rate / 10, Rate / 10)); // последние 0.1с
            Assert.Greater(head, tail, "колокол должен затухать");
            Assert.Less(tail, 0.02f, "хвост должен быть почти тишиной");
        }

        [Test]
        public void Bell_StartsNearSilence()
        {
            var b = AudioSynth.Bell(Rate, 196f);
            Assert.Less(Math.Abs(b[0]), 0.01f, "атака начинается с тишины");
        }

        [Test]
        public void Tick_ShortAndDecaying()
        {
            var t = AudioSynth.Tick(Rate);
            Assert.AreEqual((int)Math.Round(Rate * 0.05), t.Length);
            AssertInRange(t);
            float head = Peak(Slice(t, 0, t.Length / 4));
            float tail = Peak(Slice(t, t.Length * 3 / 4, t.Length / 4));
            Assert.Greater(head, tail);
        }

        [Test]
        public void Ambient_LengthRangeAndDeterministic()
        {
            var a1 = AudioSynth.Ambient(Rate, 2f, seed: 7);
            var a2 = AudioSynth.Ambient(Rate, 2f, seed: 7);
            Assert.AreEqual((int)Math.Round(Rate * 2.0), a1.Length);
            AssertInRange(a1);
            CollectionAssert.AreEqual(a1, a2, "один seed → одинаковый дрон");
            Assert.Greater(Peak(a1), 0.5f, "дрон должен звучать, а не молчать");
        }

        private static float[] Slice(float[] src, int start, int len)
        {
            start = Math.Max(0, start); len = Math.Min(len, src.Length - start);
            var r = new float[Math.Max(0, len)];
            Array.Copy(src, start, r, 0, r.Length);
            return r;
        }
    }
}
