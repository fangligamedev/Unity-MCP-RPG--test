#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Game2DRPG.Map.Runtime;
using UnityEngine;

namespace Game2DRPG.Map.Editor
{
    internal static partial class RoomChainGenerator
    {
        private sealed class LayerBuffer
        {
            private readonly Dictionary<string, Dictionary<Vector3Int, string>> _layers = new(StringComparer.Ordinal);

            public void Set(string layerId, Vector2Int position, string assetPath)
            {
                Set(layerId, new Vector3Int(position.x, position.y, 0), assetPath);
            }

            public void Set(string layerId, Vector3Int position, string assetPath)
            {
                if (!_layers.TryGetValue(layerId, out var layer))
                {
                    layer = new Dictionary<Vector3Int, string>();
                    _layers[layerId] = layer;
                }

                layer[position] = assetPath;
            }

            public List<PlacedTileLayerData> ToLayerData()
            {
                return _layers
                    .Select(pair => new PlacedTileLayerData
                    {
                        layerId = pair.Key,
                        tiles = pair.Value
                            .OrderBy(item => item.Key.y)
                            .ThenBy(item => item.Key.x)
                            .Select(item => new PlacedTileData
                            {
                                position = item.Key,
                                assetPath = item.Value,
                            })
                            .ToList(),
                    })
                    .ToList();
            }
        }

        private sealed class StairPlacement
        {
            public Vector2Int upper;
            public Vector2Int lower;
            public bool opensRight;
            public int level;
            public string roomId = string.Empty;
        }

        private sealed class RoomPlacement
        {
            public RoomNodeDefinition room = null!;
            public Vector2Int playerAnchor;
            public Vector2Int rewardAnchor;
            public Vector2Int exitAnchor;
            public List<Vector2Int> combatSpawns = new();
            public List<Vector2Int> summonSpawns = new();
        }

        public static MapSaveData GenerateShowcase(ResourceCatalogAsset catalog, LevelLayoutAsset layout, int seed)
        {
            return GenerateReferenceShowcase(catalog, seed, layout.id);
        }

        public static MapSaveData GenerateRandom(ResourceCatalogAsset catalog, LevelLayoutAsset layout, RoomChainProfile profile, int seed)
        {
            return GenerateInternal(catalog, layout, seed, randomizeRooms: true);
        }

