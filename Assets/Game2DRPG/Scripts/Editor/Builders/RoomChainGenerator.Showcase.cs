#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections.Generic;
using Game2DRPG.Map.Runtime;
using UnityEngine;

namespace Game2DRPG.Map.Editor
{
    internal static partial class RoomChainGenerator
    {
        private const string CastlePath = "Assets/Tiny Swords/Buildings/Blue Buildings/Castle.png";
        private const string TowerPath = "Assets/Tiny Swords/Buildings/Blue Buildings/Tower.png";
        private const string House1Path = "Assets/Tiny Swords/Buildings/Blue Buildings/House1.png";
        private const string House2Path = "Assets/Tiny Swords/Buildings/Blue Buildings/House2.png";
        private const string House3Path = "Assets/Tiny Swords/Buildings/Blue Buildings/House3.png";
        private const string BarracksPath = "Assets/Tiny Swords/Buildings/Blue Buildings/Barracks.png";
        private const string WarriorGuardPath = "Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior_Guard.png";
        private const string ArcherIdlePath = "Assets/Tiny Swords/Units/Blue Units/Archer/Archer_Idle.png";
        private const string LancerUpDefencePath = "Assets/Tiny Swords/Units/Blue Units/Lancer/Lancer_Up_Defence.png";
        private const string LancerRightDefencePath = "Assets/Tiny Swords/Units/Blue Units/Lancer/Lancer_Right_Defence.png";
        private const string SheepGrassPath = "Assets/Tiny Swords/Pawn and Resources/Meat/Sheep/Sheep_Grass.png";

