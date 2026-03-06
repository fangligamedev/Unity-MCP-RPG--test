#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections.Generic;
using System.Linq;
using Game2DRPG.Map.Runtime;
using UnityEngine;

namespace Game2DRPG.Map.Editor
{
    internal static class OpenWorldGenerator
    {
        public static OpenWorldSaveData GenerateShowcase(ResourceCatalogAsset catalog, OverworldLayoutAsset layout, int seed)
        {
            return GenerateInternal(catalog, layout, seed, true);
        }

        public static OpenWorldSaveData GenerateRandom(ResourceCatalogAsset catalog, OverworldLayoutAsset layout, OpenWorldProfile profile, int seed)
        {
            return GenerateInternal(catalog, layout, seed, false);
        }

        private static OpenWorldSaveData GenerateInternal(ResourceCatalogAsset catalog, OverworldLayoutAsset layout, int seed, bool fixedLayout)
        {
            var random = new System.Random(seed);
            var palette = MapPalette.Create(catalog);
            var regions = layout.regions
                .Select(region => CloneRegion(region, fixedLayout, random))
                .ToList();

            var save = new OpenWorldSaveData
            {
                schemaVersion = 1,
                mode = nameof(MapMode.OpenWorld),
                seed = seed,
                layoutId = layout.id,
                regions = regions,
            };

            var worldRect = MapGenerationUtility.Expand(MapGenerationUtility.Union(regions.Select(region => region.bounds)), 8);
            MapGenerationUtility.FillRect(save.tileLayers, "BGColor", worldRect, palette.backgroundTilePath);
            MapGenerationUtility.FillRect(save.tileLayers, "AnimatedWater", worldRect, palette.waterTilePath);

            foreach (var region in regions)
            {
                var groundTile = region.regionType == RegionType.WetlandBelt ? palette.alternateGroundTilePath : palette.flatGroundTilePath;
                MapGenerationUtility.FillRect(save.tileLayers, "FlatGround", region.bounds, groundTile);

                if (region.regionType == RegionType.HighPlateauCitadel)
                {
                    var elevatedRect = new RectInt(region.bounds.xMin + 3, region.bounds.yMin + 4, region.bounds.width - 6, region.bounds.height - 6);
                    var shadowRect = new RectInt(elevatedRect.xMin, elevatedRect.yMin - 1, elevatedRect.width, elevatedRect.height);
                    MapGenerationUtility.FillRect(save.tileLayers, "Shadow_L1", shadowRect, palette.shadowTilePath);
                    MapGenerationUtility.FillRect(save.tileLayers, "ElevatedGround_L1", elevatedRect, palette.elevationTilePath);
                }
            }

            for (var index = 0; index < regions.Count - 1; index++)
            {
                MapGenerationUtility.DrawCorridor(
                    save.tileLayers,
                    "FlatGround",
                    MapGenerationUtility.Center(regions[index].bounds),
                    MapGenerationUtility.Center(regions[index + 1].bounds),
                    4,
                    palette.flatGroundTilePath);
            }

            var spawnRegion = regions.First(region => region.regionType == RegionType.SpawnMeadow);
            MapGenerationUtility.AddMarker(save.markers, "player_start", MarkerType.PlayerStart, ToWorld(spawnRegion.bounds.center), regionId: spawnRegion.id);
            MapGenerationUtility.AddMarker(save.markers, "world_reward", MarkerType.RewardSpawn, ToWorld(regions[2].bounds.center), regionId: regions[2].id);
            MapGenerationUtility.AddMarker(save.markers, "world_exit", MarkerType.ExitSpawn, ToWorld(regions[^1].bounds.center), regionId: regions[^1].id);

            AddLandmarks(save, regions, palette);
            AddAmbient(save, regions, palette);
            AddEncounters(save, regions);

            return save;
        }

        private static void AddLandmarks(OpenWorldSaveData save, List<RegionNodeDefinition> regions, MapPalette palette)
        {
            foreach (var region in regions)
            {
                switch (region.regionType)
                {
                    case RegionType.SpawnMeadow:
                        MapGenerationUtility.AddDecoration(save.decorations, "spawn_house", palette.resourceBuildingPath, ToWorld(region.bounds.center + new Vector2(-3f, 2f)), true, 35, regionId: region.id);
                        break;
                    case RegionType.WetlandBelt:
                        MapGenerationUtility.AddDecoration(save.decorations, "wetland_tower", palette.towerPath, ToWorld(region.bounds.center + new Vector2(2f, 2f)), true, 35, regionId: region.id);
                        break;
                    case RegionType.ResourceForest:
                        MapGenerationUtility.AddDecoration(save.decorations, "forest_wood", palette.woodResourcePath, ToWorld(region.bounds.center + new Vector2(-3f, -2f)), true, 30, regionId: region.id);
                        MapGenerationUtility.AddDecoration(save.decorations, "forest_gold", palette.goldResourcePath, ToWorld(region.bounds.center + new Vector2(3f, -2f)), true, 30, regionId: region.id);
                        break;
                    case RegionType.RuinedVillage:
                        MapGenerationUtility.AddDecoration(save.decorations, "village_monastery", palette.rewardBuildingPath, ToWorld(region.bounds.center + new Vector2(-1f, 1.5f)), true, 38, regionId: region.id);
                        break;
                    case RegionType.HighPlateauCitadel:
                        MapGenerationUtility.AddDecoration(save.decorations, "citadel", palette.exitBuildingPath, ToWorld(region.bounds.center + new Vector2(0f, 3f)), true, 45, regionId: region.id, scale: new Vector3(1.25f, 1.25f, 1f));
                        break;
                }

                MapGenerationUtility.AddDecoration(save.decorations, $"{region.id}_tree_a", palette.treePaths[0], ToWorld(new Vector2(region.bounds.xMin + 1.5f, region.bounds.yMax - 1.5f)), true, 50, regionId: region.id);
                MapGenerationUtility.AddDecoration(save.decorations, $"{region.id}_tree_b", palette.treePaths[Mathf.Min(1, palette.treePaths.Count - 1)], ToWorld(new Vector2(region.bounds.xMax - 1.8f, region.bounds.yMin + 1.4f)), true, 50, regionId: region.id);
                MapGenerationUtility.AddDecoration(save.decorations, $"{region.id}_rock", palette.rockPaths[0], ToWorld(new Vector2(region.bounds.center.x + 2f, region.bounds.center.y - 1f)), true, 28, regionId: region.id, scale: new Vector3(0.9f, 0.9f, 1f));
            }
        }