        private static MapSaveData GenerateComplianceIsland(ResourceCatalogAsset catalog, int seed, string layoutId)
        {
            var palette = MapPalette.Create(catalog);
            var save = new MapSaveData
            {
                schemaVersion = 3,
                mode = nameof(MapMode.RoomChain),
                seed = seed,
                layoutId = string.IsNullOrEmpty(layoutId) ? "roomchain_compliance_island" : layoutId,
            };

            var worldRect = new RectInt(0, 0, 24, 24);
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

            AddRect(flatCells, new RectInt(5, 16, 12, 5), "start_room", roomByCell);
            AddRect(flatCells, new RectInt(8, 12, 4, 4), "start_room", roomByCell);
            AddRect(flatCells, new RectInt(4, 3, 14, 8), "combat_room", roomByCell);
            AddRect(flatCells, new RectInt(2, 5, 2, 3), "combat_room", roomByCell);
            AddRect(flatCells, new RectInt(18, 5, 2, 3), "combat_room", roomByCell);

            var plateau = new RectInt(8, 6, 7, 3);
            MoveRectToSet(plateau, level1TopCells, flatCells, "exit_room", roomByCell);
            stairs.Add(new StairPlacement
            {
                upper = new Vector2Int(11, 6),
                lower = new Vector2Int(11, 5),
                opensRight = true,
                level = 1,
                roomId = "exit_room",
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

            MapGenerationUtility.AddMarker(save.markers, "player_start", MarkerType.PlayerStart, ToWorld(new Vector2Int(11, 18)), roomId: "start_room");
            MapGenerationUtility.AddMarker(save.markers, "reward_shrine", MarkerType.RewardSpawn, ToWorld(new Vector2Int(15, 18)), roomId: "start_room");
            MapGenerationUtility.AddMarker(save.markers, "exit_gate", MarkerType.ExitSpawn, ToWorld(new Vector2Int(11, 7)), roomId: "exit_room");

            MapGenerationUtility.AddDecoration(save.decorations, "showcase_house", palette.resourceBuildingPath, ToWorld(new Vector2Int(11, 18)), false, 40, roomId: "start_room", scale: new Vector3(1.1f, 1.1f, 1f));
            MapGenerationUtility.AddDecoration(save.decorations, "showcase_log", palette.woodResourcePath, ToWorld(new Vector2Int(8, 17)), false, 36, roomId: "start_room", scale: Vector3.one * 0.85f);
            MapGenerationUtility.AddDecoration(save.decorations, "showcase_gold", palette.goldResourcePath, ToWorld(new Vector2Int(14, 17)), false, 36, roomId: "start_room", scale: Vector3.one * 0.85f);
            AddBlockedFootprint(save, new Vector2Int(11, 18), "start_room", 2, 2);

            AddAnimatedEnvironment(save, "combat_tree_left", palette.treeAnimations[0], new Vector2Int(6, 8), "combat_room", 20f, Vector3.one);
            AddAnimatedEnvironment(save, "combat_tree_right", palette.treeAnimations[1], new Vector2Int(17, 5), "combat_room", 20f, Vector3.one);
            AddAnimatedEnvironment(save, "combat_bush", palette.bushAnimations[0], new Vector2Int(10, 9), "combat_room", 18f, Vector3.one);
            AddAnimatedEnvironment(save, "water_rock_left", palette.waterRockAnimations[0], new Vector2Int(3, 6), "combat_room", 18f, Vector3.one);
            AddAnimatedEnvironment(save, "reward_fire", palette.fireAnimations[0], new Vector2Int(14, 18), "start_room", 18f, Vector3.one);
            AddBlockedFootprint(save, new Vector2Int(6, 8), "combat_room", 1, 1);
            AddBlockedFootprint(save, new Vector2Int(17, 5), "combat_room", 1, 1);

            save.encounters.Add(MapGenerationUtility.CreateEncounter(
                "combat_room_encounter",
                new RectInt(5, 4, 12, 6),
                "combat_room",
                string.Empty,
                isSummonEncounter: false,
                new EnemySpawnData
                {
                    enemyId = "torch_goblin_project_ext",
                    position = ToWorld(new Vector2Int(7, 5)),
                    count = 1,
                },
                new EnemySpawnData
                {
                    enemyId = "torch_goblin_project_ext",
                    position = ToWorld(new Vector2Int(15, 5)),
                    count = 1,
                },
                new EnemySpawnData
                {
                    enemyId = "tnt_goblin_project_ext",
                    position = ToWorld(new Vector2Int(14, 9)),
                    count = 1,
                }));

            return save;
        }

        private static MapSaveData GenerateInternal(ResourceCatalogAsset catalog, LevelLayoutAsset layout, int seed, bool randomizeRooms)
        {
            var random = new System.Random(seed);
            var palette = MapPalette.Create(catalog);
            var save = new MapSaveData
            {
                schemaVersion = 3,
                mode = nameof(MapMode.RoomChain),
                seed = seed,
                layoutId = layout.id,
            };

            var rooms = layout.rooms.Select(room => CloneRoom(room, randomizeRooms, random)).ToList();
            var roomPlacements = new Dictionary<string, RoomPlacement>(StringComparer.Ordinal);
            var worldRect = MapGenerationUtility.Expand(MapGenerationUtility.Union(rooms.Select(room => room.bounds)), 8);
            var flatCells = new HashSet<Vector2Int>();
            var level1TopCells = new HashSet<Vector2Int>();
            var level2TopCells = new HashSet<Vector2Int>();
            var stairUpperLevel1 = new HashSet<Vector2Int>();
            var stairLowerLevel1 = new HashSet<Vector2Int>();
            var stairUpperLevel2 = new HashSet<Vector2Int>();
            var stairLowerLevel2 = new HashSet<Vector2Int>();
            var cliffCells = new Dictionary<Vector2Int, TerrainSemantic>();
            var shadowCells = new Dictionary<Vector2Int, TerrainSemantic>();
            var roomByCell = new Dictionary<Vector2Int, string>();
            var stairs = new List<StairPlacement>();

            foreach (var room in rooms)
            {
                roomPlacements[room.id] = PaintRoom(room, flatCells, level1TopCells, level2TopCells, stairs, roomByCell, random);
            }

            foreach (var edge in layout.edges)
            {
                var from = roomPlacements[edge.fromRoomId];
                var to = roomPlacements[edge.toRoomId];
                PaintCorridor(flatCells, roomByCell, from.playerAnchor, to.playerAnchor, edge.isPrimaryPath ? 3 : 2, edge.fromRoomId, edge.toRoomId);
            }

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
            baseWalkable.UnionWith(stairLowerLevel2);

            var level1Walkable = new HashSet<Vector2Int>(level1TopCells);
            level1Walkable.UnionWith(stairUpperLevel1);

            var level2Walkable = new HashSet<Vector2Int>(level2TopCells);
            level2Walkable.UnionWith(stairUpperLevel2);

            BuildShadows(level1TopCells, 1, shadowCells, worldRect);
            BuildCliffs(level1TopCells, baseWalkable, stairUpperLevel1, stairLowerLevel1, 1, cliffCells, roomByCell, worldRect);
            BuildShadows(level2TopCells, 2, shadowCells, worldRect);
            BuildCliffs(level2TopCells, level1Walkable, stairUpperLevel2, stairLowerLevel2, 2, cliffCells, roomByCell, worldRect);

            var allWalkable = new HashSet<Vector2Int>(baseWalkable);
            allWalkable.UnionWith(level1Walkable);
            allWalkable.UnionWith(level2Walkable);
            var shorelineCells = CreateShorelineSource(flatCells, level1TopCells, level2TopCells, stairUpperLevel1, stairLowerLevel1, stairUpperLevel2, stairLowerLevel2, cliffCells.Keys);

            var layerBuffer = new LayerBuffer();
            FillBackground(layerBuffer, worldRect, palette.backgroundTilePath);
            PaintFlatGround(layerBuffer, palette, flatCells, stairLowerLevel1, stairLowerLevel2);
            PaintElevatedGround(layerBuffer, palette, level1TopCells, stairUpperLevel1, stairLowerLevel1, stairUpperLevel2, 1);
            PaintElevatedGround(layerBuffer, palette, level2TopCells, stairUpperLevel2, stairLowerLevel2, new HashSet<Vector2Int>(), 2);
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
            PopulateMarkers(save, roomPlacements);
            PopulateDecorations(save, palette, roomPlacements, allWalkable, worldRect);
            PopulateEncounters(save, roomPlacements);

            return save;
        }

        private static RoomPlacement PaintRoom(
            RoomNodeDefinition room,
            HashSet<Vector2Int> flatCells,
            HashSet<Vector2Int> level1TopCells,
            HashSet<Vector2Int> level2TopCells,
            List<StairPlacement> stairs,
            Dictionary<Vector2Int, string> roomByCell,
            System.Random random)
        {
            var placement = new RoomPlacement { room = room };
            RectInt island;
            switch (room.roomType)
            {
                case RoomType.Start:
                    island = Inset(room.bounds, 1);
                    AddRect(flatCells, island, room.id, roomByCell);
                    placement.playerAnchor = CellCenter(island);
                    break;
                case RoomType.Connector:
                    island = new RectInt(room.bounds.xMin + 1, CellCenter(room.bounds).y - 1, room.bounds.width - 2, 3);
                    AddRect(flatCells, island, room.id, roomByCell);
                    placement.playerAnchor = CellCenter(island);
                    break;
                case RoomType.Resource:
                    island = Inset(room.bounds, 1);
                    AddRect(flatCells, island, room.id, roomByCell);
                    AddRect(flatCells, new RectInt(island.xMin - 1, CellCenter(island).y - 1, 2, 3), room.id, roomByCell);
                    placement.playerAnchor = CellCenter(island);
                    break;
                case RoomType.Combat:
                    island = Inset(room.bounds, 1);
                    AddRect(flatCells, island, room.id, roomByCell);
                    var combatPlateau = ClampRect(new RectInt(room.bounds.xMin + 3, room.bounds.yMin + 4, room.bounds.width - 6, room.bounds.height - 5), 4, 3);
                    MoveRectToSet(combatPlateau, level1TopCells, flatCells, room.id, roomByCell);
                    AddSouthStairs(combatPlateau, 1, room.id, stairs, random.NextDouble() > 0.5d);
                    placement.playerAnchor = new Vector2Int(CellCenter(island).x, island.yMin + 2);
                    placement.combatSpawns.Add(new Vector2Int(island.xMin + 2, CellCenter(island).y));
                    placement.combatSpawns.Add(new Vector2Int(island.xMax - 3, CellCenter(island).y));
                    placement.combatSpawns.Add(new Vector2Int(CellCenter(island).x, island.yMax - 2));
                    break;
                case RoomType.Elite:
                    island = Inset(room.bounds, 1);
                    AddRect(flatCells, island, room.id, roomByCell);
                    var eliteL1 = ClampRect(new RectInt(room.bounds.xMin + 2, room.bounds.yMin + 3, room.bounds.width - 4, room.bounds.height - 4), 5, 4);
                    MoveRectToSet(eliteL1, level1TopCells, flatCells, room.id, roomByCell);
                    var eliteL2 = ClampRect(new RectInt(eliteL1.xMin + 2, eliteL1.yMin + 2, eliteL1.width - 4, eliteL1.height - 3), 3, 3);
                    MoveRectToSet(eliteL2, level2TopCells, level1TopCells, room.id, roomByCell);
                    AddSouthStairs(eliteL1, 1, room.id, stairs, true);
                    AddSouthStairs(eliteL2, 2, room.id, stairs, false);
                    placement.playerAnchor = new Vector2Int(island.xMin + 3, island.yMin + 2);
                    placement.combatSpawns.Add(new Vector2Int(island.xMax - 3, CellCenter(island).y));
                    placement.combatSpawns.Add(new Vector2Int(CellCenter(eliteL1).x, eliteL1.yMin - 1));
                    placement.summonSpawns.Add(new Vector2Int(CellCenter(eliteL2).x, eliteL2.yMin - 1));
                    break;
                case RoomType.Reward:
                    island = Inset(room.bounds, 1);
                    AddRect(flatCells, island, room.id, roomByCell);
                    var rewardPlateau = ClampRect(new RectInt(CellCenter(room.bounds).x - 2, room.bounds.yMin + 3, 4, 3), 4, 3);
                    MoveRectToSet(rewardPlateau, level1TopCells, flatCells, room.id, roomByCell);
                    AddSouthStairs(rewardPlateau, 1, room.id, stairs, false);
                    placement.playerAnchor = new Vector2Int(CellCenter(island).x, island.yMin + 2);
                    placement.rewardAnchor = CellCenter(rewardPlateau);
                    break;
                case RoomType.Exit:
                    island = Inset(room.bounds, 1);
                    AddRect(flatCells, island, room.id, roomByCell);
                    var exitL1 = ClampRect(new RectInt(room.bounds.xMin + 2, room.bounds.yMin + 3, room.bounds.width - 4, room.bounds.height - 4), 5, 4);
                    MoveRectToSet(exitL1, level1TopCells, flatCells, room.id, roomByCell);
                    var exitL2 = ClampRect(new RectInt(CellCenter(exitL1).x - 2, exitL1.yMin + 2, 4, 3), 4, 3);
                    MoveRectToSet(exitL2, level2TopCells, level1TopCells, room.id, roomByCell);
                    AddSouthStairs(exitL1, 1, room.id, stairs, true);
                    AddSouthStairs(exitL2, 2, room.id, stairs, false);
                    placement.playerAnchor = new Vector2Int(CellCenter(island).x, island.yMin + 2);
                    placement.exitAnchor = CellCenter(exitL2);
                    placement.combatSpawns.Add(new Vector2Int(island.xMin + 2, CellCenter(island).y));
                    placement.combatSpawns.Add(new Vector2Int(island.xMax - 3, CellCenter(island).y));
                    placement.summonSpawns.Add(new Vector2Int(CellCenter(exitL1).x, exitL1.yMin - 1));
                    break;
                default:
                    island = Inset(room.bounds, 1);
                    AddRect(flatCells, island, room.id, roomByCell);
                    placement.playerAnchor = CellCenter(island);
                    break;
            }

            if (placement.rewardAnchor == default)
            {
                placement.rewardAnchor = placement.playerAnchor;
            }

            if (placement.exitAnchor == default)
            {
                placement.exitAnchor = placement.playerAnchor;
            }

            if (placement.combatSpawns.Count == 0 && room.roomType != RoomType.Start && room.roomType != RoomType.Reward && room.roomType != RoomType.Connector)
            {
                placement.combatSpawns.Add(placement.playerAnchor + Vector2Int.left * 2);
                placement.combatSpawns.Add(placement.playerAnchor + Vector2Int.right * 2);
            }

            return placement;
        }

        private static void NormalizeStairs(
            IEnumerable<StairPlacement> stairs,
            HashSet<Vector2Int> flatCells,
            HashSet<Vector2Int> level1TopCells,
            HashSet<Vector2Int> level2TopCells,
            HashSet<Vector2Int> stairUpperLevel1,
            HashSet<Vector2Int> stairLowerLevel1,
            HashSet<Vector2Int> stairUpperLevel2,
            HashSet<Vector2Int> stairLowerLevel2,
            Dictionary<Vector2Int, string> roomByCell)
        {
            foreach (var stair in stairs)
            {
                if (stair.level == 1)
                {
                    level1TopCells.Remove(stair.upper);
                    stairUpperLevel1.Add(stair.upper);
                    stairLowerLevel1.Add(stair.lower);
                }
                else
                {
                    level2TopCells.Remove(stair.upper);
                    stairUpperLevel2.Add(stair.upper);
                    stairLowerLevel2.Add(stair.lower);
                    level1TopCells.Remove(stair.lower);
                }

                flatCells.Add(stair.lower);
                roomByCell[stair.upper] = stair.roomId;
                roomByCell[stair.lower] = stair.roomId;
            }
        }

        private static void BuildShadows(
            HashSet<Vector2Int> topCells,
            int level,
            Dictionary<Vector2Int, TerrainSemantic> shadowCells,
            RectInt worldRect)
        {
            foreach (var cell in topCells)
            {
                var shadowCell = cell + Vector2Int.down;
                if (!worldRect.Contains(shadowCell))
                {
                    continue;
                }

                shadowCells[shadowCell] = level == 1 ? TerrainSemantic.ShadowL1 : TerrainSemantic.ShadowL2;
            }
        }

        private static void BuildCliffs(
            HashSet<Vector2Int> topCells,
            HashSet<Vector2Int> lowerWalkable,
            HashSet<Vector2Int> stairUpper,
            HashSet<Vector2Int> stairLower,
            int level,
            Dictionary<Vector2Int, TerrainSemantic> cliffCells,
            Dictionary<Vector2Int, string> roomByCell,
            RectInt worldRect)
        {
            foreach (var cell in topCells)
            {
                var south = cell + Vector2Int.down;
                if (topCells.Contains(south) || stairUpper.Contains(south))
                {
                    continue;
                }

                if (!worldRect.Contains(south))
                {
                    continue;
                }

                var southWalkable = lowerWalkable.Contains(south) || stairLower.Contains(south);
                if (southWalkable)
                {
                    cliffCells[south] = level == 1 ? TerrainSemantic.CliffToGroundL1 : TerrainSemantic.CliffToGroundL2;
                    if (roomByCell.TryGetValue(cell, out var roomId))
                    {
                        roomByCell[south] = roomId;
                    }

                    continue;
                }

                cliffCells[south] = level == 1 ? TerrainSemantic.CliffToWaterL1 : TerrainSemantic.CliffToWaterL2;
                if (roomByCell.TryGetValue(cell, out var lowerRoomId))
                {
                    roomByCell[south] = lowerRoomId;
                }

                var south2 = south + Vector2Int.down;
                if (!worldRect.Contains(south2))
                {
                    continue;
                }

                cliffCells[south2] = level == 1 ? TerrainSemantic.CliffToWaterL1 : TerrainSemantic.CliffToWaterL2;
                if (roomByCell.TryGetValue(cell, out var waterRoomId))
                {
                    roomByCell[south2] = waterRoomId;
                }
            }
        }

        private static HashSet<Vector2Int> CreateShorelineSource(
            HashSet<Vector2Int> flatCells,
            HashSet<Vector2Int> level1TopCells,
            HashSet<Vector2Int> level2TopCells,
            HashSet<Vector2Int> stairUpperLevel1,
            HashSet<Vector2Int> stairLowerLevel1,
            HashSet<Vector2Int> stairUpperLevel2,
            HashSet<Vector2Int> stairLowerLevel2,
            IEnumerable<Vector2Int> cliffCells)
        {
            var shoreline = new HashSet<Vector2Int>(flatCells);
            shoreline.UnionWith(level1TopCells);
            shoreline.UnionWith(level2TopCells);
            shoreline.UnionWith(stairUpperLevel1);
            shoreline.UnionWith(stairLowerLevel1);
            shoreline.UnionWith(stairUpperLevel2);
            shoreline.UnionWith(stairLowerLevel2);
            shoreline.UnionWith(cliffCells);
            return shoreline;
        }

        private static void FillBackground(LayerBuffer buffer, RectInt worldRect, string assetPath)
        {
            for (var x = worldRect.xMin; x < worldRect.xMax; x++)
            {
                for (var y = worldRect.yMin; y < worldRect.yMax; y++)
                {
                    buffer.Set("BGColor", new Vector2Int(x, y), assetPath);
                }
            }
        }

        private static void PaintFlatGround(LayerBuffer buffer, MapPalette palette, HashSet<Vector2Int> flatCells, HashSet<Vector2Int> stairLowerLevel1, HashSet<Vector2Int> stairLowerLevel2)
        {
            var support = new HashSet<Vector2Int>(flatCells);
            support.UnionWith(stairLowerLevel1);
            support.UnionWith(stairLowerLevel2);
            foreach (var cell in flatCells)
            {
                buffer.Set("FlatGround", cell, palette.GetFlatTile(ResolveShape(support, cell)));
            }
        }

        private static void PaintElevatedGround(
            LayerBuffer buffer,
            MapPalette palette,
            HashSet<Vector2Int> topCells,
            HashSet<Vector2Int> stairUpper,
            HashSet<Vector2Int> stairLower,
            HashSet<Vector2Int> upperStairsOfNextLevel,
            int level)
        {
            var support = new HashSet<Vector2Int>(topCells);
            support.UnionWith(stairUpper);
            var layerId = level == 1 ? "ElevatedGround_L1" : "ElevatedGround_L2";
            foreach (var cell in topCells)
            {
                buffer.Set(layerId, cell, palette.GetElevatedTile(ResolveShape(support, cell)));
            }

            foreach (var stair in stairUpper)
            {
                var opensRight = (stair.x & 1) == 0;
                buffer.Set(layerId, stair, palette.GetStairUpperTile(opensRight));
            }

            foreach (var stair in stairLower)
            {
                var opensRight = (stair.x & 1) == 0;
                buffer.Set(layerId, stair, palette.GetStairLowerTile(opensRight));
            }

            foreach (var upper in upperStairsOfNextLevel)
            {
                if (level == 1)
                {
                    buffer.Set(layerId, upper, palette.GetElevatedTile(ResolveShape(support, upper)));
                }
            }
        }

        private static void PaintCliffs(LayerBuffer buffer, MapPalette palette, IReadOnlyDictionary<Vector2Int, TerrainSemantic> cliffCells)
        {
            var cliffSet = new HashSet<Vector2Int>(cliffCells.Keys);
            foreach (var pair in cliffCells)
            {
                var slot = ResolveCliffSlot(cliffSet, pair.Key);
                var layerId = pair.Value == TerrainSemantic.CliffToGroundL2 || pair.Value == TerrainSemantic.CliffToWaterL2
                    ? "ElevatedGround_L2"
                    : "ElevatedGround_L1";
                var assetPath = IsLowerCliffRow(pair.Key, cliffCells)
                    ? palette.GetCliffLowerTile(slot)
                    : palette.GetCliffUpperTile(slot);
                buffer.Set(layerId, pair.Key, assetPath);
            }
        }

        private static void PaintShadows(LayerBuffer buffer, MapPalette palette, IReadOnlyDictionary<Vector2Int, TerrainSemantic> shadowCells)
        {
            foreach (var pair in shadowCells)
            {
                buffer.Set(pair.Value == TerrainSemantic.ShadowL2 ? "Shadow_L2" : "Shadow_L1", pair.Key, palette.shadowTilePath);
            }
        }

        private static void PaintWaterFoam(LayerBuffer buffer, MapPalette palette, RectInt worldRect, HashSet<Vector2Int> shorelineCells)
        {
            for (var x = worldRect.xMin; x < worldRect.xMax; x++)
            {
                for (var y = worldRect.yMin; y < worldRect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (shorelineCells.Contains(cell))
                    {
                        continue;
                    }

                    if (TouchesLand(cell, shorelineCells))
                    {
                        buffer.Set("WaterFoam", cell, palette.waterFoamTilePath);
                    }
                }
            }
        }

        private static void PopulateSemanticData(
            MapSaveData save,
            RectInt worldRect,
            IReadOnlyDictionary<Vector2Int, string> roomByCell,
            HashSet<Vector2Int> flatCells,
            HashSet<Vector2Int> level1TopCells,
            HashSet<Vector2Int> level2TopCells,
            HashSet<Vector2Int> stairUpperLevel1,
            HashSet<Vector2Int> stairLowerLevel1,
            HashSet<Vector2Int> stairUpperLevel2,
            HashSet<Vector2Int> stairLowerLevel2,
            IReadOnlyDictionary<Vector2Int, TerrainSemantic> cliffCells,
            IReadOnlyDictionary<Vector2Int, TerrainSemantic> shadowCells,
            HashSet<Vector2Int> allWalkable)
        {
            save.terrainCells.Clear();
            save.occupancyCells.Clear();

            for (var x = worldRect.xMin; x < worldRect.xMax; x++)
            {
                for (var y = worldRect.yMin; y < worldRect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    var semantic = ResolveSemantic(cell, flatCells, level1TopCells, level2TopCells, stairUpperLevel1, stairLowerLevel1, stairUpperLevel2, stairLowerLevel2, cliffCells, shadowCells);
                    var walkable = allWalkable.Contains(cell);
                    var roomId = roomByCell.TryGetValue(cell, out var room) ? room : string.Empty;
                    save.terrainCells.Add(new TerrainCellData
                    {
                        position = new Vector3Int(cell.x, cell.y, 0),
                        semantic = semantic,
                        walkable = walkable,
                        roomId = roomId,
                    });

                    save.occupancyCells.Add(new OccupancyCellData
                    {
                        position = new Vector3Int(cell.x, cell.y, 0),
                        walkable = walkable,
                        semantic = semantic,
                        sourceId = roomId,
                    });
                }
            }
        }

        private static void PopulateMarkers(MapSaveData save, IReadOnlyDictionary<string, RoomPlacement> placements)
        {
            save.markers.Clear();
            var start = placements.Values.First(room => room.room.roomType == RoomType.Start);
            MapGenerationUtility.AddMarker(save.markers, "player_start", MarkerType.PlayerStart, ToWorld(start.playerAnchor), roomId: start.room.id);

            var reward = placements.Values.First(room => room.room.roomType == RoomType.Reward);
            MapGenerationUtility.AddMarker(save.markers, "reward_shrine", MarkerType.RewardSpawn, ToWorld(reward.rewardAnchor), roomId: reward.room.id);

            var exit = placements.Values.First(room => room.room.roomType == RoomType.Exit);
            MapGenerationUtility.AddMarker(save.markers, "exit_gate", MarkerType.ExitSpawn, ToWorld(exit.exitAnchor), roomId: exit.room.id);
        }

        private static void PopulateDecorations(MapSaveData save, MapPalette palette, IReadOnlyDictionary<string, RoomPlacement> placements, HashSet<Vector2Int> walkableCells, RectInt worldRect)
        {
            save.decorations.Clear();
            save.animatedPlacements.Clear();

            foreach (var placement in placements.Values)
            {
                var room = placement.room;
                if (room.roomType == RoomType.Reward)
                {
                    MapGenerationUtility.AddDecoration(save.decorations, "reward_monastery", palette.rewardBuildingPath, ToWorld(placement.rewardAnchor + Vector2Int.left), false, 40, roomId: room.id, scale: new Vector3(1.15f, 1.15f, 1f));
                    AddBlockedFootprint(save, placement.rewardAnchor + Vector2Int.left, room.id, 2, 2);
                }
                else if (room.roomType == RoomType.Resource)
                {
                    MapGenerationUtility.AddDecoration(save.decorations, "resource_house", palette.resourceBuildingPath, ToWorld(placement.playerAnchor + Vector2Int.up * 2), false, 38, roomId: room.id);
                    MapGenerationUtility.AddDecoration(save.decorations, "resource_gold", palette.goldResourcePath, ToWorld(placement.playerAnchor + Vector2Int.right * 3), false, 34, roomId: room.id);
                    MapGenerationUtility.AddDecoration(save.decorations, "resource_wood", palette.woodResourcePath, ToWorld(placement.playerAnchor + Vector2Int.left * 3), false, 34, roomId: room.id);
                    AddBlockedFootprint(save, placement.playerAnchor + Vector2Int.up * 2, room.id, 2, 2);
                }
                else if (room.roomType == RoomType.Exit)
                {
                    MapGenerationUtility.AddDecoration(save.decorations, "exit_castle", palette.exitBuildingPath, ToWorld(placement.exitAnchor + Vector2Int.up), false, 46, roomId: room.id, scale: new Vector3(1.2f, 1.2f, 1f));
                    AddBlockedFootprint(save, placement.exitAnchor + Vector2Int.up, room.id, 3, 2);
                }
                else if (room.roomType == RoomType.Elite)
                {
                    MapGenerationUtility.AddDecoration(save.decorations, "bridge_tower", palette.towerPath, ToWorld(placement.playerAnchor + Vector2Int.right * 4 + Vector2Int.up), false, 38, roomId: room.id);
                    AddBlockedFootprint(save, placement.playerAnchor + Vector2Int.right * 4 + Vector2Int.up, room.id, 2, 2);
                }

                AddVegetation(save, palette, placement, walkableCells, worldRect);
            }
        }

        private static void PopulateEncounters(MapSaveData save, IReadOnlyDictionary<string, RoomPlacement> placements)
        {
            save.encounters.Clear();
            foreach (var placement in placements.Values)
            {
                if (placement.room.roomType == RoomType.Start || placement.room.roomType == RoomType.Reward || placement.room.roomType == RoomType.Connector)
                {
                    continue;
                }

                var encounter = MapGenerationUtility.CreateEncounter(
                    $"{placement.room.id}_encounter",
                    placement.room.bounds,
                    placement.room.id,
                    string.Empty,
                    isSummonEncounter: placement.room.roomType == RoomType.Elite,
                    placement.combatSpawns.Select(spawn => new EnemySpawnData
                    {
                        enemyId = "torch_goblin_project_ext",
                        position = ToWorld(spawn),
                        count = 1,
                    }).ToArray());

                foreach (var summonSpawn in placement.summonSpawns)
                {
                    encounter.enemies.Add(new EnemySpawnData
                    {
                        enemyId = "tnt_goblin_project_ext",
                        position = ToWorld(summonSpawn),
                        count = 1,
                    });
                }

                save.encounters.Add(encounter);
            }
        }

        private static void AddVegetation(MapSaveData save, MapPalette palette, RoomPlacement placement, HashSet<Vector2Int> walkableCells, RectInt worldRect)
        {
            var room = placement.room;
            var treeIndexA = WrapIndex(room.bounds.xMin, palette.treeAnimations.Count);
            var treeIndexB = WrapIndex(room.bounds.xMin + 1, palette.treeAnimations.Count);
            var bushIndex = WrapIndex(room.bounds.yMin, palette.bushAnimations.Count);
            var waterRockIndex = WrapIndex(room.bounds.xMin, palette.waterRockAnimations.Count);
            var fireIndex = WrapIndex(room.bounds.yMin, palette.fireAnimations.Count);
            var dustIndex = WrapIndex(room.bounds.yMin, palette.dustAnimations.Count);

            var treeA = FindWalkableNear(new Vector2Int(room.bounds.xMin + 2, room.bounds.yMax - 3), walkableCells, worldRect);
            var treeB = FindWalkableNear(new Vector2Int(room.bounds.xMax - 3, room.bounds.yMin + 2), walkableCells, worldRect);
            AddAnimatedEnvironment(save, $"{room.id}_tree_a", palette.treeAnimations[treeIndexA], treeA, room.id, 18f, Vector3.one);
            AddAnimatedEnvironment(save, $"{room.id}_tree_b", palette.treeAnimations[treeIndexB], treeB, room.id, 18f, Vector3.one);
            AddBlockedFootprint(save, treeA, room.id, 1, 1);
            AddBlockedFootprint(save, treeB, room.id, 1, 1);

            var bush = FindWalkableNear(new Vector2Int(CellCenter(room.bounds).x - 2, CellCenter(room.bounds).y - 1), walkableCells, worldRect);
            AddAnimatedEnvironment(save, $"{room.id}_bush", palette.bushAnimations[bushIndex], bush, room.id, 16f, Vector3.one);

            var waterRockCell = new Vector2Int(room.bounds.xMin - 2, room.bounds.yMin + 1);
            AddAnimatedEnvironment(save, $"{room.id}_water_rock", palette.waterRockAnimations[waterRockIndex], waterRockCell, room.id, 18f, Vector3.one);

            if (room.roomType == RoomType.Reward || room.roomType == RoomType.Exit)
            {
                var fireCell = FindWalkableNear(new Vector2Int(CellCenter(room.bounds).x + 2, CellCenter(room.bounds).y + 1), walkableCells, worldRect);
                AddAnimatedEnvironment(save, $"{room.id}_fire", palette.fireAnimations[fireIndex], fireCell, room.id, 14f, Vector3.one);
            }

            if (room.roomType == RoomType.Combat || room.roomType == RoomType.Elite || room.roomType == RoomType.Exit)
            {
                var dustCell = FindWalkableNear(CellCenter(room.bounds), walkableCells, worldRect);
                AddAnimatedEnvironment(save, $"{room.id}_dust", palette.dustAnimations[dustIndex], dustCell, room.id, 10f, Vector3.one);
            }
        }

        private static void AddAnimatedEnvironment(MapSaveData save, string id, AnimatedEnvironmentDefinition definition, Vector2Int cell, string roomId, float activationRadius, Vector3 scale)
        {
            save.animatedPlacements.Add(new AnimatedPlacementData
            {
                id = id,
                assetPath = definition.spriteAssetPath,
                animatorControllerPath = definition.animatorControllerPath,
                useAnimatorController = true,
                channel = definition.channel,
                activationPolicy = definition.activationPolicy,
                position = ToWorld(cell),
                roomId = roomId,
                activationRadius = activationRadius,
                scale = scale,
            });
        }

        private static void AddBlockedFootprint(MapSaveData save, Vector2Int centerCell, string roomId, int width, int height)
        {
            var startX = centerCell.x - width / 2;
            var startY = centerCell.y - height / 2;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var pos = new Vector3Int(startX + x, startY + y, 0);
                    var existing = save.occupancyCells.FirstOrDefault(cell => cell.position == pos);
                    if (existing != null)
                    {
                        existing.walkable = false;
                        existing.semantic = TerrainSemantic.BlockedDecoration;
                        existing.sourceId = roomId;
                        continue;
                    }

                    save.occupancyCells.Add(new OccupancyCellData
                    {
                        position = pos,
                        walkable = false,
                        semantic = TerrainSemantic.BlockedDecoration,
                        sourceId = roomId,
                    });
                }
            }
        }

