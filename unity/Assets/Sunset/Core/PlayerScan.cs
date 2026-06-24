using System;
using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>
    /// Входные данные для «скана» игрока — то, что реально доступно движку (имя,
    /// время, часовой пояс/город, ОС, экран, язык, ядра). Заполняется на стороне UI
    /// из Unity-API, чтобы логика «досье» оставалась чистой и тестируемой.
    /// </summary>
    [Serializable]
    public struct ScanInput
    {
        public string name;
        public DateTime now;
        public string city;
        public string region;
        public string os;
        public string device;   // браузер/устройство (свободно)
        public string entry;    // источник захода ("напрямую", хост и т.п.)
        public int screenW;
        public int screenH;
        public string lang;
        public int cpuThreads;
    }

    /// <summary>
    /// «Досье» игрока для интро «Некого» (порт getScanProfile/getScanHits из
    /// веб-версии). <see cref="Profile"/> — читаемая выкладка строк, <see cref="Hits"/>
    /// — короткие «найденные» фрагменты, мелькающие в потоке кода во время скана.
    /// Чистый C# без зависимостей от движка.
    /// </summary>
    public static class PlayerScan
    {
        private static string Pad2(int x) => x.ToString("D2");

        private static KeyValuePair<string, string> Row(string k, string v)
            => new KeyValuePair<string, string>(k, v);

        /// <summary>Читаемые строки досье: ключ → значение (имя, время, откуда…).</summary>
        public static List<KeyValuePair<string, string>> Profile(ScanInput s)
        {
            var rows = new List<KeyValuePair<string, string>>();

            rows.Add(Row("имя", string.IsNullOrEmpty(s.name) ? "не назвал" : s.name));
            rows.Add(Row("время", $"{Pad2(s.now.Hour)}:{Pad2(s.now.Minute)}:{Pad2(s.now.Second)}"));
            string from = string.IsNullOrEmpty(s.city) ? "?" : s.city;
            if (!string.IsNullOrEmpty(s.region)) from += " · " + s.region;
            rows.Add(Row("откуда", from));
            rows.Add(Row("вход", string.IsNullOrEmpty(s.entry) ? "напрямую" : s.entry));
            string sys = string.IsNullOrEmpty(s.os) ? "неизвестно" : s.os;
            if (!string.IsNullOrEmpty(s.device)) sys += " · " + s.device;
            rows.Add(Row("система", sys));
            if (s.screenW > 0 && s.screenH > 0) rows.Add(Row("экран", $"{s.screenW}×{s.screenH}"));
            if (!string.IsNullOrEmpty(s.lang)) rows.Add(Row("язык", s.lang));
            if (s.cpuThreads > 0) rows.Add(Row("ядра", s.cpuThreads.ToString()));
            return rows;
        }

        /// <summary>«Найденные» фрагменты кода — будто «Некий» сканирует игрока.</summary>
        public static List<string> Hits(ScanInput s)
        {
            var hits = new List<string>();
            if (!string.IsNullOrEmpty(s.region) || !string.IsNullOrEmpty(s.city))
            {
                string tz = !string.IsNullOrEmpty(s.region) && !string.IsNullOrEmpty(s.city)
                    ? s.region + "/" + s.city.Replace(' ', '_')
                    : (string.IsNullOrEmpty(s.region) ? s.city : s.region);
                hits.Add("find:TZ=" + tz);
            }
            if (!string.IsNullOrEmpty(s.os)) hits.Add("mem.scan(os)=" + s.os);
            hits.Add($"clock.local={Pad2(s.now.Hour)}:{Pad2(s.now.Minute)}");
            if (!string.IsNullOrEmpty(s.lang)) hits.Add("locale=" + s.lang.ToLowerInvariant());
            if (s.screenW > 0 && s.screenH > 0) hits.Add($"screen={s.screenW}x{s.screenH}");
            if (s.cpuThreads > 0) hits.Add("cpu.threads=" + s.cpuThreads);
            if (!string.IsNullOrEmpty(s.name))
            {
                hits.Add($"soul.bind(\"{s.name}\")");
                hits.Add("who_are_you :: " + s.name);
            }
            hits.Add("rift.open(target=YOU)");
            return hits;
        }

        /// <summary>
        /// Фразы-триггеры скана (порт SCAN_TRIGGER_RE): игрок спрашивает, видит ли
        /// его «Некий», кто он, откуда, который час и т.п.
        /// </summary>
        private static readonly string[] ScanTriggers =
        {
            "видишь меня", "видишь ли меня", "ты меня вид", "ты меня знаешь",
            "ты меня слыш", "ты меня чувств", "следишь за мной", "ты за мной",
            "кто я", "знаешь кто я", "что ты обо мне", "что ты про меня",
            "знаешь обо мне", "знаешь про меня", "откуда ты это", "откуда ты всё",
            "откуда ты все", "шпион", "ты меня запис", "откуда я",
            "из какого я город", "с какого я город", "какой у меня город",
            "какой мой город", "где я живу", "который час", "который сейчас час",
            "сколько времени", "сколько сейчас времени", "какая у меня система",
            "какая у меня ос", "с чего я зашёл", "с чего я зашел",
        };

        /// <summary>Просит ли реплика игрока «скан» (видит ли его «Некий», кто он…).</summary>
        public static bool WantsScan(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.ToLowerInvariant();
            foreach (var trig in ScanTriggers)
                if (t.Contains(trig)) return true;
            return false;
        }

        /// <summary>Готовый текстовый блок «досье» для всплывающей панели скана.</summary>
        public static string ReadoutText(ScanInput s)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("⟢ СКАНИРОВАНИЕ ОБЪЕКТА…\n");
            foreach (var row in Profile(s))
                sb.Append((row.Key + ":").PadRight(9)).Append(' ').Append(row.Value).Append('\n');
            sb.Append("⟢ ИДЕНТИФИКАЦИЯ ЗАВЕРШЕНА");
            return sb.ToString();
        }
    }
}
