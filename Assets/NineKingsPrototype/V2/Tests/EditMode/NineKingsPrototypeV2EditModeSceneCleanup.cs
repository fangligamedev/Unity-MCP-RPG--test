#nullable enable
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace NineKingsPrototype.V2.Tests.EditMode
{
    [SetUpFixture]
    public sealed class NineKingsPrototypeV2EditModeSceneCleanup
    {
        private const string MainScenePath = "Assets/NineKingsPrototype/V2/Scenes/NineKings_Main_V2.unity";
        private const string TempSceneFolder = "Assets/NineKingsPrototype/V2/Scenes/temp";
        private const string TempSceneFallbackPath = "Assets/NineKingsPrototype/V2/Scenes/temp/__autocleanup__.unity";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            NormalizeEditorScenes();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            NormalizeEditorScenes();
        }

        private static void NormalizeEditorScenes()
        {
            SaveDirtyScenesWithoutPrompt();
            CloseOpenTempScenes();
            DeleteTempSceneAssets();
            OpenMainScene();
        }

        private static void SaveDirtyScenesWithoutPrompt()
        {
            EnsureTempSceneFolderExists();
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (!scene.IsValid() || !scene.isLoaded || !scene.isDirty)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(scene.path))
                {
                    EditorSceneManager.SaveScene(scene, TempSceneFallbackPath, false);
                    continue;
                }

                EditorSceneManager.SaveScene(scene, scene.path, false);
            }
        }

        private static void CloseOpenTempScenes()
        {
            for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                if (!scene.path.StartsWith(TempSceneFolder, System.StringComparison.Ordinal))
                {
                    continue;
                }

                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void DeleteTempSceneAssets()
        {
            if (!AssetDatabase.IsValidFolder(TempSceneFolder))
            {
                return;
            }

            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { TempSceneFolder });
            foreach (var guid in sceneGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(assetPath);
            }

            AssetDatabase.Refresh();
        }

        private static void OpenMainScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            {
                return;
            }

            EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }

        private static void EnsureTempSceneFolderExists()
        {
            var absoluteFolderPath = Path.Combine(Directory.GetCurrentDirectory(), TempSceneFolder);
            Directory.CreateDirectory(absoluteFolderPath);
            AssetDatabase.Refresh();
        }
    }
}