        private static SemanticTileShape ResolveShape(HashSet<Vector2Int> cells, Vector2Int cell)
        {
            var north = cells.Contains(cell + Vector2Int.up);
            var south = cells.Contains(cell + Vector2Int.down);
            var west = cells.Contains(cell + Vector2Int.left);
            var east = cells.Contains(cell + Vector2Int.right);

            if (north && south && west && east) return SemanticTileShape.Center;
            if (!north && south && !west && east) return SemanticTileShape.TopLeftCorner;
            if (!north && south && west && !east) return SemanticTileShape.TopRightCorner;
            if (north && !south && !west && east) return SemanticTileShape.BottomLeftCorner;
            if (north && !south && west && !east) return SemanticTileShape.BottomRightCorner;
            if (!north && south && west && east) return SemanticTileShape.TopEdge;
            if (north && !south && west && east) return SemanticTileShape.BottomEdge;
            if (north && south && !west && east) return SemanticTileShape.LeftEdge;
            if (north && south && west && !east) return SemanticTileShape.RightEdge;
            if (!north && south && !west && !east) return SemanticTileShape.TopBridge;
            if (north && !south && !west && !east) return SemanticTileShape.BottomBridge;
            if (!north && !south && west && east) return SemanticTileShape.HorizontalBridge;
            if (north && south && !west && !east) return SemanticTileShape.VerticalBridge;
            if (!north && !south && !west && east) return SemanticTileShape.LeftBridge;
            if (!north && !south && west && !east) return SemanticTileShape.RightBridge;
            return SemanticTileShape.Isolated;
        }

