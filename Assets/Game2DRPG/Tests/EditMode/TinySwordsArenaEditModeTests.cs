#nullable enable
/*
 * Copyright (c) 2026.
 */

using Game2DRPG.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game2DRPG.Tests.EditMode
{
    public sealed class TinySwordsArenaEditModeTests
    {
        private const string ScenePath = "Assets/Game2DRPG/Scenes/TinySwordsArena.unity";
        private const string PlayerPrefabPath = "Assets/Game2DRPG/Prefabs/Player.prefab";
        private const string TorchPrefabPath = "Assets/Game2DRPG/Prefabs/TorchGoblin.prefab";
        private const string TntPrefabPath = "Assets/Game2DRPG/Prefabs/TntGoblin.prefab";
        private const string ShrinePrefabPath = "Assets/Game2DRPG/Prefabs/RewardShrine.prefab";

        [Test]
        public void TinySwordsArenaScene_ExistsAndOpens()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Assert.That(sceneAsset, Is.Not.Null);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Assert.That(scene.IsValid(), Is.True);
            Assert.That(scene.isLoaded, Is.True);
            EditorSceneManager.CloseScene(scene, true);
        }

        [Test]
        public void PlayerPrefab_HasRequiredComponents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab!.GetComponent<TopDownPlayerController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerCombat>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Health>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Animator>(), Is.Not.Null);
        }

        [Test]
        public void EnemyAndShrinePrefabs_HaveRequiredComponents()
        {
            var torch = AssetDatabase.LoadAssetAtPath<GameObject>(TorchPrefabPath);
            var tnt = AssetDatabase.LoadAssetAtPath<GameObject>(TntPrefabPath);
            var shrine = AssetDatabase.LoadAssetAtPath<GameObject>(ShrinePrefabPath);

            Assert.That(torch, Is.Not.Null);
            Assert.That(torch!.GetComponent<EnemyBrainTorchGoblin>(), Is.Not.Null);
            Assert.That(torch.GetComponent<Health>(), Is.Not.Null);

            Assert.That(tnt, Is.Not.Null);
            Assert.That(tnt!.GetComponent<EnemyBrainTntGoblin>(), Is.Not.Null);
            Assert.That(tnt.GetComponent<Health>(), Is.Not.Null);

            Assert.That(shrine, Is.Not.Null);
            Assert.That(shrine!.GetComponent<RewardShrine>(), Is.Not.Null);
            Assert.That(shrine.GetComponent<Collider2D>(), Is.Not.Null);
        }

        [Test]
        public void Scene_ContainsHudAndSpawnPoints()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Assert.That(GameObject.Find("UIRoot/Canvas/HUD"), Is.Not.Null);
                Assert.That(GameObject.Find("Systems/WaveDirector"), Is.Not.Null);
                Assert.That(GameObject.Find("Systems/Bootstrap"), Is.Not.Null);
                Assert.That(GameObject.Find("Spawns/Spawn_A"), Is.Not.Null);
                Assert.That(GameObject.Find("Spawns/Spawn_B"), Is.Not.Null);
                Assert.That(GameObject.Find("Spawns/Spawn_C"), Is.Not.Null);
                Assert.That(GameObject.Find("Spawns/Spawn_D"), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Scene_MainCameraAndBootstrapReferences_AreReadyForRuntime()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = GameObject.Find("Main Camera");
            var bootstrap = GameObject.Find("Systems/Bootstrap");
            var player = GameObject.Find("Player");
            var shrine = GameObject.Find("RewardShrine");

            Assert.That(camera, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(shrine, Is.Not.Null);
            Assert.That(camera!.transform.position.z, Is.LessThan(-1f));
            Assert.That(camera.GetComponent<CameraFollow2D>(), Is.Not.Null);
            Assert.That(player!.transform.position.y, Is.LessThan(-0.5f));
            Assert.That(shrine!.transform.position.x, Is.GreaterThan(1f));

            var serializedBootstrap = new SerializedObject(bootstrap.GetComponent<Bootstrap>());
            Assert.That(serializedBootstrap.FindProperty("cameraFollow")!.objectReferenceValue, Is.Not.Null);
            Assert.That(serializedBootstrap.FindProperty("player")!.objectReferenceValue, Is.Not.Null);
            Assert.That(scene.isLoaded, Is.True);
        }
    }
}