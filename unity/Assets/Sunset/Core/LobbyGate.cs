using System.Collections.Generic;
using System.Linq;

namespace Sunset.Core
{
    /// <summary>Игрок в лобби: имя, хост ли он, и сколько локаций он открыл.</summary>
    public class LobbyPlayer
    {
        public string name;
        public bool host;
        public int unlocked = 1;

        public LobbyPlayer(string name, bool host, int unlocked = 1)
        {
            this.name = name; this.host = host; this.unlocked = unlocked < 1 ? 1 : unlocked;
        }
    }

    public struct GateResult
    {
        public bool ok;
        public string message;
        public GateResult(bool ok, string message) { this.ok = ok; this.message = message; }
    }

    /// <summary>
    /// Логика разблокировки локаций в лобби (порт lobbyGate из веб-версии).
    /// Локация с индексом i доступна игроку, если он открыл хотя бы i+1 локаций.
    /// Старт возможен, только если выбранную локацию открыли ВСЕ игроки.
    /// </summary>
    public static class LobbyGate
    {
        /// <summary>Открыта ли локация <paramref name="locIndex"/> игроку.</summary>
        public static bool IsUnlocked(LobbyPlayer p, int locIndex) => p != null && p.unlocked > locIndex;

        public static GateResult Evaluate(IReadOnlyList<LobbyPlayer> players, int locIndex)
        {
            var loc = locIndex >= 0 && locIndex < GameLocations.Count ? GameLocations.All[locIndex] : null;
            string locName = loc != null ? loc.name : "?";

            var host = players?.FirstOrDefault(p => p.host);
            if (host == null) return new GateResult(false, "Нет хоста.");

            if (!IsUnlocked(host, locIndex))
                return new GateResult(false, $"🔒 Ты ещё не открыл «{locName}». Пройди предыдущие локации в одиночной игре.");

            var missing = players.Where(p => !p.host && !IsUnlocked(p, locIndex)).Select(p => p.name).ToList();
            if (missing.Count > 0)
                return new GateResult(false, $"Игра не начнётся: у {string.Join(", ", missing)} не разблокирована локация «{locName}».");

            return new GateResult(true, $"✓ Все игроки открыли «{locName}». Можно начинать.");
        }
    }
}
