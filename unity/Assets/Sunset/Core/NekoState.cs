using System;
using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>
    /// Состояние «Некого» и отношений с игроком. Простой сериализуемый класс
    /// (JsonUtility / PlayerPrefs). Порт <c>neko</c>-объекта из веб-версии.
    ///
    /// Шкалы:
    ///  - <see cref="trust"/>  −9..+9  — доверие (как игрок относится к «Некому»);
    ///  - <see cref="dark"/>    0..12  — «тьма» (жестокость/безысходность в репликах).
    /// Счётчики <see cref="kind"/>/<see cref="cruel"/>/<see cref="curious"/> копят
    /// поведение игрока. <see cref="endingPath"/> — путь к концовке (см. NekoBrain).
    /// </summary>
    [Serializable]
    public class NekoState
    {
        public string knownName;
        public int trust;
        public int dark;
        public int kind;
        public int cruel;
        public int curious;
        public string endingPath = "drift";

        public bool storyTold;
        public int visits;
        public long metAtUnix;
        public long lastSeenUnix;

        /// <summary>Как игрок ушёл в прошлый раз: "peek" (заглянул и ушёл) или "played".</summary>
        public string lastExit;

        /// <summary>Сколько локаций открыто в одиночной игре (1 = только стартовая).</summary>
        public int unlockedLocations = 1;

        /// <summary>Последние реплики игрока — для распознавания повторов.</summary>
        public List<string> playerHistory = new List<string>();

        public void Clamp()
        {
            if (trust < -9) trust = -9; else if (trust > 9) trust = 9;
            if (dark < 0) dark = 0; else if (dark > 12) dark = 12;
            if (unlockedLocations < 1) unlockedLocations = 1;
        }
    }

    /// <summary>
    /// «Слепок» игрока, который «Некий» якобы сканирует. Заполняется из Unity-API
    /// на стороне UI (часовой пояс, ОС, время) и передаётся в мозг — так логика
    /// остаётся независимой от движка и тестируемой.
    /// </summary>
    [Serializable]
    public struct PlayerProbe
    {
        public string city;      // город по часовому поясу, напр. "Moscow"
        public string region;    // регион пояса, напр. "Europe"
        public string os;        // "Windows" / "Android" / ...
        public string device;    // модель/браузер/устройство (свободно)
        public DateTime now;     // локальное время игрока

        public static PlayerProbe Empty()
        {
            return new PlayerProbe { city = "", region = "", os = "", device = "", now = DateTime.Now };
        }
    }
}
