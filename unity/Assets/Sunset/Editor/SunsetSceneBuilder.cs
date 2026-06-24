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
