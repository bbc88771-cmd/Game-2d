using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>Тип записи дневника.</summary>
    public enum DiaryKind { Player, Neko, Creepy, Edited }

    /// <summary>Одна запись дневника (текст, рисунок, приписка, флаги).</summary>
    public struct DiaryEntry
    {
        public string text;
        public string note;           // приписка (обычно от «Некого»), null
        public DiaryKind kind;
        public string drawing;        // маркер рисунка (pebble/tree/heart/food/cup), null
        public bool drawingAltered;   // рисунок «испорчен» (трещина/зубы) на тёмном пути
        public bool altered;          // запись переписана задним числом
    }

    /// <summary>
    /// Сборка дневника героя (по дизайн-документу). Тон смещается по пути к концовке,
    /// «Некий» дописывает (3 уровня) и правит старые записи, изредка появляются
    /// крипи-записи; у каждого героя свой голос. Детерминировано по seed → тестируемо.
    /// </summary>
    public static class Diary
    {
        public const string Intro =
            "Записи появляются по ходу игры. Дневник меняется вместе с тобой — " +
            "иногда сам. Перечитывай: бывает, твои слова уже не твои.";

        /// <summary>Уровень вмешательства «Некого» в дневник: 0 (нет) … 3 (явно он).</summary>
        public static int NekoWriteLevel(NekoState s)
        {
            if (s == null) return 0;
            if (s.dark >= 8) return 3;
            if (s.dark >= 5) return 2;
            if (s.dark >= 2 || s.visits >= 2) return 1;
            return 0;
        }

        /// <summary>Тон дневника по пути к концовке.</summary>
        public static string Tone(NekoState s) => NekoPath.Estimate(s);

        /// <summary>
        /// Собирает дневник под состояние/героя. <paramref name="seed"/> делает выбор
        /// детерминированным (в UI можно передать случайный).
        /// </summary>
        public static List<DiaryEntry> Build(NekoState s, string heroId, int seed = 0, int playerCount = 1)
        {
            var rng = new System.Random(seed);
            string path = NekoPath.Estimate(s);
            bool dark = path == NekoPath.Bad;
            var list = new List<DiaryEntry>();

            // повседневные (с рисунками; на тёмном пути рисунок «портится»)
            var usedEveryday = new HashSet<int>();
            for (int k = 0; k < 2 && usedEveryday.Count < DiaryEntries.Everyday.Length; k++)
            {
                int idx, guard = 0;
                do { idx = rng.Next(DiaryEntries.Everyday.Length); } while (!usedEveryday.Add(idx) && guard++ < 50);
                var e = DiaryEntries.Everyday[idx];
                list.Add(new DiaryEntry { text = e.text, drawing = e.drawing, kind = DiaryKind.Player, drawingAltered = dark });
            }

            // бой (с добротой)
            list.Add(Player(Pick(DiaryEntries.Combat, rng)));

            // запись под героя
            if (!string.IsNullOrEmpty(heroId) && DiaryEntries.Heroes.TryGetValue(heroId, out var hpool))
                list.Add(Player(Pick(hpool, rng)));

            // ветка по пути (тёплая/холодная/средняя/сомнения)
            string[] branch =
                path == NekoPath.Good ? DiaryEntries.GoodBranch :
                path == NekoPath.Bad ? DiaryEntries.BadBranch :
                path == NekoPath.Middle ? DiaryEntries.MiddleBranch :
                DiaryEntries.Doubt;
            list.Add(Player(Pick(branch, rng)));

            // мультиплеер
            if (playerCount >= 2)
                list.Add(Player(Pick(DiaryEntries.Multiplayer, rng)));

            // намёк на «Некого»
            list.Add(Player(Pick(DiaryEntries.NekoHints, rng)));

            // правка старой записи (тёмный путь / высокая тьма)
            if (dark || (s != null && s.dark >= 4))
            {
                var ed = DiaryEntries.Edits[rng.Next(DiaryEntries.Edits.Length)];
                list.Add(new DiaryEntry { text = ed.to, note = ed.note, kind = DiaryKind.Edited, altered = true });
            }

            // «Некий» дописывает — по уровню вмешательства
            int lvl = NekoWriteLevel(s);
            if (lvl > 0)
            {
                string[] np = lvl >= 3 ? DiaryEntries.NekoWriteClear
                            : lvl == 2 ? DiaryEntries.NekoWriteStrange
                            : DiaryEntries.NekoWriteMild;
                list.Add(new DiaryEntry { text = Pick(np, rng), kind = DiaryKind.Neko });
            }

            // крипи-записи (редко) — по тьме/визитам
            if (s != null)
            {
                double roll = rng.NextDouble();
                if (s.dark >= 7 && roll < 0.5)
                    list.Add(new DiaryEntry { text = Pick(DiaryEntries.CreepyStrong, rng), kind = DiaryKind.Creepy });
                else if (s.visits >= 3 && roll < 0.4)
                    list.Add(new DiaryEntry { text = Pick(DiaryEntries.CreepyMild, rng), kind = DiaryKind.Creepy });

                if (s.dark >= 10 && rng.NextDouble() < 0.35)
                    list.Add(new DiaryEntry { text = Pick(DiaryEntries.SpecialRare, rng), kind = DiaryKind.Creepy });
            }

            return list;
        }

        /// <summary>Запись для острова (ветка по пути), когда такие локации появятся.</summary>
        public static string IslandEntry(string islandId, NekoState s, int seed = 0)
        {
            if (string.IsNullOrEmpty(islandId) || !DiaryEntries.Islands.TryGetValue(islandId, out var pair))
                return null;
            var rng = new System.Random(seed);
            bool good = NekoPath.Estimate(s) != NekoPath.Bad;
            var pool = good ? pair.good : pair.bad;
            return pool.Length > 0 ? pool[rng.Next(pool.Length)] : null;
        }

        // ----- внутреннее -----

        private static DiaryEntry Player(string text)
            => new DiaryEntry { text = text, kind = DiaryKind.Player };

        private static string Pick(string[] pool, System.Random rng)
            => pool == null || pool.Length == 0 ? null : pool[rng.Next(pool.Length)];
    }
}
