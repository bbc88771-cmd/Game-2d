using System.Collections.Generic;
using UnityEngine;

namespace Sunset.Core
{
    /// <summary>
    /// Персистентность прогресса сложностей в PlayerPrefs (порт localStorage
    /// "sotw_cleared"). Хранит список пройденных сложностей строкой через запятую.
    /// </summary>
    public static class ProgressSave
    {
        private const string Key = "sunset_cleared";

        public static List<string> Load()
        {
            var list = new List<string>();
            string raw = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (var part in raw.Split(','))
            {
                string id = part.Trim();
                if (id.Length > 0 && !list.Contains(id)) list.Add(id);
            }
            return list;
        }

        public static void Save(List<string> cleared)
        {
            if (cleared == null) return;
            try
            {
                PlayerPrefs.SetString(Key, string.Join(",", cleared));
                PlayerPrefs.Save();
            }
            catch { /* запись не критична */ }
        }

        /// <summary>Отмечает сложность пройденной и сохраняет (если изменилось).</summary>
        public static void MarkCleared(string difficulty)
        {
            var cleared = Load();
            if (GameProgress.MarkCleared(cleared, difficulty)) Save(cleared);
        }

        public static bool SecretUnlocked() => GameProgress.SecretUnlocked(Load());
        public static int ClearedCount() => GameProgress.ClearedCount(Load());
    }
}
