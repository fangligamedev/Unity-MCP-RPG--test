#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections.Generic;
using Game2DRPG.Map.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game2DRPG.Map.Editor
{
    internal static class TileRuleAssetFactory
    {
        public static TileLayerRuleAsset EnsureTileLayerRules()
        {
            var asset = LoadOrCreate<TileLayerRuleAsset>(MapAssetPaths.TileLayerRulesAsset);
            asset.schemaVersion = 1;
            asset.cellSize = 1f;
            asset.staticLayers = new List<StaticLayerDefinition>
            {
                CreateLayer("BGColor", 0, false, false),
                CreateLayer("WaterFoam", 1, false, false),
                CreateLayer("FlatGround", 2, false, true),
                CreateLayer("Shadow_L1", 3, false, false),
                CreateLayer("ElevatedGround_L1", 4, false, true),
                CreateLayer("Shadow_L2", 5, false, false),
                CreateLayer("ElevatedGround_L2", 6, false, true),
                CreateLayer("NavigationMask", 7, false, true),
            };
            asset.dynamicChannels = new List<DynamicChannelDefinition>
            {
                CreateDynamicChannel(AnimationChannel.AnimatedWater, ActivationPolicy.AlwaysOn, 10),
                CreateDynamicChannel(AnimationChannel.AnimatedShoreline, ActivationPolicy.AlwaysOn, 11),
                CreateDynamicChannel(AnimationChannel.AnimatedVegetation, ActivationPolicy.ByCameraProximity, 12),
                CreateDynamicChannel(AnimationChannel.AmbientProps, ActivationPolicy.ByCameraProximity, 13),
                CreateDynamicChannel(AnimationChannel.ReactiveFX, ActivationPolicy.ByEncounterState, 14),
            };
            asset.navigationRules = new NavigationRuleDefinition
            {
                spawnSafeRadius = 3,
                minimumCombatClearRadius = 4,
                blockWater = true,
            };
            asset.placementRules = new PlacementRuleDefinition
            {
                maxDecorationDensity = 0.5f,
                minimumRewardDistanceFromSpawn = 8,
                minimumEncounterDistanceFromReward = 4,
            };

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static AmbientAnimationProfileAsset EnsureAmbientProfile()
        {
            var asset = LoadOrCreate<AmbientAnimationProfileAsset>(MapAssetPaths.AmbientAnimationProfileAsset);
            asset.maxAlwaysOnAnimations = 40;
            asset.maxVisibleAmbientAnimations = 24;
            asset.maxReactiveFxPerBurst = 10;
            asset.cameraActivationRadius = 12f;
            asset.randomizeLoopOffsets = true;

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static LevelLayoutAsset EnsureRoomChainLayout()
        {
            var asset = LoadOrCreate<LevelLayoutAsset>(MapAssetPaths.RoomChainLayoutAsset);
            asset.id = "roomchain_showcase_v1";
            asset.mode = MapMode.RoomChain;
            asset.rooms = new List<RoomNodeDefinition>
            {
                new() { id = "start_room", roomType = RoomType.Start, bounds = new RectInt(0, 0, 12, 8) },
                new() { id = "narrow_pass", roomType = RoomType.Connector, bounds = new RectInt(14, 1, 8, 6) },
                new() { id = "resource_court", roomType = RoomType.Resource, bounds = new RectInt(24, 10, 12, 8) },
                new() { id = "elevation_arena", roomType = RoomType.Combat, bounds = new RectInt(24, -1, 14, 10) },
                new() { id = "bridge_room", roomType = RoomType.Elite, bounds = new RectInt(42, -3, 12, 8) },
                new() { id = "reward_room", roomType = RoomType.Reward, bounds = new RectInt(58, 5, 12, 8) },
                new() { id = "exit_gate", roomType = RoomType.Exit, bounds = new RectInt(58, -8, 12, 8) },
            };
            asset.edges = new List<RoomEdgeDefinition>
            {
                new() { fromRoomId = "start_room", toRoomId = "narrow_pass", isPrimaryPath = true },
                new() { fromRoomId = "narrow_pass", toRoomId = "elevation_arena", isPrimaryPath = true },
                new() { fromRoomId = "elevation_arena", toRoomId = "bridge_room", isPrimaryPath = true },
                new() { fromRoomId = "bridge_room", toRoomId = "exit_gate", isPrimaryPath = true },
                new() { fromRoomId = "narrow_pass", toRoomId = "resource_court", isPrimaryPath = false },
                new() { fromRoomId = "bridge_room", toRoomId = "reward_room", isPrimaryPath = false },
            };

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static OverworldLayoutAsset EnsureOpenWorldLayout()
        {
            var asset = LoadOrCreate<OverworldLayoutAsset>(MapAssetPaths.OpenWorldLayoutAsset);
            asset.id = "openworld_showcase_v1";
            asset.mode = MapMode.OpenWorld;
            asset.regions = new List<RegionNodeDefinition>
            {
                new() { id = "spawn_meadow", regionType = RegionType.SpawnMeadow, bounds = new RectInt(0, 0, 20, 20) },
                new() { id = "wetland_belt", regionType = RegionType.WetlandBelt, bounds = new RectInt(18, -2, 22, 22) },
                new() { id = "resource_forest", regionType = RegionType.ResourceForest, bounds = new RectInt(38, 1, 22, 22) },
                new() { id = "ruined_village", regionType = RegionType.RuinedVillage, bounds = new RectInt(60, 0, 22, 22) },
                new() { id = "high_plateau_citadel", regionType = RegionType.HighPlateauCitadel, bounds = new RectInt(80, 2, 18, 24) },
            };
            asset.edges = new List<RegionEdgeDefinition>
            {
                new() { fromRegionId = "spawn_meadow", toRegionId = "wetland_belt" },
                new() { fromRegionId = "wetland_belt", toRegionId = "resource_forest" },
                new() { fromRegionId = "resource_forest", toRegionId = "ruined_village" },
                new() { fromRegionId = "ruined_village", toRegionId = "high_plateau_citadel" },
            };

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static PCGProfileAsset EnsurePcgProfile()
        {
            var asset = LoadOrCreate<PCGProfileAsset>(MapAssetPaths.PcgProfileAsset);
            asset.roomChainProfile.roomCount = new IntRange { Min = 7, Max = 9 };
            asset.roomChainProfile.branchDepth = new IntRange { Min = 1, Max = 2 };
            asset.roomChainProfile.waterRoomChance = 0.5f;
            asset.roomChainProfile.elevationRoomChance = 0.35f;
            asset.roomChainProfile.animationDensity = 0.45f;
            asset.openWorldProfile.worldSize = new Vector2Int(96, 40);
            asset.openWorldProfile.waterCoverage = 0.28f;
            asset.openWorldProfile.elevationCoverage = 0.22f;
            asset.openWorldProfile.vegetationDensity = 0.35f;
            asset.openWorldProfile.encounterZoneCount = 6;
            asset.openWorldProfile.animationDensity = 0.55f;

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static StaticLayerDefinition CreateLayer(string id, int order, bool hasCollider, bool participatesInNavigation)
        {
            return new StaticLayerDefinition
            {
                id = id,
                sortingOrder = order,
                hasCollider = hasCollider,
                participatesInNavigation = participatesInNavigation,
            };
        }

        private static DynamicChannelDefinition CreateDynamicChannel(AnimationChannel channel, ActivationPolicy policy, int order)
        {
            return new DynamicChannelDefinition
            {
                channel = channel,
                activationPolicy = policy,
                sortingOrder = order,
                allowRuntimeToggle = policy != ActivationPolicy.AlwaysOn,
            };
        }
    }
}
