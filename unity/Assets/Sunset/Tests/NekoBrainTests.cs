using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>
    /// Юнит-тесты «мозга» диалога. NekoBrain не зависит от Unity-сцены, поэтому
    /// тесты идут в EditMode. Фиксированный seed делает выбор реплик детерминированным.
    /// </summary>
    public class NekoBrainTests
    {
        private static NekoBrain Make(out NekoState state)
        {
            state = new NekoState { knownName = "Тесси" };
            return new NekoBrain(state, seed: 12345);
        }

        // ---------- классификация ----------

        [TestCase("что это за острова", "islands")]
        [TestCase("где взять воду", "water")]
        [TestCase("что это за огоньки", "souls")]
        [TestCase("как драться с врагами", "combat")]
        [TestCase("есть ли другие выжившие", "survivors")]
        [TestCase("можно ли вернуться домой", "home")]
        [TestCase("зачем ты мне помогаешь", "purpose")]
        [TestCase("можно ли тебе верить", "trust")]
        [TestCase("кто ты", "who")]
        [TestCase("где я", "where")]
        [TestCase("привет", "greet")]
        [TestCase("откуда я?", "selfprobe")]
        [TestCase("который час", "selfprobe")]
        [TestCase("что случилось с миром", "story")]
        public void Classify_Categories(string input, string expected)
        {
            var brain = Make(out _);
            Assert.AreEqual(expected, brain.Classify(input), $"«{input}» должно быть '{expected}'");
        }

        // ---------- вопросы вне мира → внутримировой редирект ----------

        [TestCase("сколько будет два плюс два")]
        [TestCase("кто президент")]
        [TestCase("какая погода в париже")]
        [TestCase("как написать код на питоне")]
        public void OffWorld_RedirectsToWorld(string input)
        {
            var brain = Make(out _);
            string answer = brain.Respond(input);
            Assert.IsNotEmpty(answer);
            StringAssert.IsMatch("мир|осколк|пустот|остров|трещин", answer.ToLowerInvariant());
        }

        // ---------- вопросы о мире → ответ по существу (лексикон) ----------

        [TestCase("а есть тут магия")]
        [TestCase("тут есть река")]
        [TestCase("есть тут звери")]
        [TestCase("где солнце")]
        public void InWorld_LexiconAnswers_NonEmpty(string input)
        {
            var brain = Make(out _);
            string answer = brain.Respond(input);
            Assert.IsNotEmpty(answer);
            // лексикон не должен сваливаться в общий редирект
            StringAssert.DoesNotContain("Спрашивай о них", answer);
        }

        // ---------- анти-повтор: на одну тему даёт разные реплики ----------

        [Test]
        public void Respond_DoesNotRepeatImmediately()
        {
            var brain = Make(out _);
            var seen = new List<string>();
            for (int i = 0; i < 6; i++) seen.Add(brain.Respond("что это за острова"));
            // в пуле островов несколько реплик — подряд не должно быть одинаковых
            for (int i = 1; i < seen.Count; i++)
                Assert.AreNotEqual(seen[i - 1], seen[i], "две подряд реплики совпали");
            Assert.GreaterOrEqual(new HashSet<string>(seen).Count, 3, "слишком мало разнообразия");
        }

        // ---------- конкретный ответ о игроке ----------

        [Test]
        public void SelfAnswer_UsesProbeCity()
        {
            var brain = Make(out _);
            brain.Probe = new PlayerProbe { city = "Moscow", region = "Europe", os = "Windows", now = new DateTime(2026, 6, 24, 21, 53, 0) };
            string answer = brain.Respond("откуда я?");
            StringAssert.Contains("Moscow", answer);
        }

        [Test]
        public void SelfAnswer_TimeQuestion_ContainsClock()
        {
            var brain = Make(out _);
            brain.Probe = new PlayerProbe { city = "Moscow", region = "Europe", now = new DateTime(2026, 6, 24, 9, 5, 0) };
            string answer = brain.Respond("который час");
            StringAssert.Contains("09:05", answer);
        }

        // ---------- отношение влияет на настроение и концовку ----------

        [Test]
        public void Kindness_RaisesTrust_LeadsToBond()
        {
            var brain = Make(out var s);
            foreach (var msg in new[] { "спасибо", "ты помог", "доверяю тебе", "ты хороший", "люблю тебя", "береги себя" })
                brain.Adjust(msg);
            Assert.GreaterOrEqual(s.trust, 5);
            Assert.AreEqual("warm", brain.Mood);
            Assert.AreEqual("bond", brain.EndingPath);
        }

        [Test]
        public void Hostility_LowersTrust_LeadsToDefiance()
        {
            var brain = Make(out var s);
            foreach (var msg in new[] { "заткнись", "тупой", "идиот", "ненавижу", "ты бесполезный" })
                brain.Adjust(msg);
            Assert.LessOrEqual(s.trust, -3);
            Assert.AreEqual("defiance", brain.EndingPath);
        }

        [Test]
        public void Darkness_FeedsOblivion()
        {
            var brain = Make(out var s);
            foreach (var msg in new[] { "хочу умереть", "убей меня", "сдохни", "ненавижу", "мне всё равно" })
                brain.Adjust(msg);
            Assert.GreaterOrEqual(s.dark, 8);
            Assert.AreEqual("oblivion", brain.EndingPath);
        }

        [Test]
        public void Respond_NeverEmpty_AcrossManyInputs()
        {
            var brain = Make(out _);
            foreach (var msg in new[] { "привет", "кто ты", "где я", "что с водой", "магия", "пока", "ауаыва", "спасибо", "ненавижу" })
                Assert.IsNotEmpty(brain.Respond(msg), $"пустой ответ на «{msg}»");
        }
    }
}
