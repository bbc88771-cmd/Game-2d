using System;
using System.Text;

namespace Sunset.Core
{
    /// <summary>
    /// Коды лобби (порт randomCode из веб-версии). Алфавит без похожих символов
    /// (нет 0/O, 1/I), длина по умолчанию 6. Чистый C# — RNG передаётся снаружи,
    /// чтобы код был тестируемым и детерминированным.
    /// </summary>
    public static class LobbyCode
    {
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        public const int DefaultLength = 6;
        public const int MinValidLength = 4;

        public static string Generate(Random rng, int length = DefaultLength)
        {
            if (rng == null) rng = new Random();
            if (length < 1) length = DefaultLength;
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(Alphabet[rng.Next(Alphabet.Length)]);
            return sb.ToString();
        }

        /// <summary>Приводит введённый код к каноничному виду (UPPER, без пробелов).</summary>
        public static string Normalize(string code)
            => (code ?? "").Trim().ToUpperInvariant();

        /// <summary>Корректен ли введённый код (минимум 4 символа после нормализации).</summary>
        public static bool IsValid(string code)
            => Normalize(code).Length >= MinValidLength;
    }
}
