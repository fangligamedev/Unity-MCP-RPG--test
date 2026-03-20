#nullable enable
using NineKingsPrototype.V2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NineKingsPrototype.V2.Editor
{
    public static class NineKingsPrototypeV2Bootstrapper
    {
        private const string ContentDatabasePath = "Assets/NineKingsPrototype/V2/Data/Definitions/NineKingsV2ContentDatabase.asset";
        private const string ScenePath = "Assets/NineKingsPrototype/V2/Scenes/NineKings_Main_V2.unity";
        private const string ArcherDuelDebugScenePath = "Assets/NineKingsPrototype/V2/Scenes/NineKings_ArcherDuel_Debug.unity";

        [MenuItem("Tools/NineKings/V2/Build Foundation")]
        public static void BuildFoundation()
        {
            EnsureContentDatabase();
            EnsureScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/NineKings/V2/Build Archer Duel Debug Scene")]
        public static void BuildArcherDuelDebugScene()
        {
            EnsureContentDatabase();
            EnsureArcherDuelDebugScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static ContentDatabase EnsureContentDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<ContentDatabase>(ContentDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ContentDatabase>();
                Populate(database);
                AssetDatabase.CreateAsset(database, ContentDatabasePath);
            }
            else
            {
                Populate(database);
                EditorUtility.SetDirty(database);
            }

            return database;
        }

        public static void EnsureScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("NineKingsV2Root");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(EnsureContentDatabase());
            root.AddComponent<NineKingsV2ScenePresenter>().SetController(controller);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.2f;
            camera.backgroundColor = new Color(0.77f, 0.67f, 0.45f, 1f);
            camera.transform.position = new Vector3(0f, 0f, -10f);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        public static void EnsureArcherDuelDebugScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("NineKingsV2Root");
            var controller = root.AddComponent<NineKingsV2GameController>();
            controller.SetDatabase(EnsureContentDatabase());

            var controllerSerializedObject = new SerializedObject(controller);
            controllerSerializedObject.FindProperty("_autoStartRun")!.boolValue = false;
            controllerSerializedObject.ApplyModifiedPropertiesWithoutUndo();

            var presenter = root.AddComponent<NineKingsV2ScenePresenter>();
            presenter.SetController(controller);
            root.AddComponent<NineKingsV2ArcherDuelDebugController>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.8f;
            camera.backgroundColor = new Color(0.77f, 0.67f, 0.45f, 1f);
            camera.transform.position = new Vector3(0.35f, -0.2f, -10f);

            EditorSceneManager.SaveScene(scene, ArcherDuelDebugScenePath);
        }

        private static void Populate(ContentDatabase database)
        {
            NineKingsV2SampleContentFactory.Populate(database);
        }
    }
}
