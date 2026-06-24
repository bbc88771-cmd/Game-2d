using System.IO;
using Sunset.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sunset.EditorTools
{
    /// <summary>
    /// Собирает сцены «Sunset» одним кликом: пустая сцена + объект с нужным UI-
    /// компонентом (тот сам строит Canvas в рантайме).
    /// Меню: Sunset → Build Dialogue Scene / Build Cutscene Scene. Затем — Play.
    /// </summary>
    public static class SunsetSceneBuilder
    {
        private const string SceneDir = "Assets/Sunset/Scenes";
        private const string ScenePath = SceneDir + "/Dialogue.unity";
        private const string CutscenePath = SceneDir + "/Cutscene.unity";
        private const string MenuPath = SceneDir + "/MainMenu.unity";
        private const string LobbyPath = SceneDir + "/Lobby.unity";
        private const string HeroPath = SceneDir + "/HeroSelect.unity";

        [MenuItem("Sunset/Build Hero Select Scene")]
        public static void BuildHeroSelectScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("SunsetHeroSelect");
            go.AddComponent<SunsetHeroSelect>();

            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, HeroPath);
            AssetDatabase.Refresh();

            Debug.Log("[Sunset] Сцена выбора героя собрана: " + HeroPath + ". Нажми Play.");
            EditorUtility.DisplayDialog("Sunset",
                "Сцена выбора героя собрана:\n" + HeroPath +
                "\n\nПортреты ищутся в Resources/Sunset/ (hero_tessi, hero_amira, hero_swordsman, hero_kaijo, hero_walter).\n" +
                "Секретная 5-я способность открывается после прохождения на всех 4 сложностях.\n\n" +
                "Если текст не виден — импортируй TMP Essentials:\nWindow → TextMeshPro → Import TMP Essential Resources.\nЗатем нажми Play.", "Ок");
        }

        [MenuItem("Sunset/Build Lobby Scene")]
        public static void BuildLobbyScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("SunsetLobby");
            go.AddComponent<SunsetLobby>();

            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, LobbyPath);
            AssetDatabase.Refresh();

            Debug.Log("[Sunset] Сцена лобби собрана: " + LobbyPath + ". Нажми Play.");
            EditorUtility.DisplayDialog("Sunset",
                "Сцена лобби собрана:\n" + LobbyPath +
                "\n\nКартинки локаций ищутся в Resources/Sunset/ (loc_start, loc_snow, loc_dead, loc_swamp, loc_magic).\n" +
                "Без них карточка будет тёмной. «Друзья» подключаются по таймеру со случайным прогрессом —\n" +
                "это наглядно показывает блокировку старта (гейт).\n\n" +
                "Если текст не виден — импортируй TMP Essentials:\nWindow → TextMeshPro → Import TMP Essential Resources.\nЗатем нажми Play.", "Ок");
        }

        [MenuItem("Sunset/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("SunsetMainMenu");
            go.AddComponent<SunsetMainMenu>();

            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, MenuPath);
            AssetDatabase.Refresh();

            Debug.Log("[Sunset] Сцена главного меню собрана: " + MenuPath + ". Нажми Play.");
            EditorUtility.DisplayDialog("Sunset",
                "Сцена главного меню собрана:\n" + MenuPath +
                "\n\nФон и логотип ищутся в Resources/Sunset/ (menu_bg, logo).\n" +
                "Без них фон будет тёмным, а заголовок — текстом.\n\n" +
                "Если текст не виден — импортируй TMP Essentials:\nWindow → TextMeshPro → Import TMP Essential Resources.\nЗатем нажми Play.", "Ок");
        }

        [MenuItem("Sunset/Build Dialogue Scene")]
        public static void BuildDialogueScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("SunsetDialogue");
            go.AddComponent<SunsetDialogue>();

            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[Sunset] Сцена диалога собрана: " + ScenePath + ". Нажми Play, чтобы поговорить с «Неким».");
            EditorUtility.DisplayDialog("Sunset",
                "Сцена диалога собрана:\n" + ScenePath +
                "\n\nЕсли текст не виден — импортируй TMP Essentials:\nWindow → TextMeshPro → Import TMP Essential Resources.\nЗатем нажми Play.", "Ок");
        }

        [MenuItem("Sunset/Build Cutscene Scene")]
        public static void BuildCutsceneScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("SunsetCutscene");
            go.AddComponent<SunsetCutscene>();

            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, CutscenePath);
            AssetDatabase.Refresh();

            Debug.Log("[Sunset] Сцена катсцены собрана: " + CutscenePath + ". Нажми Play, чтобы посмотреть вступление.");
            EditorUtility.DisplayDialog("Sunset",
                "Сцена катсцены собрана:\n" + CutscenePath +
                "\n\nФоновые спрайты ищутся в Resources/Sunset/ (background, loc_dead, loc_magic).\n" +
                "Без них кадры будут тёмной заливкой — катсцена всё равно проходима.\n\n" +
                "Если текст не виден — импортируй TMP Essentials:\nWindow → TextMeshPro → Import TMP Essential Resources.\nЗатем нажми Play.", "Ок");
        }
    }
}
