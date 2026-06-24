using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>
    /// Прогресс прохождения сложностей и разблокировка секретной 5-й способности
    /// (порт getCleared/markCleared/secretUnlocked из веб-версии). «Творческий» не
    /// засчитывается. Чистая логика над списком пройденных сложностей —
    /// персистентность вынесена в <see cref="ProgressSave"/> для тестируемости.
    /// </summary>
    public static class GameProgress
    {
        /// <summary>Сложности, что засчитываются в секретку (без «Творческого»).</summary>
        public static readonly string[] RealDifficulties = { "easy", "normal", "hard", "nightmare" };

        public static bool IsReal(string id)
        {
            foreach (var d in RealDifficulties) if (d == id) return true;
            return false;
        }

        /// <summary>
        /// Помечает сложность пройденной. Возвращает true, если список изменился
        /// (сложность реальная и ещё не была отмечена).
        /// </summary>
        public static bool MarkCleared(List<string> cleared, string id)
        {
            if (cleared == null || !IsReal(id) || cleared.Contains(id)) return false;
            cleared.Add(id);
            return true;
        }

        /// <summary>Сколько РЕАЛЬНЫХ сложностей пройдено (0..4).</summary>
        public static int ClearedCount(IEnumerable<string> cleared)
        {
            if (cleared == null) return 0;
            int n = 0;
            foreach (var d in RealDifficulties)
            {
                foreach (var c in cleared) { if (c == d) { n++; break; } }
            }
            return n;
        }

        /// <summary>Все реальные сложности пройдены → секретная способность открыта.</summary>
        public static bool SecretUnlocked(IEnumerable<string> cleared)
            => ClearedCount(cleared) >= RealDifficulties.Length;
    }
}
