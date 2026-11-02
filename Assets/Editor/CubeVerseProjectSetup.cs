#if UNITY_EDITOR
using System.IO;
using CubeVerse.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class CubeVerseProjectSetup
{
    private const string ScenePath = "Assets/Scenes/CubeVerse.unity";

    static CubeVerseProjectSetup() { EditorApplication.delayCall += EnsurePlayableScene; }

    [MenuItem("CubeVerse/Create or Repair Playable Scene")]
    private static void EnsurePlayableScene()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EnsureFolder("Assets/Scenes");
            if (!File.Exists(ScenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var gameObject = new GameObject("CubeVerse Game");
                gameObject.AddComponent<CubeVerseGame>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            bool included = false;
            foreach (EditorBuildSettingsScene entry in current)
                if (entry.path == ScenePath && entry.enabled) included = true;
            if (!included)
            {
                var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(current);
                scenes.RemoveAll(s => s.path == ScenePath);
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            PlayerSettings.companyName = "Veyl Studio";
            PlayerSettings.productName = "CubeVerse";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.veylstudio.cubeverse");
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            AssetDatabase.SaveAssets();
        }
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
