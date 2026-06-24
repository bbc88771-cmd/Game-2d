using System;
using System.Text;

namespace Sunset.Core
{
    /// <summary>
    /// Генератор строк «красного кода» для интро «Некого» (порт CODE_CH/CODE_SEGS/
    /// makeCodeLine из веб-версии): псевдо-код из случайных глифов и осмысленных
    /// сегментов (soul.bind, who_are_you, rift.open…). RNG передаётся снаружи —
    /// генерация детерминирована и тестируема.
    /// </summary>
    public class CodeStream
    {
        public const string Glyphs = "01xX#@/\\|<>[]{}()=+*-ABCDEF0123456789░▒▓§∆ΣλØ¤µ¬‡†";

        private readonly Random _rng;

        public CodeStream(Random rng) { _rng = rng ?? new Random(); }

        /// <summary>Случайная последовательность из <paramref name="n"/> глифов.</summary>
        public string Rnd(int n)
        {
            var sb = new StringBuilder(n);
            for (int i = 0; i < n; i++) sb.Append(Glyphs[_rng.Next(Glyphs.Length)]);
            return sb.ToString();
        }

        /// <summary>Один осмысленный сегмент кода.</summary>
        public string Segment()
        {
            switch (_rng.Next(19))
            {
                case 0: return "0x" + Rnd(4);
                case 1: return "INIT";
                case 2: return "soul.bind(" + Rnd(3) + ")";
                case 3: return "who_are_you";
                case 4: return "0b" + Rnd(6);
                case 5: return "trace[" + Rnd(2) + "]";
                case 6: return "rift.open()";
                case 7: return "mem.scan";
                case 8: return "::" + Rnd(5);
                case 9: return "echo(" + Rnd(3) + ")";
                case 10: return "WAKE";
                case 11: return "bind(" + Rnd(2) + ")";
                case 12: return "scan(" + Rnd(3) + ")";
                case 13: return "find:" + Rnd(4);
                case 14: return "soul[" + Rnd(2) + "]";
                case 15: return "??";
                case 16: return "0x" + Rnd(6);
                case 17: return "purge";
                default: return "seek()";
            }
        }

        /// <summary>Строка кода ровно заданной ширины (в символах).</summary>
        public string Line(int width)
        {
            if (width <= 0) return "";
            var sb = new StringBuilder(width + 16);
            while (sb.Length < width) sb.Append(Segment()).Append("  ");
            return sb.ToString(0, width);
        }
    }
}
