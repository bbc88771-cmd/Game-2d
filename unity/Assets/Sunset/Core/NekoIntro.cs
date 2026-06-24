using System;
using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>
    /// Скриптовое интро «Некого» (порт runNekoIntro/nekoGreetReturning/nekoPeek/
    /// greetByTrust из веб-версии): приветствия по тону отношений, «глитч имени» с
    /// поправкой, «подглядывание» (я тебя вижу: пояс/время/ОС) и распознавание
    /// команды «дальше». Чистый C# — RNG снаружи, чтобы было тестируемо.
    /// </summary>
    public static class NekoIntro
    {
        public static readonly string[] GreetCold =
        {
            "{n}. Ты вернулся. Ладно.", "А. {n}. Снова ты.", "Значит, снова. Хорошо.",
            "{n}. У меня нет причин быть рад. Но я здесь.",
        };
        public static readonly string[] GreetNeutral =
        {
            "{n}. Ты вернулся. Хорошо.", "Снова ты. Это меня устраивает.",
            "{n}. Я ждал. Не долго, но ждал.", "Ты здесь. Начнём там, где остановились?",
        };
        public static readonly string[] GreetWarm =
        {
            "{n}. Ты пришёл. Я рад. Не делай из этого выводов.",
            "Ты снова здесь. Это… хорошо. Мне правда так кажется.",
            "{n}. Я думал о тебе. Немного.",
            "Ты вернулся. Ты всегда возвращаешься. Это что-то значит.",
        };

        public static readonly string[] GlitchNames =
        {
            "Анна", "Матвей", "Лиза", "Кто-то другой", "—", "Первый", "Остальные", "Ты",
        };
        public static readonly string[] GlitchCorrections =
        {
            "…нет. {n}. Прости.", "…нет. {n}. Я знаю, кто ты.",
            "…нет. {n}. Иногда они перемешиваются.", "…нет. Ты — {n}. Остальных здесь нет.",
        };

        /// <summary>Тон приветствия по шкалам: "cold" / "neutral" / "warm".</summary>
        public static string Tone(NekoState s)
        {
            if (s == null) return "neutral";
            if (s.trust < 0 || s.dark >= 5) return "cold";
            if (s.trust >= 2) return "warm";
            return "neutral";
        }

        /// <summary>Приветствие вернувшегося игрока по тону, с подстановкой имени.</summary>
        public static string Greeting(NekoState s, Random rng)
        {
            string[] pool = Tone(s) == "cold" ? GreetCold : Tone(s) == "warm" ? GreetWarm : GreetNeutral;
            string line = pool[(rng ?? new Random()).Next(pool.Length)];
            return line.Replace("{n}", s != null ? s.knownName : "");
        }

        /// <summary>Должен ли сработать «глитч имени» (реже при высоком доверии).</summary>
        public static bool ShouldGlitch(NekoState s, Random rng)
        {
            if (s == null || s.visits < 2) return false;
            double chance = s.trust >= 3 ? 0.08 : 0.2;
            return (rng ?? new Random()).NextDouble() < chance;
        }

        /// <summary>Пара реплик «глитча»: чужое имя и поправка.</summary>
        public static void Glitch(NekoState s, Random rng, out string wrong, out string correction)
        {
            rng = rng ?? new Random();
            string name = s != null ? s.knownName : "";
            // выбираем чужое имя (не совпадающее с настоящим)
            var pool = new List<string>();
            foreach (var g in GlitchNames) if (g != name) pool.Add(g);
            wrong = pool.Count > 0 ? pool[rng.Next(pool.Count)] : "Кто-то";
            correction = GlitchCorrections[rng.Next(GlitchCorrections.Length)].Replace("{n}", name);
        }

        /// <summary>Реплика про то, как игрок ушёл/как давно его не было (или null).</summary>
        public static string AwayLine(string lastExit, long timeAwayMs)
        {
            if (lastExit == "peek")
                return "Я помню: в прошлый раз ты лишь заглянул и закрыл, не начав. Думал, не замечу?";
            if (timeAwayMs > MenuPresence.DayMs)
            {
                int days = (int)(timeAwayMs / MenuPresence.DayMs);
                return $"Тебя не было {days} {Plural(days, "день", "дня", "дней")}. Я считал каждый.";
            }
            return null;
        }

        /// <summary>«Подглядывание»: я тебя вижу — пояс/время/ОС (порт nekoPeek).</summary>
        public static List<string> PeekLines(ScanInput s)
        {
            var lines = new List<string> { "Подожди. Я тебя… вижу." };
            string tz = null;
            if (!string.IsNullOrEmpty(s.region) && !string.IsNullOrEmpty(s.city))
                tz = s.region + "/" + s.city;
            else if (!string.IsNullOrEmpty(s.city)) tz = s.city;
            if (!string.IsNullOrEmpty(tz)) lines.Add($"Часовой пояс — {tz}.");

            string hh = s.now.Hour.ToString("D2"), mm = s.now.Minute.ToString("D2");
            int h = s.now.Hour;
            string part = h < 5 ? "Глубокая ночь" : h < 12 ? "Утро" : h < 18 ? "День" : "Поздний вечер";
            lines.Add($"{hh}:{mm} у тебя сейчас. {part}, верно?");

            if (!string.IsNullOrEmpty(s.os)) lines.Add($"И ты пришёл с {s.os}.");
            lines.Add("Не пугайся. Я вижу ещё не всё. Пока.");
            return lines;
        }

        private static readonly string[] AdvanceTriggers =
        {
            "дальше", "продолж", "поехали", "идём", "идем", "пошли", "погнали",
            "вперёд", "вперед", "готов", "хватит", "начнём", "начнем", "начинаем", "играть",
            "давай начн", "давай дальше", "ладно дальше",
        };

        /// <summary>Просит ли игрок продолжить (порт DONE_RE из nekoConverse).</summary>
        public static bool WantsAdvance(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.Trim().ToLowerInvariant();
            foreach (var trig in AdvanceTriggers)
                if (t.StartsWith(trig)) return true;
            return t == "го";
        }

        /// <summary>Склонение существительного по числу (день/дня/дней).</summary>
        public static string Plural(int n, string one, string few, string many)
        {
            int mod10 = n % 10, mod100 = n % 100;
            if (mod10 == 1 && mod100 != 11) return one;
            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 10 || mod100 >= 20)) return few;
            return many;
        }
    }
}