        private static int ResolveCliffSlot(IReadOnlyCollection<Vector2Int> cliffCells, Vector2Int cell)
        {
            var west = cliffCells.Contains(cell + Vector2Int.left);
            var east = cliffCells.Contains(cell + Vector2Int.right);
            if (!west && east) return 0;
            if (west && !east) return 3;
            return (cell.x & 1) == 0 ? 1 : 2;
        }

        private static bool IsLowerCliffRow(Vector2Int cell, IReadOnlyDictionary<Vector2Int, TerrainSemantic> cliffCells)
        {
            var north = cell + Vector2Int.up;
            return cliffCells.TryGetValue(north, out var northSemantic) &&
                   (northSemantic == TerrainSemantic.CliffToWaterL1 || northSemantic == TerrainSemantic.CliffToWaterL2);
        }

        private static TerrainSemantic ResolveSemantic(
            Vector2Int cell,
            HashSet<Vector2Int> flatCells,
            HashSet<Vector2Int> level1TopCells,
            HashSet<Vector2Int> level2TopCells,
            HashSet<Vector2Int> stairUpperLevel1,
            HashSet<Vector2Int> stairLowerLevel1,
            HashSet<Vector2Int> stairUpperLevel2,
            HashSet<Vector2Int> stairLowerLevel2,
            IReadOnlyDictionary<Vector2Int, TerrainSemantic> cliffCells,
            IReadOnlyDictionary<Vector2Int, TerrainSemantic> shadowCells)
        {
            if (level2TopCells.Contains(cell)) return TerrainSemantic.ElevatedTopL2;
            if (level1TopCells.Contains(cell)) return TerrainSemantic.ElevatedTopL1;
            if (stairUpperLevel2.Contains(cell) || stairLowerLevel2.Contains(cell)) return TerrainSemantic.StairsL2;
            if (stairUpperLevel1.Contains(cell) || stairLowerLevel1.Contains(cell)) return TerrainSemantic.StairsL1;
            if (flatCells.Contains(cell)) return TerrainSemantic.FlatGround;
            if (cliffCells.TryGetValue(cell, out var cliffSemantic)) return cliffSemantic;
            if (shadowCells.TryGetValue(cell, out var shadowSemantic)) return shadowSemantic;
            return TerrainSemantic.Water;
        }

