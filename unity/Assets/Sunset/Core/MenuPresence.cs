namespace Sunset.Core
{
    /// <summary>
    /// Логика присутствия «Некого» в главном меню (порт nekoMenuPresence из
    /// веб-версии). Чистые функции без зависимостей от сцены: возвращают текст
    /// реплики (или null, если «Некий» молчит).
    /// </summary>
    public static class MenuPresence
    {
        /// <summary>Сутки в миллисекундах — порог «давно не был».</summary>
        public const long DayMs = 24L * 3600L * 1000L;

        /// <summary>
        /// Приветственная реплика при заходе в меню. <paramref name="prevExit"/> —
        /// как игрок ушёл в прошлый раз ("peek"/"played"), <paramref name="timeAwayMs"/>
        /// — сколько его не было. Возвращает null, если «Некий» молчит.
        /// </summary>
        public static string Greeting(NekoState state, string prevExit, long timeAwayMs)
        {
            if (state == null) return null;
            string name = state.knownName;
            if (!string.IsNullOrEmpty(name))
            {
                if (prevExit == "peek")
                    return $"Ты заглянул на пару секунд и ушёл. Я заметил, {name}.";
                if (timeAwayMs > DayMs)
                    return $"Тебя давно не было, {name}. Я считал.";
                if (state.visits > 1)
                    return $"Снова ты, {name}.";
                return null;
            }
            if (state.visits > 1)
                return "Ты опять здесь. Но так и не зашёл.";
            return null;
        }

        /// <summary>
        /// Реплика «зависания» — если игрок долго стоит в меню, ничего не запуская.
        /// </summary>
        public static string DwellLine(NekoState state)
        {
            string name = state?.knownName;
            return string.IsNullOrEmpty(name)
                ? "Ты просто стоишь здесь. Я вижу."
                : $"Чего ты ждёшь, {name}?";
        }
    }
}
