using System.Collections.Generic;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>
    /// Тесты дневника: тон по пути, вмешательство «Некого», правки записей, крипи,
    /// записи под героя, детерминизм по seed.
    /// </summary>
    public class DiaryTests
    {
        private static bool Has(List<DiaryEntry> list, DiaryKind kind)
        {
            foreach (var e in list) if (e.kind == kind) return true;
            return false;
        }

        [Test]
        public void Build_NonEmpty_AllTextsPresent()
        {
            var list = Diary.Build(new NekoState(), "tessi", 1);
            Assert.Greater(list.Count, 0);
            foreach (var e in list) Assert.IsFalse(string.IsNullOrWhiteSpace(e.text));
        }

        [Test]
        public void NekoWriteLevel_RisesWithDark()
        {
            Assert.AreEqual(0, Diary.NekoWriteLevel(new NekoState { dark = 0, visits = 1 }));
            Assert.AreEqual(1, Diary.NekoWriteLevel(new NekoState { dark = 3 }));
            Assert.AreEqual(2, Diary.NekoWriteLevel(new NekoState { dark = 6 }));
            Assert.AreEqual(3, Diary.NekoWriteLevel(new NekoState { dark = 9 }));
        }

        [Test]
        public void CalmFresh_HasNoNekoOrCreepy()
        {
            // первый визит, без тьмы → чистый дневник игрока
            var list = Diary.Build(new NekoState { visits = 1, dark = 0 }, "amira", 5);
            Assert.IsFalse(Has(list, DiaryKind.Neko), "на чистом старте «Некий» не дописывает");
            Assert.IsFalse(Has(list, DiaryKind.Creepy), "крипи-записей быть не должно");
            Assert.IsFalse(Has(list, DiaryKind.Edited), "правок быть не должно");
        }

        [Test]
        public void DarkPath_NekoWritesAndEdits()
        {
            var s = new NekoState { dark = 9, cruel = 4, visits = 4 };
            var list = Diary.Build(s, "swordsman", 3);
            Assert.IsTrue(Has(list, DiaryKind.Neko), "на тёмном пути «Некий» дописывает");
            Assert.IsTrue(Has(list, DiaryKind.Edited), "на тёмном пути правит старые записи");
        }

        [Test]
        public void Edited_CarriesRewriteAndNote()
        {
            var s = new NekoState { dark = 9, cruel = 4 };
            var list = Diary.Build(s, "tessi", 7);
            DiaryEntry edited = default; bool found = false;
            foreach (var e in list) if (e.kind == DiaryKind.Edited) { edited = e; found = true; break; }
            Assert.IsTrue(found);
            Assert.IsTrue(edited.altered);
            Assert.IsFalse(string.IsNullOrEmpty(edited.note), "у правки есть приписка «Некого»");
        }

        [Test]
        public void HeroEntry_PresentForKnownHero()
        {
            var list = Diary.Build(new NekoState(), "walter", 2);
            bool any = false;
            foreach (var e in list)
                foreach (var h in DiaryEntries.Heroes["walter"])
                    if (e.text == h) any = true;
            Assert.IsTrue(any, "должна быть запись из дневника Уолтера");
        }

        [Test]
        public void Deterministic_BySeed()
        {
            var s = new NekoState { dark = 6, visits = 3 };
            var a = Diary.Build(s, "kaijo", 42);
            var b = Diary.Build(s, "kaijo", 42);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.AreEqual(a[i].text, b[i].text);
        }

        [Test]
        public void DrawingAltered_OnDarkPath()
        {
            var dark = Diary.Build(new NekoState { dark = 9, cruel = 4 }, "tessi", 1);
            bool anyDrawing = false, anyAltered = false;
            foreach (var e in dark)
                if (!string.IsNullOrEmpty(e.drawing)) { anyDrawing = true; if (e.drawingAltered) anyAltered = true; }
            Assert.IsTrue(anyDrawing, "повседневные записи несут рисунок");
            Assert.IsTrue(anyAltered, "на тёмном пути рисунок «испорчен»");
        }

        [Test]
        public void IslandEntry_BranchesByPath()
        {
            string good = Diary.IslandEntry("dark", new NekoState { kind = 5 }, 1);
            string bad = Diary.IslandEntry("dark", new NekoState { dark = 9 }, 1);
            Assert.IsFalse(string.IsNullOrEmpty(good));
            Assert.IsFalse(string.IsNullOrEmpty(bad));
            Assert.IsNull(Diary.IslandEntry("no_such_island", new NekoState(), 1));
        }
    }
}