        private static MapSaveData GenerateReferenceShowcase(ResourceCatalogAsset catalog, int seed, string layoutId)
        {
            var palette = MapPalette.Create(catalog);
            var save = new MapSaveData
            {
                schemaVersion = 3,
                mode = nameof(MapMode.RoomChain),
                seed = seed,
                layoutId = string.IsNullOrWhiteSpace(layoutId) ? "roomchain_reference_showcase" : $"{layoutId}_reference_showcase",
            };

            var worldRect = new RectInt(0, 0, 40, 30);
            var roomByCell = new Dictionary<Vector2Int, string>();
            var flatCells = new HashSet<Vector2Int>();
            var level1TopCells = new HashSet<Vector2Int>();
            var level2TopCells = new HashSet<Vector2Int>();
            var stairUpperLevel1 = new HashSet<Vector2Int>();
            var stairLowerLevel1 = new HashSet<Vector2Int>();
            var stairUpperLevel2 = new HashSet<Vector2Int>();
            var stairLowerLevel2 = new HashSet<Vector2Int>();
            var cliffCells = new Dictionary<Vector2Int, TerrainSemantic>();
            var shadowCells = new Dictionary<Vector2Int, TerrainSemantic>();
            var stairs = new List<StairPlacement>();

            AddRect(flatCells, new RectInt(1, 4, 10, 7), "elevation_arena", roomByCell);
            AddRect(flatCells, new RectInt(1, 11, 4, 4), "bridge_room", roomByCell);
            AddRect(flatCells, new RectInt(8, 11, 16, 4), "bridge_room", roomByCell);
            AddRect(flatCells, new RectInt(11, 15, 16, 4), "narrow_pass", roomByCell);
            AddRect(flatCells, new RectInt(24, 11, 12, 8), "reward_room", roomByCell);
            AddRect(flatCells, new RectInt(24, 4, 13, 7), "exit_gate", roomByCell);
            AddRect(flatCells, new RectInt(15, 2, 4, 2), "bridge_room", roomByCell);
            AddRect(flatCells, new RectInt(2, 15, 15, 10), "start_room", roomByCell);
            AddRect(flatCells, new RectInt(18, 15, 10, 3), "narrow_pass", roomByCell);
            AddRect(flatCells, new RectInt(22, 19, 11, 6), "resource_court", roomByCell);

            RemoveRect(flatCells, roomByCell, new RectInt(11, 8, 12, 5));
            RemoveRect(flatCells, roomByCell, new RectInt(19, 18, 2, 4));
            RemoveRect(flatCells, roomByCell, new RectInt(31, 18, 2, 2));
            RemoveRect(flatCells, roomByCell, new RectInt(1, 13, 1, 1));
            RemoveRect(flatCells, roomByCell, new RectInt(10, 4, 1, 1));
            RemoveRect(flatCells, roomByCell, new RectInt(35, 10, 1, 1));
            RemoveRect(flatCells, roomByCell, new RectInt(24, 4, 1, 1));
            RemoveRect(flatCells, roomByCell, new RectInt(35, 4, 2, 1));

            MoveRectToSet(new RectInt(2, 18, 12, 7), level1TopCells, flatCells, "start_room", roomByCell);
            MoveRectToSet(new RectInt(13, 15, 13, 3), level1TopCells, flatCells, "narrow_pass", roomByCell);
            MoveRectToSet(new RectInt(22, 19, 10, 5), level1TopCells, flatCells, "resource_court", roomByCell);
            MoveRectToSet(new RectInt(27, 6, 8, 4), level1TopCells, flatCells, "exit_gate", roomByCell);

            stairs.Add(new StairPlacement
            {
                upper = new Vector2Int(10, 18),
                lower = new Vector2Int(10, 17),
                opensRight = true,
                level = 1,
                roomId = "start_room",
            });
            stairs.Add(new StairPlacement
            {
                upper = new Vector2Int(16, 15),
                lower = new Vector2Int(16, 14),
                opensRight = true,
                level = 1,
                roomId = "narrow_pass",
            });
            stairs.Add(new StairPlacement
            {
                upper = new Vector2Int(24, 15),
                lower = new Vector2Int(24, 14),
                opensRight = false,
                level = 1,
                roomId = "narrow_pass",
            });
            stairs.Add(new StairPlacement
            {
                upper = new Vector2Int(24, 19),
                lower = new Vector2Int(24, 18),
                opensRight = true,
                level = 1,
                roomId = "resource_court",
            });
            stairs.Add(new StairPlacement
            {
                upper = new Vector2Int(29, 6),
                lower = new Vector2Int(29, 5),
                opensRight = false,
                level = 1,
                roomId = "exit_gate",
            });

            NormalizeStairs(
                stairs,
                flatCells,
                level1TopCells,
                level2TopCells,
                stairUpperLevel1,
                stairLowerLevel1,
                stairUpperLevel2,
                stairLowerLevel2,
                roomByCell);

            var baseWalkable = new HashSet<Vector2Int>(flatCells);
            baseWalkable.UnionWith(stairLowerLevel1);
            var level1Walkable = new HashSet<Vector2Int>(level1TopCells);
            level1Walkable.UnionWith(stairUpperLevel1);

            BuildShadows(level1TopCells, 1, shadowCells, worldRect);
            BuildCliffs(level1TopCells, baseWalkable, stairUpperLevel1, stairLowerLevel1, 1, cliffCells, roomByCell, worldRect);

            var allWalkable = new HashSet<Vector2Int>(baseWalkable);
            allWalkable.UnionWith(level1Walkable);
            var shorelineCells = CreateShorelineSource(flatCells, level1TopCells, level2TopCells, stairUpperLevel1, stairLowerLevel1, stairUpperLevel2, stairLowerLevel2, cliffCells.Keys);

            var layerBuffer = new LayerBuffer();
            FillBackground(layerBuffer, worldRect, palette.backgroundTilePath);
            PaintFlatGround(layerBuffer, palette, flatCells, stairLowerLevel1, stairLowerLevel2);
            PaintElevatedGround(layerBuffer, palette, level1TopCells, stairUpperLevel1, stairLowerLevel1, new HashSet<Vector2Int>(), 1);
            PaintCliffs(layerBuffer, palette, cliffCells);
            PaintShadows(layerBuffer, palette, shadowCells);
            PaintWaterFoam(layerBuffer, palette, worldRect, shorelineCells);
            save.tileLayers = layerBuffer.ToLayerData();

            PopulateSemanticData(
                save,
                worldRect,
                roomByCell,
                flatCells,
                level1TopCells,
                level2TopCells,
                stairUpperLevel1,
                stairLowerLevel1,
                stairUpperLevel2,
                stairLowerLevel2,
                cliffCells,
                shadowCells,
                allWalkable);

            MapGenerationUtility.AddMarker(save.markers, "player_start", MarkerType.PlayerStart, ToWorld(new Vector2Int(18, 16)), roomId: "narrow_pass");
            MapGenerationUtility.AddMarker(save.markers, "reward_shrine", MarkerType.RewardSpawn, ToWorld(new Vector2Int(31, 14)), roomId: "reward_room");
            MapGenerationUtility.AddMarker(save.markers, "exit_gate", MarkerType.ExitSpawn, ToWorld(new Vector2Int(30, 7)), roomId: "exit_gate");

            AddReferenceBuildings(save);
            AddReferenceUnits(save);
            AddReferenceEnvironment(save, palette);
            AddReferenceEncounters(save);

            return save;
        }

