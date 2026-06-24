using System;

namespace Sunset.Core
{
    /// <summary>Запись дневника: текст и (опционально) приписка «Некого».</summary>
    [Serializable]
    public struct JournalEntry
    {
        public string text;
        public string note; // null — без приписки
        public JournalEntry(string text, string note) { this.text = text; this.note = note; }
    }

    /// <summary>
    /// Дневник игрока (порт openJournal из веб-версии). «Некий» дописывает строки и
    /// правит уже написанное, когда растёт «тьма» и число визитов. Чистая логика:
    /// генерация записей детерминирована по seed, чтобы её можно было тестировать.
    /// </summary>
    public static class MenuJournal
    {
        public static readonly string[] Annotations =
        {
            "— Я это читал. — Н.",
            "— Ты забыл добавить: ты был напуган. — Н.",
            "— Это неточно. Но пусть останется. — Н.",
            "— Хорошее слово. «Тишина». — Н.",
            "— Ты здесь врёшь себе. Это нормально. — Н.",
        };

        public const string Intro =
            "Записи появляются по ходу игры. «Некий» оставляет здесь свои строки — " +
            "и, бывает, правит уже написанное. Перечитывай: иногда твои слова уже не твои.";

        /// <summary>
        /// Строит записи дневника по состоянию «Некого». <paramref name="seed"/> делает
        /// выбор приписки детерминированным (в UI можно передать случайный).
        /// </summary>
        public static JournalEntry[] Build(NekoState state, int seed = 0)
        {
            int dark = state?.dark ?? 0;
            int visits = state?.visits ?? 0;
            // правка «задним числом» включается при повторном заходе или росте тьмы
            bool edited = visits >= 2 || dark >= 2;

            string annot = Annotations[((seed % Annotations.Length) + Annotations.Length) % Annotations.Length];

            return new[]
            {
                new JournalEntry(
                    "Я нашёл деревянный мост. Он скрипит." + (edited ? " Некий был там." : ""),
                    edited ? "— Я не менял это. Клянусь. — Н." : null),
                new JournalEntry(
                    dark >= 7 ? "Мне страшно. Это правильно." : "Мне страшно. Но я продолжу.",
                    annot),
                new JournalEntry("Вода кончается. Надо искать источник.", null),
            };
        }
    }
}