        private static bool TouchesLand(Vector2Int cell, IReadOnlyCollection<Vector2Int> solids)
        {
            return solids.Contains(cell + Vector2Int.up) ||
                   solids.Contains(cell + Vector2Int.down) ||
                   solids.Contains(cell + Vector2Int.left) ||
                   solids.Contains(cell + Vector2Int.right);
        }

        private static void PaintCorridor(HashSet<Vector2Int> flatCells, Dictionary<Vector2Int, string> roomByCell, Vector2Int from, Vector2Int to, int width, string fromRoomId, string toRoomId)
        {
            FillCorridorSegment(flatCells, roomByCell, from, new Vector2Int(to.x, from.y), width, fromRoomId);
            FillCorridorSegment(flatCells, roomByCell, new Vector2Int(to.x, from.y), to, width, toRoomId);
        }

        private static void FillCorridorSegment(HashSet<Vector2Int> flatCells, Dictionary<Vector2Int, string> roomByCell, Vector2Int from, Vector2Int to, int width, string roomId)
        {
            if (from.x == to.x)
            {
                var minY = Mathf.Min(from.y, to.y);
                var maxY = Mathf.Max(from.y, to.y);
                for (var x = from.x - width / 2; x <= from.x + width / 2; x++)
                {
                    for (var y = minY; y <= maxY; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        flatCells.Add(cell);
                        roomByCell[cell] = roomId;
                    }
                }

                return;
            }

            if (from.y == to.y)
            {
                var minX = Mathf.Min(from.x, to.x);
                var maxX = Mathf.Max(from.x, to.x);
                for (var x = minX; x <= maxX; x++)
                {
                    for (var y = from.y - width / 2; y <= from.y + width / 2; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        flatCells.Add(cell);
                        roomByCell[cell] = roomId;
                    }
                }
            }
        }

