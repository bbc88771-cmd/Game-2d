using UnityEngine;

namespace Sunset.Core
{
    /// <summary>
    /// Персистентность <see cref="GameSettings"/> в PlayerPrefs (порт localStorage).
    /// Аналог <see cref="SunsetSave"/> для настроек. Ошибки гасятся.
    /// </summary>
    public static class SettingsSave
    {
        private const string Key = "sunset_settings";

        public static GameSettings Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new GameSettings();
            try
            {
                var s = JsonUtility.FromJson<GameSettings>(json);
                if (s == null) return new GameSettings();
                s.Clamp();
                return s;
            }
            catch { return new GameSettings(); }
        }

        public static void Save(GameSettings settings)
        {
            if (settings == null) return;
            try
            {
                settings.Clamp();
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(settings));
                PlayerPrefs.Save();
            }
            catch { /* запись не критична */ }
        }
    }
}