        private static void AddReferenceBuildings(MapSaveData save)
        {
            MapGenerationUtility.AddDecoration(save.decorations, "citadel_castle", CastlePath, ToWorld(new Vector2Int(5, 22)), false, 52, roomId: "start_room", scale: new Vector3(1.18f, 1.18f, 1f));
            AddBlockedFootprint(save, new Vector2Int(5, 22), "start_room", 3, 2);

            MapGenerationUtility.AddDecoration(save.decorations, "west_tower", TowerPath, ToWorld(new Vector2Int(2, 11)), false, 46, roomId: "bridge_room", scale: new Vector3(1.08f, 1.08f, 1f));
            AddBlockedFootprint(save, new Vector2Int(2, 11), "bridge_room", 2, 2);

            MapGenerationUtility.AddDecoration(save.decorations, "south_tower", TowerPath, ToWorld(new Vector2Int(17, 3)), false, 46, roomId: "bridge_room", scale: new Vector3(1.08f, 1.08f, 1f));
            AddBlockedFootprint(save, new Vector2Int(17, 3), "bridge_room", 2, 2);

            MapGenerationUtility.AddDecoration(save.decorations, "village_house_a", House1Path, ToWorld(new Vector2Int(28, 14)), false, 42, roomId: "reward_room");
            AddBlockedFootprint(save, new Vector2Int(28, 14), "reward_room", 2, 2);
            MapGenerationUtility.AddDecoration(save.decorations, "village_house_b", House2Path, ToWorld(new Vector2Int(31, 13)), false, 42, roomId: "reward_room");
            AddBlockedFootprint(save, new Vector2Int(31, 13), "reward_room", 2, 2);
            MapGenerationUtility.AddDecoration(save.decorations, "village_house_c", House3Path, ToWorld(new Vector2Int(33, 12)), false, 42, roomId: "reward_room");
            AddBlockedFootprint(save, new Vector2Int(33, 12), "reward_room", 2, 2);
            MapGenerationUtility.AddDecoration(save.decorations, "village_barracks", BarracksPath, ToWorld(new Vector2Int(28, 7)), false, 44, roomId: "exit_gate");
            AddBlockedFootprint(save, new Vector2Int(28, 7), "exit_gate", 2, 2);
        }

        private static void AddReferenceUnits(MapSaveData save)
        {
            AddGuard(save, "guard_lancer_a", LancerUpDefencePath, new Vector2Int(7, 21), "start_room");
            AddGuard(save, "guard_lancer_b", LancerRightDefencePath, new Vector2Int(9, 20), "start_room");
            AddGuard(save, "guard_archer_a", ArcherIdlePath, new Vector2Int(11, 22), "start_room");
            AddGuard(save, "guard_archer_b", ArcherIdlePath, new Vector2Int(12, 20), "start_room");
            AddGuard(save, "guard_bridge_warrior", WarriorGuardPath, new Vector2Int(18, 16), "narrow_pass", 46, new Vector3(1.08f, 1.08f, 1f));
            AddGuard(save, "guard_bridge_lancer", LancerUpDefencePath, new Vector2Int(19, 16), "narrow_pass", 46, new Vector3(1.08f, 1.08f, 1f));
            AddGuard(save, "guard_south_archer", ArcherIdlePath, new Vector2Int(28, 6), "exit_gate");
            AddGuard(save, "guard_south_warrior", WarriorGuardPath, new Vector2Int(31, 6), "exit_gate");

            MapGenerationUtility.AddDecoration(save.decorations, "sheep_a", SheepGrassPath, ToWorld(new Vector2Int(28, 12)), false, 34, roomId: "reward_room", scale: Vector3.one * 0.8f);
            MapGenerationUtility.AddDecoration(save.decorations, "sheep_b", SheepGrassPath, ToWorld(new Vector2Int(30, 12)), false, 34, roomId: "reward_room", scale: Vector3.one * 0.8f);
            MapGenerationUtility.AddDecoration(save.decorations, "sheep_c", SheepGrassPath, ToWorld(new Vector2Int(34, 11)), false, 34, roomId: "reward_room", scale: Vector3.one * 0.8f);
        }

