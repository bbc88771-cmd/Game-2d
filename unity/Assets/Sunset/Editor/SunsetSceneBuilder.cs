using System.IO;
using Sunset.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sunset.EditorTools
{
    /// <summary>
    /// Собирает сцену диалога с «Неким» одним кликом: пустая сцена + объект с
    /// <see cref="SunsetDialogue"/> (тот сам строит Canvas/чат/ввод в рантайме).
    /// Меню: Sunset → Build Dialogue Scene. Затем — Play.
    /// </summary>
    public static class SunsetSceneBuilder
    {
        private const string SceneDir = "Assets/Sunset/Scenes";
        private const string ScenePath = SceneDir + "/Dialogue.unity";

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
    }
}
