using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sunset.UI
{
    /// <summary>
    /// Переходы между сценами игры (фаза 9 порта). Связывает экраны в единый поток:
    /// меню → интро «Некого» → катсцена → выбор героя; и меню → лобби → катсцена.
    /// Имена сцен совпадают с теми, что собирает редактор (Sunset → Build … Scene).
    ///
    /// Если сцены нет в Build Settings, переход не падает, а пишет подсказку —
    /// поэтому отдельные сцены остаются запускаемыми по одной. Собери весь поток:
    /// Sunset → Build All Scenes.
    /// </summary>
    public static class SceneFlow
    {
        public const string MainMenu = "MainMenu";
        public const string Dialogue = "Dialogue";
        public const string Cutscene = "Cutscene";
        public const string Lobby = "Lobby";
        public const string HeroSelect = "HeroSelect";

        public static void Go(string scene)
        {
            if (string.IsNullOrEmpty(scene)) return;
            if (Application.CanStreamedLevelBeLoaded(scene))
            {
                SceneManager.LoadScene(scene);
            }
            else
            {
                Debug.LogWarning($"[Sunset] Сцена «{scene}» не в Build Settings — переход пропущен. " +
                    "Собери весь поток: меню Sunset → Build All Scenes.");
            }
        }
    }
}
