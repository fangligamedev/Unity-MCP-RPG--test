#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.IO;
using System.Linq;
using Game2DRPG.Map.Editor;
using Game2DRPG.Map.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game2DRPG.Map.EditMode.Tests
{
    public sealed class MapFoundationEditModeTests
    {
        [Test]
        public void FoundationAssets_CanBeEnsured()
        {
            MapBuilderController.EnsureFoundationAssets();

            Assert.That(AssetDatabase.LoadAssetAtPath<ResourceCatalogAsset>(MapAssetPaths.ResourceCatalogAsset), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<TileLayerRuleAsset>(MapAssetPaths.TileLayerRulesAsset), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<AmbientAnimationProfileAsset>(MapAssetPaths.AmbientAnimationProfileAsset), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<LevelLayoutAsset>(MapAssetPaths.RoomChainLayoutAsset), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<OverworldLayoutAsset>(MapAssetPaths.OpenWorldLayoutAsset), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<PCGProfileAsset>(MapAssetPaths.PcgProfileAsset), Is.Not.Null);
        }

        [Test]
        public void CatalogScan_ExportsAnimatedCatalogJson()
        {
            var catalog = MapBuilderController.ScanTinySwordsCatalog();

            Assert.That(catalog.entries.Count, Is.GreaterThan(0));
            Assert.That(catalog.animatedVariants.Count, Is.GreaterThan(0));
            Assert.That(catalog.externalCombatAssets.Any(asset => asset.id == "torch_goblin_project_ext"), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(MapAssetPaths.ResourceCatalogJson)), Is.True);
        }

        [Test]
        public void ShowcaseScenes_ExistOnDisk()
        {
            Assert.That(File.Exists(ToAbsolutePath(MapAssetPaths.RoomChainScene)), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(MapAssetPaths.OpenWorldScene)), Is.True);
        }

        [Test]
        public void RoomChainRandomGeneration_CanRoundTripJson()
        {
            MapBuilderController.GenerateRoomChain(20260317);
            var data = ReadJson<MapSaveData>(MapAssetPaths.RoomChainJson);

            Assert.That(data, Is.Not.Null);
            Assert.That(data!.markers.Any(marker => marker.markerType == MarkerType.PlayerStart), Is.True);
            Assert.That(data.encounters.Count, Is.GreaterThan(0));
            Assert.That(File.Exists(ToAbsolutePath(MapAssetPaths.RoomChainJson)), Is.True);
        }

        [Test]
        public void OpenWorldRandomGeneration_CanRoundTripJson()
        {
            MapBuilderController.GenerateOpenWorld(20260318);
            var data = ReadJson<OpenWorldSaveData>(MapAssetPaths.OpenWorldJson);

            Assert.That(data, Is.Not.Null);
            Assert.That(data!.regions.Count, Is.EqualTo(5));
            Assert.That(data.regionEncounters.Count, Is.GreaterThan(0));
            Assert.That(data.markers.Any(marker => marker.markerType == MarkerType.PlayerStart), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(MapAssetPaths.OpenWorldJson)), Is.True);
        }

        [Test]
        public void LoadLevelConfig_CanRebuildOpenWorldScene()
        {
            MapBuilderController.GenerateOpenWorld(20260319);
            MapBuilderController.LoadLevelConfig(MapMode.OpenWorld);

            var activeScene = EditorSceneManager.GetActiveScene();
            Assert.That(activeScene.path, Is.EqualTo(MapAssetPaths.OpenWorldScene));
            Assert.That(GameObject.Find("SceneRoot"), Is.Not.Null);
            Assert.That(GameObject.Find("SceneRoot/GameplayRoot/RuntimeBinder"), Is.Not.Null);
        }

        private static T? ReadJson<T>(string assetPath) where T : class
        {
            var absolutePath = ToAbsolutePath(assetPath);
            return File.Exists(absolutePath)
                ? JsonUtility.FromJson<T>(File.ReadAllText(absolutePath))
                : null;
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(UnityEngine.Application.dataPath)!;
            return Path.Combine(projectRoot, assetPath);
        }
    }
}