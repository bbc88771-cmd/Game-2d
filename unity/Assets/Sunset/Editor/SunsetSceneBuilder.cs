using System.Collections.Generic;
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
        private const string DifficultyPath = SceneDir + "/Difficulty.unity";
        private const string RedCodePath = SceneDir + "/RedCode.unity";

        [MenuItem("Sunset/Build Difficulty Scene")]
        public static void BuildDifficultyScene()
        {
            BuildScene<SunsetDifficultySelect>("SunsetDifficultySelect", DifficultyPath);
            AssetDatabase.Refresh();
            Debug.Log("[Sunset] Сцена выбора сложности собрана: " + DifficultyPath + ". Нажми Play.");
            EditorUtility.DisplayDialog("Sunset",
                "Сцена выбора сложности собрана:\n" + DifficultyPath +
                "\n\nЭто шаг новой игры между интро «Некого» и катсценой.\n\n" +
                "Если текст не виден — импортируй TMP Essentials:\nWindow → TextMeshPro → Import TMP Essential Resources.\nЗатем нажми Play.", "Ок");
        }

        /// <summary>Собирает сцену с одним объектом-компонентом без диалога.</summary>
        private static void BuildScene<T>(string objectName, string path) where T : Component
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            new GameObject(objectName).AddComponent<T>();
            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, path);
        }

        [MenuItem("Sunset/Build All Scenes")]
        public static void BuildAllScenes()
        {
            // порядок в Build Settings: первым — главное меню (точка входа)
            BuildScene<SunsetMainMenu>("SunsetMainMenu", MenuPath);
            BuildScene<SunsetDialogue>("SunsetDialogue", ScenePath);
            BuildScene<SunsetDifficultySelect>("SunsetDifficultySelect", DifficultyPath);
            BuildScene<SunsetCutscene>("SunsetCutscene", CutscenePath);
            BuildScene<SunsetLobby>("SunsetLobby", LobbyPath);
            BuildScene<SunsetHeroSelect>("SunsetHeroSelect", HeroPath);

            var paths = new[] { MenuPath, ScenePath, DifficultyPath, CutscenePath, LobbyPath, HeroPath };
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in paths) list.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = list.ToArray();

            AssetDatabase.Refresh();
            Debug.Log("[Sunset] Собраны все сцены и прописаны в Build Settings (вход — MainMenu).");
            EditorUtility.DisplayDialog("Sunset",
                "Собран весь поток игры и прописан в Build Settings:\n\n" +
                "MainMenu → (Новая игра) → Dialogue → (далее) → Difficulty → Cutscene → HeroSelect → меню\n" +
                "MainMenu → (Лобби) → Lobby → (старт) → Cutscene → HeroSelect\n\n" +
                "Открой сцену MainMenu и нажми Play.\n" +
                "Если текст не виден — Window → TextMeshPro → Import TMP Essential Resources.", "Ок");
        }

        [MenuItem("Sunset/Build Red Code Scene")]
        public static void BuildRedCodeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var go = new GameObject("RedCode");
            var rc = go.AddComponent<Sunset.UI.RedCodeBackground>();
            rc.baseOpacity = 0.45f;
            rc.autoScan = true;

            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, RedCodePath);
            AssetDatabase.Refresh();

            Debug.Log("[Sunset] Демо-сцена красного кода собрана: " + RedCodePath + ". Нажми Play.");
            EditorUtility.DisplayDialog("Sunset",
                "Демо красного кода собрано:\n" + RedCodePath +
                "\n\nПоток кода + случайные сканы (autoScan). В реальном интро код подключается\n" +
                "автоматически на экране диалога (SunsetDialogue.enableRedCode).\n\n" +
                "Если текст не виден — импортируй TMP Essentials:\nWindow → TextMeshPro → Import TMP Essential Resources.\nЗатем нажми Play.", "Ок");
        }

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