        private static void MoveRectToSet(RectInt rect, HashSet<Vector2Int> destination, HashSet<Vector2Int> source, string roomId, Dictionary<Vector2Int, string> roomByCell)
        {
            for (var x = rect.xMin; x < rect.xMax; x++)
            {
                for (var y = rect.yMin; y < rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    source.Remove(cell);
                    destination.Add(cell);
                    roomByCell[cell] = roomId;
                }
            }
        }

        private static void AddRect(HashSet<Vector2Int> target, RectInt rect, string roomId, Dictionary<Vector2Int, string> roomByCell)
        {
            for (var x = rect.xMin; x < rect.xMax; x++)
            {
                for (var y = rect.yMin; y < rect.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    target.Add(cell);
                    roomByCell[cell] = roomId;
                }
            }
        }

        private static void AddSouthStairs(RectInt plateauRect, int level, string roomId, List<StairPlacement> stairs, bool opensRight)
        {
            var stairX = Mathf.Clamp(CellCenter(plateauRect).x, plateauRect.xMin + 1, plateauRect.xMax - 2);
            var upper = new Vector2Int(stairX, plateauRect.yMin);
            var lower = upper + Vector2Int.down;
            stairs.Add(new StairPlacement
            {
                upper = upper,
                lower = lower,
                opensRight = opensRight,
                level = level,
                roomId = roomId,
            });
        }

