using System.Collections.Generic;
using System.Linq;

namespace Sunset.Core
{
    public enum LobbyMode { Host, Guest }

    /// <summary>
    /// Состояние лобби (порт объекта <c>lobby</c> из веб-версии): режим (хост/гость),
    /// код, список игроков, выбранная в карусели локация и сложность. Логика
    /// разблокировки локаций — в <see cref="LobbyGate"/>. Чистый C# для тестов.
    /// </summary>
    public class LobbyState
    {
        public const int MaxPlayers = 5;

        public LobbyMode mode;
        public string code;
        public readonly List<LobbyPlayer> players = new List<LobbyPlayer>();
        public int locIndex;
        public string difficulty = "normal";

        public LobbyState(LobbyMode mode, string code, LobbyPlayer host)
        {
            this.mode = mode;
            this.code = code;
            if (host != null) { host.host = true; players.Add(host); }
        }

        public LobbyPlayer Host => players.FirstOrDefault(p => p.host);
        public int HostUnlocked => Host != null ? Host.unlocked : 1;
        public bool IsFull => players.Count >= MaxPlayers;
        public int Count => players.Count;

        /// <summary>Добавляет гостя, если есть место. Возвращает true при успехе.</summary>
        public bool AddGuest(LobbyPlayer p)
        {
            if (p == null || IsFull) return false;
            p.host = false;
            players.Add(p);
            return true;
        }

        /// <summary>Приводит индекс карусели в допустимые рамки [0, локаций-1].</summary>
        public void ClampLocIndex()
        {
            int max = GameLocations.Count - 1;
            if (locIndex < 0) locIndex = 0;
            else if (locIndex > max) locIndex = max;
        }

        public void StepLocation(int delta)
        {
            locIndex += delta;
            ClampLocIndex();
        }

        /// <summary>Доступна ли локация <paramref name="i"/> хосту (для карусели/точек).</summary>
        public bool HostHasLocation(int i) => HostUnlocked > i;

        /// <summary>Результат проверки: можно ли стартовать на выбранной локации.</summary>
        public GateResult Gate()
        {
            ClampLocIndex();
            return LobbyGate.Evaluate(players, locIndex);
        }
    }
}
