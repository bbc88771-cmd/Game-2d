using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>
    /// Тесты данных вступительной катсцены. CutsceneData — чистые данные без
    /// зависимостей от сцены, поэтому проверяем в EditMode: число слайдов, что у
    /// каждого есть биты, корректность «голоса Некого» на финальном слайде и
    /// распределение длительностей по числу символов.
    /// </summary>
    public class CutsceneDataTests
    {
        [Test]
        public void HasFourSlides()
        {
            Assert.AreEqual(4, CutsceneData.Count);
            Assert.AreEqual(4, CutsceneData.FallbackDurationsMs.Length);
        }

        [Test]
        public void EverySlideHasBeats()
        {
            foreach (var s in CutsceneData.Slides)
            {
                Assert.IsNotNull(s.beats, "у слайда нет битов");
                Assert.Greater(s.beats.Length, 0, "слайд без текстовых битов");
                foreach (var b in s.beats)
                    Assert.IsFalse(string.IsNullOrWhiteSpace(b), "пустой бит");
            }
        }

        [Test]
        public void OnlyFinalSlideIsNekoVoice()
        {
            for (int i = 0; i < CutsceneData.Count; i++)
            {
                bool expected = i == CutsceneData.Count - 1;
                Assert.AreEqual(expected, CutsceneData.Slides[i].neko,
                    $"слайд {i}: ожидался neko={expected}");
            }
        }

        [Test]
        public void FinalSlideHasNoImage()
        {
            // Последний слайд — чёрный космос (image == null).
            Assert.IsNull(CutsceneData.Slides[CutsceneData.Count - 1].image);
            // Первые три — с фоном.
            for (int i = 0; i < CutsceneData.Count - 1; i++)
                Assert.IsFalse(string.IsNullOrEmpty(CutsceneData.Slides[i].image), $"слайд {i} без фона");
        }

        [Test]
        public void CharCountMatchesSumOfBeats()
        {
            int sum = 0;
            foreach (var s in CutsceneData.Slides) sum += s.CharCount;
            Assert.AreEqual(sum, CutsceneData.TotalCharCount);
            Assert.Greater(CutsceneData.TotalCharCount, 0);
        }

        [Test]
        public void SlideDurations_AreProportionalAndFloored()
        {
            // При большой общей длительности — пропорционально числу символов,
            // и каждый слайд не короче минимума 8000 мс.
            int total = CutsceneData.TotalCharCount;
            int[] dur = CutsceneData.SlideDurationsMs(300000.0);

            Assert.AreEqual(CutsceneData.Count, dur.Length);
            foreach (var d in dur) Assert.GreaterOrEqual(d, 8000);

            // Слайд с большим числом символов длится не меньше слайда с меньшим.
            for (int i = 0; i < CutsceneData.Count; i++)
                for (int j = 0; j < CutsceneData.Count; j++)
                    if (CutsceneData.Slides[i].CharCount > CutsceneData.Slides[j].CharCount)
                        Assert.GreaterOrEqual(dur[i], dur[j],
                            $"слайд {i} (символов больше) короче слайда {j}");
        }

        [Test]
        public void SlideDurations_FloorAppliesWhenTotalTiny()
        {
            // Малая общая длительность → минимум 8000 на каждый слайд.
            int[] dur = CutsceneData.SlideDurationsMs(1000.0);
            foreach (var d in dur) Assert.AreEqual(8000, d);
        }
    }
}
