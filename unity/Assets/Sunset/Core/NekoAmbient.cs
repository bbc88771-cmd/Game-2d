using System;
using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>
    /// Присутствие «Некого» во время игры: редкие короткие реплики — спонтанные или
    /// по триггеру события. Главный канал общения с «Неким» — здесь, в мире, а не в
    /// интро/лобби. Реплики появляются редко и с большими паузами, чтобы не приставать
    /// и чтобы игрок иногда успевал о нём забыть.
    ///
    /// Чистая логика выбора реплик (тон по отношениям, анти-повтор). Тайминги/показ —
    /// в UI/NekoPresence. RNG передаётся снаружи → тестируемо.
    /// </summary>
    public static class NekoAmbient
    {
        // ----- паузы (секунды). Большие и случайные: «Некий» не назойлив -----
        public const float FirstDelayMin = 25f;
        public const float FirstDelayMax = 50f;
        public const float SpontaneousMin = 150f;   // 2.5 мин
        public const float SpontaneousMax = 360f;   // 6 мин
        public const float TriggerMinGap = 18f;      // не чаще, даже по триггерам

        // ----- спонтанные реплики по тону отношений -----
        private static readonly string[] SpontNeutral =
        {
            "Я ещё здесь. Просто смотрю.",
            "Этот мир дышит иначе, когда в нём кто-то есть.",
            "Ты делаешь успехи. Или мне так кажется.",
            "Иногда я молчу не потому, что меня нет.",
            "Где-то там — другие. Но ты меня слышишь. Только ты.",
            "Не оборачивайся. Это просто ветер. Наверное.",
        };
        private static readonly string[] SpontCold =
        {
            "Ты всё ещё думаешь, что справишься сам.",
            "Я считаю твои ошибки. Их немало.",
            "Можешь меня игнорировать. Я подожду. Я умею ждать.",
            "Этот мир тебя не любит. Я — другое дело. Может быть.",
            "Ты бы уже сдался. Но почему-то идёшь дальше. Любопытно.",
        };
        private static readonly string[] SpontWarm =
        {
            "Ты молодец. Я редко это говорю.",
            "Мне… спокойнее, когда ты рядом. Не делай выводов.",
            "Я присматриваю за тобой. На всякий случай.",
            "Передохни, если устал. Мир подождёт. И я тоже.",
            "Знаешь, я начинаю привыкать к тебе.",
        };

        // ----- реплики по триггерам события -----
        private static readonly Dictionary<string, string[]> Triggers = new Dictionary<string, string[]>
        {
            ["night"] = new[]
            {
                "Ночь. В темноте они смелее. Будь осторожен.",
                "Когда гаснет свет — я вижу лучше. А ты?",
            },
            ["wounded"] = new[]
            {
                "Тебе больно. Я чувствую это. Не геройствуй.",
                "Кровь. Остановись, пока можешь.",
            },
            ["resource_low"] = new[]
            {
                "У тебя кончается. Ты ведь заметил?",
                "Без воды далеко не уйдёшь. Поищи источник.",
            },
            ["discovery"] = new[]
            {
                "Вот оно. Я знал, что ты найдёшь.",
                "Любопытно. Этого здесь раньше не было. Или было?",
            },
            ["boss"] = new[]
            {
                "Чувствуешь? Что-то большое. Оно уже знает о тебе.",
                "Не все встречи заканчиваются разговором. Эта — нет.",
            },
            ["build"] = new[]
            {
                "Ты строишь. Значит, веришь, что останешься. Хорошо.",
                "Стены не спасут. Но с ними теплее. Я понимаю.",
            },
        };

        /// <summary>Тон реплик по отношениям (как в интро).</summary>
        public static string Tone(NekoState s) => NekoIntro.Tone(s);

        /// <summary>Спонтанная реплика по тону, без недавних повторов.</summary>
        public static string Spontaneous(NekoState s, IList<string> recent, Random rng)
        {
            string[] pool = Tone(s) == "cold" ? SpontCold : Tone(s) == "warm" ? SpontWarm : SpontNeutral;
            return PickFresh(pool, recent, rng);
        }

        /// <summary>Реплика по ключу события (или null, если ключ неизвестен).</summary>
        public static string ForTrigger(string key, IList<string> recent, Random rng)
        {
            if (string.IsNullOrEmpty(key) || !Triggers.TryGetValue(key, out var pool)) return null;
            return PickFresh(pool, recent, rng);
        }

        /// <summary>Все известные ключи триггеров (для редактора/демо).</summary>
        public static IEnumerable<string> TriggerKeys => Triggers.Keys;

        /// <summary>Случайная пауза до следующей спонтанной реплики (секунды).</summary>
        public static float NextSpontaneousDelay(Random rng)
        {
            rng = rng ?? new Random();
            return SpontaneousMin + (float)rng.NextDouble() * (SpontaneousMax - SpontaneousMin);
        }

        public static float FirstDelay(Random rng)
        {
            rng = rng ?? new Random();
            return FirstDelayMin + (float)rng.NextDouble() * (FirstDelayMax - FirstDelayMin);
        }

        /// <summary>
        /// Выбирает строку из пула, избегая недавно показанных (<paramref name="recent"/>).
        /// Если все недавние — берёт любую. Детерминировано при заданном RNG.
        /// </summary>
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
