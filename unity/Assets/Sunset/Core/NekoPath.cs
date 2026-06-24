using System;

namespace Sunset.Core
{
    /// <summary>
    /// Путь к концовке «Некого» и его тон. По дизайн-документу «Некий» нейтрален, но
    /// его общение смещается в зависимости от того, к какой концовке ведут поступки
    /// игрока: тёмные дела → он темнее (ведёт к разрушению), светлые → мягче.
    ///
    /// Путь оценивается по поведению (kind/cruel/dark из <see cref="NekoState"/>):
    ///  - "good"   — добрые поступки преобладают, тьмы мало;
    ///  - "bad"    — жестокость/тьма преобладают;
    ///  - "middle" — смешанно, сомнение;
    ///  - "drift"  — сигнала ещё нет (начало игры).
    /// Чистая логика — тестируемо.
    /// </summary>
    public static class NekoPath
    {
        public const string Drift = "drift";
        public const string Good = "good";
        public const string Middle = "middle";
        public const string Bad = "bad";

        /// <summary>Оценивает путь по состоянию (добро − зло − тьма).</summary>
        public static string Estimate(NekoState s)
        {
            if (s == null) return Drift;
            int activity = s.kind + s.cruel + s.curious + s.dark;
            if (activity == 0) return Drift;

            int score = s.kind - s.cruel - s.dark; // выше → светлее, ниже → темнее
            if (score >= 3) return Good;
            if (score <= -3) return Bad;
            return Middle;
        }

        /// <summary>«Темнота» тона 0..1 (для цвета/интонации реплик).</summary>
        public static float DarknessLevel(NekoState s)
        {
            if (s == null) return 0f;
            float d = s.dark / 12f;                       // базовая тьма
            string path = Estimate(s);
            if (path == Bad) d += 0.25f;
            else if (path == Good) d -= 0.15f;
            return Clamp01(d);
        }

        /// <summary>
        /// Записать поступок игрока в мире (добрый/злой). Двигает счётчики и путь —
        /// для геймплейных выборов (бой, кого пощадить и т.п.).
        /// </summary>
        public static void RecordChoice(NekoState s, bool good)
        {
            if (s == null) return;
            if (good) { s.kind += 1; if (s.dark > 0 && Even(s.kind)) s.dark -= 1; }
            else { s.cruel += 1; s.dark += 1; }
            s.Clamp();
            s.endingPath = Estimate(s);
        }

        /// <summary>Финальная фраза «Некого» по текущему пути.</summary>
        public static string Final(NekoState s)
        {
            switch (Estimate(s))
            {
                case Good: return NekoLines.FinalGood;
                case Bad: return NekoLines.FinalBad;
                default: return NekoLines.FinalMiddle;
            }
        }

        private static bool Even(int x) => (x & 1) == 0;
        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
