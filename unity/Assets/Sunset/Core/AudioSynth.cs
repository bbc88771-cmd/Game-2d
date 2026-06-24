using System;

namespace Sunset.Core
{
    /// <summary>
    /// Процедурный синтез звука (порт WebAudio-графа из веб-версии): эмбиент-дрон,
    /// «колокол» на смене кадра и «бип» печати «Некого». Возвращает массивы сэмплов
    /// в диапазоне [-1, 1] — чистая математика без зависимостей от движка, чтобы
    /// форму/длину можно было покрыть тестами. Обёртка в AudioClip — в UI/SunsetAudio.
    /// </summary>
    public static class AudioSynth
    {
        private const double TwoPi = Math.PI * 2.0;

        /// <summary>
        /// «Колокол»: синус частоты <paramref name="freq"/> с быстрой атакой (0.02с)
        /// до <paramref name="peak"/> и экспоненциальным спадом до тишины.
        /// </summary>
        public static float[] Bell(int sampleRate, float freq, float dur = 1.9f, float peak = 0.14f)
        {
            int n = Math.Max(1, (int)Math.Round(sampleRate * (double)dur));
            var data = new float[n];
            double attack = 0.02;
            int attackN = Math.Max(1, (int)(sampleRate * attack));
            // экспоненциальный спад от peak до 0.001 за dur
            double decayK = Math.Log(0.001 / Math.Max(1e-6, peak)) / dur;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / sampleRate;
                double env = i < attackN
                    ? peak * ((double)i / attackN)
                    : peak * Math.Exp(decayK * (t - attack));
                data[i] = (float)(Math.Sin(TwoPi * freq * t) * env);
            }
            return data;
        }

        /// <summary>«Бип» печати: короткий высокий блип с быстрым затуханием.</summary>
        public static float[] Tick(int sampleRate, float freq = 1200f, float dur = 0.05f, float peak = 0.08f)
        {
            int n = Math.Max(1, (int)Math.Round(sampleRate * (double)dur));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / sampleRate;
                double env = peak * Math.Exp(-t / (dur * 0.35));
                data[i] = (float)(Math.Sin(TwoPi * freq * t) * env);
            }
            return data;
        }

        /// <summary>
        /// Эмбиент-дрон: пила 55 Гц + синус 82.4 Гц + лёгкий шум, медленная амплитудная
        /// модуляция (LFO 0.07 Гц) и простой однополюсный ФНЧ. Длину берут кратной
        /// периоду LFO (≈14.3с), чтобы петля была бесшовной. Нормализован в [-1, 1].
        /// </summary>
        public static float[] Ambient(int sampleRate, float dur = 14.3f, int seed = 12345)
        {
            int n = Math.Max(1, (int)Math.Round(sampleRate * (double)dur));
            var data = new float[n];
            var rng = new Random(seed);
            double lp = 0.0;                  // состояние ФНЧ
            double lpA = LowpassCoeff(480.0, sampleRate);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / sampleRate;
                double saw = 2.0 * (55.0 * t - Math.Floor(0.5 + 55.0 * t)) * 0.16;
                double sine = Math.Sin(TwoPi * 82.4 * t) * 0.12;
                double noise = (rng.NextDouble() * 2.0 - 1.0) * 0.05;
                double raw = saw + sine + noise;
                lp += lpA * (raw - lp);       // однополюсный lowpass ~480 Гц
                double lfo = 0.8 + 0.2 * Math.Sin(TwoPi * 0.07 * t); // мягкое «дыхание»
                data[i] = (float)(lp * lfo);
            }
            Normalize(data, 0.9f);
            return data;
        }

        private static double LowpassCoeff(double cutoffHz, int sampleRate)
        {
            double dt = 1.0 / sampleRate;
            double rc = 1.0 / (TwoPi * cutoffHz);
            return dt / (rc + dt);
        }

        private static void Normalize(float[] data, float target)
        {
            float max = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float a = Math.Abs(data[i]);
                if (a > max) max = a;
            }
            if (max <= 1e-6f) return;
            float k = target / max;
            for (int i = 0; i < data.Length; i++) data[i] *= k;
        }
    }
}