        private static void AddAmbient(OpenWorldSaveData save, List<RegionNodeDefinition> regions, MapPalette palette)
        {
            foreach (var region in regions)
            {
                if (palette.bushFrames.Count > 0)
                {
                    MapGenerationUtility.AddAnimated(save.animatedPlacements, $"{region.id}_bush", palette.bushFrames, AnimationChannel.AnimatedVegetation, ActivationPolicy.ByRegion, ToWorld(region.bounds.center + new Vector2(-2f, -2f)), 5f, regionId: region.id, activationRadius: 10f);
                }

                if (palette.waterRockFrames.Count > 0 && (region.regionType == RegionType.WetlandBelt || region.regionType == RegionType.ResourceForest))
                {
                    MapGenerationUtility.AddAnimated(save.animatedPlacements, $"{region.id}_water_rocks", palette.waterRockFrames, AnimationChannel.AmbientProps, ActivationPolicy.ByCameraProximity, ToWorld(region.bounds.center + new Vector2(4f, -4f)), 4f, regionId: region.id, activationRadius: 10f);
                }

                if (palette.fireFrames.Count > 0 && (region.regionType == RegionType.RuinedVillage || region.regionType == RegionType.HighPlateauCitadel))
                {
                    MapGenerationUtility.AddAnimated(save.animatedPlacements, $"{region.id}_fire", palette.fireFrames, AnimationChannel.AmbientProps, ActivationPolicy.ByRegion, ToWorld(region.bounds.center + new Vector2(-3f, 2f)), 6f, regionId: region.id);
                }

                if (palette.dustFrames.Count > 0 && region.regionType == RegionType.HighPlateauCitadel)
                {
                    MapGenerationUtility.AddAnimated(save.animatedPlacements, $"{region.id}_dust", palette.dustFrames, AnimationChannel.ReactiveFX, ActivationPolicy.ByEncounterState, ToWorld(region.bounds.center), 7f, regionId: region.id);
                }
            }
        }

        private static void AddEncounters(OpenWorldSaveData save, List<RegionNodeDefinition> regions)
        {
            foreach (var region in regions.Where(region => region.regionType != RegionType.SpawnMeadow))
            {
                var encounterDefinitions = new List<EncounterDefinition>
                {
                    MapGenerationUtility.CreateEncounter(
                        $"{region.id}_encounter_primary",
                        new RectInt(region.bounds.xMin + 2, region.bounds.yMin + 2, region.bounds.width - 4, region.bounds.height - 4),
                        string.Empty,
                        region.id,
                        isSummonEncounter: region.regionType == RegionType.WetlandBelt || region.regionType == RegionType.RuinedVillage,
                        new EnemySpawnData { enemyId = "torch_goblin_project_ext", position = ToWorld(region.bounds.center + new Vector2(-2f, 0f)), count = 2 },
                        new EnemySpawnData { enemyId = "tnt_goblin_project_ext", position = ToWorld(region.bounds.center + new Vector2(2f, 0f)), count = region.regionType == RegionType.ResourceForest ? 1 : 2 }),
                };

                save.regionEncounters.Add(new RegionEncounterDefinition
                {
                    regionId = region.id,
                    encounterBounds = region.bounds,
                    encounters = encounterDefinitions,
                });
            }
        }

        private static RegionNodeDefinition CloneRegion(RegionNodeDefinition region, bool fixedLayout, System.Random random)
        {
            var clone = new RegionNodeDefinition
            {
                id = region.id,
                regionType = region.regionType,
                bounds = region.bounds,
            };

            if (fixedLayout)
            {
                return clone;
            }

            clone.bounds.position += new Vector2Int(random.Next(-2, 3), random.Next(-1, 2));
            return clone;
        }

        private static Vector3 ToWorld(Vector2 position)
        {
            return new Vector3(position.x, position.y, 0f);
        }
    }
}
