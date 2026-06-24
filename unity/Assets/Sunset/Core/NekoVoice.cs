using System;
using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>
    /// Подбор реплик «Некого» по пути к концовке (<see cref="NekoPath"/>) из пулов
    /// <see cref="NekoLines"/>: спонтанные, поддержка после ошибок, «двойные реплики»
    /// (тёплое → тревожный хвост), мультиплеер (без натравливания) и финал. Анти-повтор
    /// через список недавних. RNG снаружи → тестируемо.
    /// </summary>
    public static class NekoVoice
    {
        /// <summary>Спонтанная реплика по пути (с примесью общих для разнообразия).</summary>
        public static string Spontaneous(NekoState s, IList<string> recent, Random rng)
        {
            rng = rng ?? new Random();
            string path = NekoPath.Estimate(s);
            // ~40% — общая/таинственная, иначе — реплика пути
            bool general = rng.NextDouble() < 0.4 || path == NekoPath.Drift;
            string[] pool = general ? NekoLines.General : PathPool(path);
            return PickFresh(pool, recent, rng);
        }

        /// <summary>Поддержка после ошибки/смерти/тяжёлого выбора (по тону пути).</summary>
        public static string Support(NekoState s, IList<string> recent, Random rng)
        {
            string path = NekoPath.Estimate(s);
            bool light = path == NekoPath.Good || path == NekoPath.Drift;
            return PickFresh(light ? NekoLines.SupportLight : NekoLines.SupportSad, recent, rng);
        }

        /// <summary>Мультиплеер: тёплые при добром пути, иначе наблюдательные (не сталкивает).</summary>
        public static string Multiplayer(NekoState s, IList<string> recent, Random rng, int playerCount)
        {
            if (playerCount < 2) return Spontaneous(s, recent, rng);
            string path = NekoPath.Estimate(s);
            string[] pool = path == NekoPath.Good ? NekoLines.MpWarm : NekoLines.MpObserve;
            return PickFresh(pool, recent, rng);
        }

        /// <summary>
        /// Иногда возвращает «двойную реплику»: тёплое <paramref name="warm"/> и через миг
        /// чуть тревожный хвост <paramref name="tail"/>. true — если выпала.
        /// </summary>
        public static bool TryDouble(NekoState s, Random rng, out string warm, out string tail)
        {
            rng = rng ?? new Random();
            warm = tail = null;
            string path = NekoPath.Estimate(s);
            double chance = path == NekoPath.Good ? 0.15 : 0.28; // на светлом пути реже
            if (rng.NextDouble() >= chance) return false;
            var pair = NekoLines.DoublePairs[rng.Next(NekoLines.DoublePairs.Length)];
            warm = pair[0]; tail = pair[1];
            return true;
        }

        /// <summary>Финальная фраза по пути.</summary>
        public static string Final(NekoState s) => NekoPath.Final(s);

        // ----- внутреннее -----

        private static string[] PathPool(string path)
        {
            switch (path)
            {
                case NekoPath.Good: return Concat(NekoLines.Good, NekoLines.Warm);
                case NekoPath.Bad: return NekoLines.Bad;
                case NekoPath.Middle: return NekoLines.Middle;
                default: return NekoLines.General;
            }
        }

        private static string[] Concat(string[] a, string[] b)
        {
            var r = new string[a.Length + b.Length];
            Array.Copy(a, r, a.Length);
            Array.Copy(b, 0, r, a.Length, b.Length);
            return r;
        }

        private static string PickFresh(string[] pool, IList<string> recent, Random rng)
        {
            if (pool == null || pool.Length == 0) return null;
            rng = rng ?? new Random();
            var fresh = new List<string>();
            foreach (var line in pool)
                if (recent == null || !recent.Contains(line)) fresh.Add(line);
            var src = fresh.Count > 0 ? fresh : new List<string>(pool);
            return src[rng.Next(src.Count)];
        }
    }
}
