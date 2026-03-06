#nullable enable
/*
 * Copyright (c) 2026.
 */

using Game2DRPG.Runtime;
using NUnit.Framework;
using System.Linq;
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
                Assert.That(GameObject.Find("UIRoot/Canvas/HUD").transform is RectTransform, Is.True);
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

        [Test]
        public void AnimationClips_UseSequentialSpriteFrames()
        {
            AssertClipFrames("Assets/Game2DRPG/Animations/Player_Idle.anim", "Warrior_Blue_0", "Warrior_Blue_1", "Warrior_Blue_2", "Warrior_Blue_3", "Warrior_Blue_4", "Warrior_Blue_5");
            AssertClipFrames("Assets/Game2DRPG/Animations/Player_Move.anim", "Warrior_Blue_6", "Warrior_Blue_7", "Warrior_Blue_8", "Warrior_Blue_9", "Warrior_Blue_10", "Warrior_Blue_11");
            AssertClipFrames("Assets/Game2DRPG/Animations/Player_Attack.anim", "Warrior_Blue_12", "Warrior_Blue_13", "Warrior_Blue_14", "Warrior_Blue_15", "Warrior_Blue_16", "Warrior_Blue_17");
            AssertClipFrames("Assets/Game2DRPG/Animations/Torch_Idle.anim", "Torch_Red_0", "Torch_Red_1", "Torch_Red_2", "Torch_Red_3", "Torch_Red_4", "Torch_Red_5", "Torch_Red_6");
            AssertClipFrames("Assets/Game2DRPG/Animations/Torch_Move.anim", "Torch_Red_7", "Torch_Red_8", "Torch_Red_9", "Torch_Red_10", "Torch_Red_11", "Torch_Red_12");
            AssertClipFrames("Assets/Game2DRPG/Animations/Torch_Attack.anim", "Torch_Red_14", "Torch_Red_15", "Torch_Red_16", "Torch_Red_17", "Torch_Red_18", "Torch_Red_19");
            AssertClipFrames("Assets/Game2DRPG/Animations/Torch_Death.anim", "Torch_Red_28", "Torch_Red_29", "Torch_Red_30", "Torch_Red_31", "Torch_Red_32", "Torch_Red_33");
            AssertClipFrames("Assets/Game2DRPG/Animations/TNT_Idle.anim", "TNT_Red_0", "TNT_Red_1", "TNT_Red_2", "TNT_Red_3", "TNT_Red_4", "TNT_Red_5");
            AssertClipFrames("Assets/Game2DRPG/Animations/TNT_Move.anim", "TNT_Red_7", "TNT_Red_8", "TNT_Red_9", "TNT_Red_10", "TNT_Red_11", "TNT_Red_12");
            AssertClipFrames("Assets/Game2DRPG/Animations/TNT_Attack.anim", "TNT_Red_14", "TNT_Red_15", "TNT_Red_16", "TNT_Red_17", "TNT_Red_18", "TNT_Red_19", "TNT_Red_20");
            AssertClipFrames("Assets/Game2DRPG/Animations/TNT_Death.anim", "TNT_Red_18", "TNT_Red_19", "TNT_Red_20");
        }

        private static void AssertClipFrames(string clipPath, params string[] expectedSpriteNames)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Assert.That(clip, Is.Not.Null, $"Missing clip: {clipPath}");

            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip!);
            Assert.That(bindings.Length, Is.GreaterThan(0), $"Clip has no sprite bindings: {clipPath}");

            var actualNames = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0])
                .Select(keyframe => (keyframe.value as Sprite)?.name)
                .ToArray();

            CollectionAssert.AreEqual(expectedSpriteNames, actualNames, $"Unexpected frame order in {clipPath}");
        }
    }
}
