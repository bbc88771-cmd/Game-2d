using System;

namespace Sunset.Core
{
    /// <summary>
    /// Настройки игры (порт settings из веб-версии): громкость, язык, сложность по
    /// умолчанию, возрастной рейтинг (цензура реплик «Некого») и полноэкранный режим.
    /// Чистые сериализуемые данные без зависимостей от движка — персистентность
    /// вынесена в <see cref="SettingsSave"/>, чтобы логику можно было тестировать.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public int music = 70;       // 0..100
        public int sfx = 80;         // 0..100
        public string lang = "ru";   // "ru" | "en"
        public string difficulty = "normal";
        public bool fullscreen = false;
        public string rating = "16"; // "16" | "18" — цензура «Некого»

        /// <summary>Приводит значения в допустимые рамки (после загрузки/правки).</summary>
        public void Clamp()
        {
            music = Clamp01_100(music);
            sfx = Clamp01_100(sfx);
            if (lang != "ru" && lang != "en") lang = "ru";
            if (!GameDifficulties.IsValid(difficulty)) difficulty = "normal";
            if (rating != "16" && rating != "18") rating = "16";
        }

        private static int Clamp01_100(int v) => v < 0 ? 0 : (v > 100 ? 100 : v);

        public GameSettings Clone()
        {
            return new GameSettings
            {
                music = music, sfx = sfx, lang = lang,
                difficulty = difficulty, fullscreen = fullscreen, rating = rating,
            };
        }
    }
}
