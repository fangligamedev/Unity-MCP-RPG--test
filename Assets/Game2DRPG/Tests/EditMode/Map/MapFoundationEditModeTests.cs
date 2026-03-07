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
        public void RoomChainGeneration_UsesGuideLayersAndSemantics()
        {
            MapBuilderController.GenerateRoomChain(20260321);
            var data = ReadJson<MapSaveData>(MapAssetPaths.RoomChainJson);

            Assert.That(data, Is.Not.Null);
            Assert.That(data!.tileLayers.Any(layer => layer.layerId == "BGColor"), Is.True);
            Assert.That(data.tileLayers.Any(layer => layer.layerId == "WaterFoam" && layer.tiles.Count > 0), Is.True);
            Assert.That(data.tileLayers.Any(layer => layer.layerId == "FlatGround"), Is.True);
            Assert.That(data.tileLayers.Any(layer => layer.layerId == "Shadow_L1" && layer.tiles.Count > 0), Is.True);
            Assert.That(data.tileLayers.Any(layer => layer.layerId == "ElevatedGround_L1"), Is.True);
            Assert.That(data.terrainCells.Any(cell => cell.semantic == TerrainSemantic.StairsL1 || cell.semantic == TerrainSemantic.StairsL2), Is.True);
            Assert.That(data.terrainCells.Any(cell => cell.semantic == TerrainSemantic.ElevatedTopL1), Is.True);
        }

        [Test]
        public void RoomChainShadows_AreOffsetOneTileBelowElevatedGround()
        {
            MapBuilderController.BuildRoomChainShowcase();
            var data = ReadJson<MapSaveData>(MapAssetPaths.RoomChainJson);
            Assert.That(data, Is.Not.Null);

            var elevatedCells = data!.terrainCells
                .Where(cell => cell.semantic == TerrainSemantic.ElevatedTopL1)
                .Select(cell => cell.position)
                .ToList();
            Assert.That(elevatedCells.Count, Is.GreaterThan(0));

            var shadowLayer = data.tileLayers.FirstOrDefault(layer => layer.layerId == "Shadow_L1");
            Assert.That(shadowLayer, Is.Not.Null);
            var shadowPositions = shadowLayer!.tiles.Select(tile => tile.position).ToHashSet();

            foreach (var elevated in elevatedCells)
            {
                var expectedShadow = new Vector3Int(elevated.x, elevated.y - 1, 0);
                if (data.terrainCells.Any(cell => cell.position == expectedShadow && cell.semantic == TerrainSemantic.ElevatedTopL1))
                {
                    continue;
                }

                Assert.That(
                    shadowPositions.Contains(expectedShadow),
                    Is.True,
                    $"缺少位于 {expectedShadow} 的 L1 阴影视觉层，当前高地格为 {elevated}。"
                );
            }
        }

        [Test]
        public void RoomChainCliffs_AreBlockedAndWalkabilityMatchesSemantics()
        {
            MapBuilderController.BuildRoomChainShowcase();
            var data = ReadJson<MapSaveData>(MapAssetPaths.RoomChainJson);
            Assert.That(data, Is.Not.Null);

            var cliffCells = data!.occupancyCells.Where(cell =>
                cell.semantic == TerrainSemantic.CliffToGroundL1 ||
                cell.semantic == TerrainSemantic.CliffToWaterL1 ||
                cell.semantic == TerrainSemantic.CliffToGroundL2 ||
                cell.semantic == TerrainSemantic.CliffToWaterL2).ToList();

            Assert.That(cliffCells.Count, Is.GreaterThan(0));
            Assert.That(cliffCells.All(cell => !cell.walkable), Is.True);
            Assert.That(data.occupancyCells.Any(cell => cell.semantic == TerrainSemantic.Water && !cell.walkable), Is.True);
            Assert.That(data.occupancyCells.Any(cell => (cell.semantic == TerrainSemantic.FlatGround || cell.semantic == TerrainSemantic.ElevatedTopL1 || cell.semantic == TerrainSemantic.StairsL1) && cell.walkable), Is.True);
        }

        [Test]
        public void RoomChainVegetation_UsesAnimatorControllersAndCameraProximity()
        {
            MapBuilderController.BuildRoomChainShowcase();
            var data = ReadJson<MapSaveData>(MapAssetPaths.RoomChainJson);
            Assert.That(data, Is.Not.Null);

            var vegetation = data!.animatedPlacements.Where(item => item.id.Contains("tree") || item.id.Contains("bush")).ToList();
            Assert.That(vegetation.Count, Is.GreaterThan(0));
            Assert.That(vegetation.All(item => item.useAnimatorController), Is.True);
            Assert.That(vegetation.All(item => item.animatorControllerPath.EndsWith(".controller")), Is.True);
            Assert.That(vegetation.All(item => item.activationPolicy == ActivationPolicy.ByCameraProximity), Is.True);
        }

        [Test]
        public void RoomChainScene_CreatesOccupancyAndEdgeBarriers()
        {
            MapBuilderController.BuildRoomChainShowcase();
            var activeScene = EditorSceneManager.GetActiveScene();

            Assert.That(activeScene.path, Is.EqualTo(MapAssetPaths.RoomChainScene));
            Assert.That(GameObject.Find("SceneRoot/MapRoot/GridRoot/BGColor"), Is.Not.Null);
            Assert.That(GameObject.Find("SceneRoot/MapRoot/GridRoot/WaterFoam"), Is.Not.Null);
            Assert.That(GameObject.Find("SceneRoot/MapRoot/GridRoot/FlatGround"), Is.Not.Null);
            Assert.That(GameObject.Find("SceneRoot/MapRoot/GridRoot/ElevatedGround_L1"), Is.Not.Null);

            var interactiveRoot = GameObject.Find("SceneRoot/MapRoot/InteractiveRoot");
            Assert.That(interactiveRoot, Is.Not.Null);
            Assert.That(interactiveRoot!.GetComponentsInChildren<BoxCollider2D>().Length, Is.GreaterThan(0));
            Assert.That(interactiveRoot.transform.Cast<Transform>().Any(child => child.name.StartsWith("EdgeBarrier_")), Is.True);
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