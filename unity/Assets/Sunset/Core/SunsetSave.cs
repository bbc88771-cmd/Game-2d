using UnityEngine;

namespace Sunset.Core
{
    /// <summary>
    /// Сохранение состояния «Некого»/отношений в PlayerPrefs (порт localStorage).
    /// JsonUtility сериализует <see cref="NekoState"/>. Все ошибки гасятся —
    /// сбой сохранения не должен ронять игру.
    /// </summary>
    public static class SunsetSave
    {
        private const string Key = "sunset_neko";

        public static NekoState Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new NekoState();
            try
            {
                var s = JsonUtility.FromJson<NekoState>(json);
                if (s == null) return new NekoState();
                if (s.playerHistory == null) s.playerHistory = new System.Collections.Generic.List<string>();
                s.Clamp();
                return s;
            }
            catch { return new NekoState(); }
        }

        public static void Save(NekoState state)
        {
            if (state == null) return;
            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(state));
                PlayerPrefs.Save();
            }
            catch { /* запись не критична */ }
        }
    }
}