        private static RectInt Inset(RectInt rect, int padding)
        {
            return new RectInt(rect.xMin + padding, rect.yMin + padding, rect.width - padding * 2, rect.height - padding * 2);
        }

        private static RectInt ClampRect(RectInt rect, int minWidth, int minHeight)
        {
            return new RectInt(rect.xMin, rect.yMin, Mathf.Max(minWidth, rect.width), Mathf.Max(minHeight, rect.height));
        }

        private static Vector2Int CellCenter(RectInt rect)
        {
            return new Vector2Int(rect.xMin + rect.width / 2, rect.yMin + rect.height / 2);
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            return ((value % count) + count) % count;
        }

        private static Vector2Int FindWalkableNear(Vector2Int preferred, HashSet<Vector2Int> walkableCells, RectInt worldRect)
        {
            if (walkableCells.Contains(preferred))
            {
                return preferred;
            }

            for (var radius = 1; radius <= 4; radius++)
            {
                for (var x = preferred.x - radius; x <= preferred.x + radius; x++)
                {
                    for (var y = preferred.y - radius; y <= preferred.y + radius; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (!worldRect.Contains(cell))
                        {
                            continue;
                        }

                        if (walkableCells.Contains(cell))
                        {
                            return cell;
                        }
                    }
                }
            }

            return preferred;
        }

        private static RoomNodeDefinition CloneRoom(RoomNodeDefinition room, bool randomize, System.Random random)
        {
            var clone = new RoomNodeDefinition
            {
                id = room.id,
                roomType = room.roomType,
                bounds = room.bounds,
            };

            if (!randomize || room.roomType == RoomType.Start)
            {
                return clone;
            }

            clone.bounds.position += new Vector2Int(random.Next(-1, 2), random.Next(-1, 2));
            return clone;
        }

        private static Vector3 ToWorld(Vector2Int cell)
        {
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }
    }
}