#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.IO;
using Game2DRPG.Map.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game2DRPG.Map.Editor
{
    public static class MapBuilderController
    {
        public static ResourceCatalogAsset EnsureCatalogAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ResourceCatalogAsset>(MapAssetPaths.ResourceCatalogAsset);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<ResourceCatalogAsset>();
            AssetDatabase.CreateAsset(asset, MapAssetPaths.ResourceCatalogAsset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static TileLayerRuleAsset EnsureRulesAsset()
        {
            return TileRuleAssetFactory.EnsureTileLayerRules();
        }

        public static AmbientAnimationProfileAsset EnsureAmbientProfile()
        {
            return TileRuleAssetFactory.EnsureAmbientProfile();
        }

        public static LevelLayoutAsset EnsureRoomChainLayout()
        {
            return TileRuleAssetFactory.EnsureRoomChainLayout();
        }

        public static OverworldLayoutAsset EnsureOpenWorldLayout()
        {
            return TileRuleAssetFactory.EnsureOpenWorldLayout();
        }

        public static PCGProfileAsset EnsurePcgProfile()
        {
            return TileRuleAssetFactory.EnsurePcgProfile();
        }

        public static void EnsureFoundationAssets()
        {
            EnsureCatalogAsset();
            EnsureRulesAsset();
            EnsureAmbientProfile();
            EnsureRoomChainLayout();
            EnsureOpenWorldLayout();
            EnsurePcgProfile();
            AssetDatabase.SaveAssets();
        }

        public static ResourceCatalogAsset ScanTinySwordsCatalog(bool recordMilestone = true)
        {
            EnsureFoundationAssets();
            var catalog = EnsureCatalogAsset();
            TinySwordsCatalogScanner.Scan(catalog);
            MapJsonExporter.ExportCatalog(catalog);
            if (recordMilestone)
            {
                AppendMilestone("Catalog 扫描导出闭环", "已生成 ResourceCatalog.asset 和 resource-catalog.json");
            }

            return catalog;
        }

        public static void BuildRoomChainShowcase()
        {
            EnsureFoundationAssets();
            var catalog = ScanTinySwordsCatalog(recordMilestone: false);
            var rules = EnsureRulesAsset();
            var layout = EnsureRoomChainLayout();
            var saveData = RoomChainGenerator.GenerateShowcase(catalog, layout, 20260307);
            MapJsonExporter.ExportRoomChain(saveData);
            MapSceneAssembler.BuildRoomChainScene(saveData, rules);
            AppendMilestone("RoomChain Showcase 可生成", $"已保存 {MapAssetPaths.RoomChainScene}");
        }

        public static void BuildOpenWorldShowcase()
        {
            EnsureFoundationAssets();
            var catalog = ScanTinySwordsCatalog(recordMilestone: false);
            var rules = EnsureRulesAsset();
            var layout = EnsureOpenWorldLayout();
            var saveData = OpenWorldGenerator.GenerateShowcase(catalog, layout, 20260307);
            MapJsonExporter.ExportOpenWorld(saveData);
            MapSceneAssembler.BuildOpenWorldScene(saveData, rules);
            AppendMilestone("OpenWorld Showcase 可生成", $"已保存 {MapAssetPaths.OpenWorldScene}");
        }

        public static void GenerateRoomChain(int seed)
        {
            EnsureFoundationAssets();
            var catalog = ScanTinySwordsCatalog(recordMilestone: false);
            var rules = EnsureRulesAsset();
            var layout = EnsureRoomChainLayout();
            var profile = EnsurePcgProfile();
            var saveData = RoomChainGenerator.GenerateRandom(catalog, layout, profile.roomChainProfile, seed);
            MapJsonExporter.ExportRoomChain(saveData);
            MapSceneAssembler.BuildRoomChainScene(saveData, rules);
            AppendMilestone("RoomChain PCG 可生成", $"seed={seed}");
        }

        public static void GenerateOpenWorld(int seed)
        {
            EnsureFoundationAssets();
            var catalog = ScanTinySwordsCatalog(recordMilestone: false);
            var rules = EnsureRulesAsset();
            var layout = EnsureOpenWorldLayout();
            var profile = EnsurePcgProfile();
            var saveData = OpenWorldGenerator.GenerateRandom(catalog, layout, profile.openWorldProfile, seed);
            MapJsonExporter.ExportOpenWorld(saveData);
            MapSceneAssembler.BuildOpenWorldScene(saveData, rules);
            AppendMilestone("OpenWorld PCG 可生成", $"seed={seed}");
        }

        public static void SaveCurrentLevelConfig()
        {
            var binder = UnityEngine.Object.FindAnyObjectByType<MapRuntimeBinder>();
            if (binder == null)
            {
                Debug.LogWarning("当前场景中没有 MapRuntimeBinder，无法保存地图配置。");
                return;
            }

            if (binder.Mode == MapMode.OpenWorld && binder.OpenWorldData != null)
            {
                MapJsonExporter.ExportOpenWorld(binder.OpenWorldData);
                AppendMilestone("Save/Load 闭环通过", "已导出 openworld-save.json");
                return;
            }

            if (binder.RoomChainData != null)
            {
                MapJsonExporter.ExportRoomChain(binder.RoomChainData);
                AppendMilestone("Save/Load 闭环通过", "已导出 roomchain-save.json");
            }
        }

        public static void LoadLevelConfig(MapMode mode)
        {
            EnsureFoundationAssets();
            var rules = EnsureRulesAsset();
            if (mode == MapMode.OpenWorld)
            {
                var saveData = MapJsonImporter.ImportOpenWorld();
                if (saveData != null)
                {
                    MapSceneAssembler.BuildOpenWorldScene(saveData, rules);
                }

                return;
            }

            var roomChain = MapJsonImporter.ImportRoomChain();
            if (roomChain != null)
            {
                MapSceneAssembler.BuildRoomChainScene(roomChain, rules);
            }
        }

        private static void AppendMilestone(string milestone, string result)
        {
            var absolutePath = ToAbsolutePath(MapAssetPaths.ImplementationLog);
            var block = $"### 里程碑\n- {milestone}\n- {result}\n";
            var current = File.Exists(absolutePath) ? File.ReadAllText(absolutePath) : string.Empty;
            if (current.Contains(block, StringComparison.Ordinal))
            {
                return;
            }

            File.AppendAllText(absolutePath, $"\n{block}");
            AssetDatabase.Refresh();
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("无法解析 Unity 工程根目录。");
            }

            return Path.Combine(projectRoot, assetPath);
        }
    }
}