        private static void AddReferenceEnvironment(MapSaveData save, MapPalette palette)
        {
            AddAnimatedEnvironment(save, "forest_tree_a", palette.treeAnimations[0], new Vector2Int(24, 23), "resource_court", 30f, Vector3.one);
            AddAnimatedEnvironment(save, "forest_tree_b", palette.treeAnimations[1], new Vector2Int(26, 23), "resource_court", 30f, Vector3.one);
            AddAnimatedEnvironment(save, "forest_tree_c", palette.treeAnimations[2], new Vector2Int(28, 22), "resource_court", 30f, Vector3.one);
            AddAnimatedEnvironment(save, "forest_tree_d", palette.treeAnimations[3], new Vector2Int(31, 21), "resource_court", 30f, Vector3.one);
            AddAnimatedEnvironment(save, "village_tree_a", palette.treeAnimations[1], new Vector2Int(35, 17), "reward_room", 30f, Vector3.one);
            AddAnimatedEnvironment(save, "village_tree_b", palette.treeAnimations[2], new Vector2Int(34, 8), "exit_gate", 30f, Vector3.one);
            AddAnimatedEnvironment(save, "village_tree_c", palette.treeAnimations[0], new Vector2Int(32, 6), "exit_gate", 30f, Vector3.one);
            AddAnimatedEnvironment(save, "cliff_tree", palette.treeAnimations[3], new Vector2Int(6, 14), "bridge_room", 30f, Vector3.one);

            AddBlockedFootprint(save, new Vector2Int(24, 23), "resource_court", 1, 1);
            AddBlockedFootprint(save, new Vector2Int(26, 23), "resource_court", 1, 1);
            AddBlockedFootprint(save, new Vector2Int(28, 22), "resource_court", 1, 1);
            AddBlockedFootprint(save, new Vector2Int(31, 21), "resource_court", 1, 1);
            AddBlockedFootprint(save, new Vector2Int(35, 17), "reward_room", 1, 1);
            AddBlockedFootprint(save, new Vector2Int(34, 8), "exit_gate", 1, 1);
            AddBlockedFootprint(save, new Vector2Int(32, 6), "exit_gate", 1, 1);
            AddBlockedFootprint(save, new Vector2Int(6, 14), "bridge_room", 1, 1);

            AddAnimatedEnvironment(save, "bush_bridge", palette.bushAnimations[0], new Vector2Int(15, 13), "bridge_room", 26f, Vector3.one);
            AddAnimatedEnvironment(save, "bush_village", palette.bushAnimations[1], new Vector2Int(34, 13), "reward_room", 26f, Vector3.one);
            AddAnimatedEnvironment(save, "bush_exit", palette.bushAnimations[2], new Vector2Int(26, 5), "exit_gate", 26f, Vector3.one);
            AddAnimatedEnvironment(save, "bush_southwest", palette.bushAnimations[3], new Vector2Int(6, 5), "elevation_arena", 26f, Vector3.one);

            AddAnimatedEnvironment(save, "water_rock_a", palette.waterRockAnimations[0], new Vector2Int(14, 12), "bridge_room", 24f, Vector3.one);
            AddAnimatedEnvironment(save, "water_rock_b", palette.waterRockAnimations[1], new Vector2Int(20, 11), "bridge_room", 24f, Vector3.one);
            AddAnimatedEnvironment(save, "water_rock_c", palette.waterRockAnimations[2], new Vector2Int(18, 8), "bridge_room", 24f, Vector3.one);
            AddAnimatedEnvironment(save, "water_rock_d", palette.waterRockAnimations[3], new Vector2Int(3, 3), "elevation_arena", 24f, Vector3.one);
        }

        private static void AddReferenceEncounters(MapSaveData save)
        {
            save.encounters.Add(MapGenerationUtility.CreateEncounter(
                "southwest_encounter",
                new RectInt(2, 4, 8, 6),
                "elevation_arena",
                string.Empty,
                isSummonEncounter: false,
                new EnemySpawnData
                {
                    enemyId = "torch_goblin_project_ext",
                    position = ToWorld(new Vector2Int(5, 7)),
                    count = 1,
                },
                new EnemySpawnData
                {
                    enemyId = "torch_goblin_project_ext",
                    position = ToWorld(new Vector2Int(8, 6)),
                    count = 1,
                },
                new EnemySpawnData
                {
                    enemyId = "tnt_goblin_project_ext",
                    position = ToWorld(new Vector2Int(7, 9)),
                    count = 1,
                }));

            save.encounters.Add(MapGenerationUtility.CreateEncounter(
                "village_encounter",
                new RectInt(25, 10, 10, 7),
                "reward_room",
                string.Empty,
                isSummonEncounter: false,
                new EnemySpawnData
                {
                    enemyId = "torch_goblin_project_ext",
                    position = ToWorld(new Vector2Int(27, 15)),
                    count = 1,
                },
                new EnemySpawnData
                {
                    enemyId = "torch_goblin_project_ext",
                    position = ToWorld(new Vector2Int(33, 15)),
                    count = 1,
                },
                new EnemySpawnData
                {
                    enemyId = "tnt_goblin_project_ext",
                    position = ToWorld(new Vector2Int(31, 10)),
                    count = 1,
                }));
        }

        private static void AddGuard(MapSaveData save, string id, string assetPath, Vector2Int cell, string roomId, int sortingOrder = 45, Vector3? scale = null)
        {
            MapGenerationUtility.AddDecoration(save.decorations, id, assetPath, ToWorld(cell), false, sortingOrder, roomId: roomId, scale: scale ?? Vector3.one);
        }

        private static void RemoveRect(HashSet<Vector2Int> target, Dictionary<Vector2Int, string> roomByCell, RectInt rect)
        {
            for (var x = rect.xMin; x < rect.xMax; x++)
            {
                for (var y = rect.yMin; y < rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    target.Remove(cell);
                    roomByCell.Remove(cell);
                }
            }
        }
    }
}