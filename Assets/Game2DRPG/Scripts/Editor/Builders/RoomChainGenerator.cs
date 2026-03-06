#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Linq;
using Game2DRPG.Map.Runtime;
using UnityEngine;

namespace Game2DRPG.Map.Editor
{
    internal static class RoomChainGenerator
    {
        public static MapSaveData GenerateShowcase(ResourceCatalogAsset catalog, LevelLayoutAsset layout, int seed)
        {
            return GenerateInternal(catalog, layout, seed, randomizeRooms: false);
        }

        public static MapSaveData GenerateRandom(ResourceCatalogAsset catalog, LevelLayoutAsset layout, RoomChainProfile profile, int seed)
        {
            return GenerateInternal(catalog, layout, seed, randomizeRooms: true);
        }

        private static MapSaveData GenerateInternal(ResourceCatalogAsset catalog, LevelLayoutAsset layout, int seed, bool randomizeRooms)
        {
            var random = new System.Random(seed);
            var palette = MapPalette.Create(catalog);
            var save = new MapSaveData
            {
                schemaVersion = 1,
                mode = nameof(MapMode.RoomChain),
                seed = seed,
                layoutId = layout.id,
            };

            var rooms = layout.rooms
                .Select(room => CloneRoom(room, randomizeRooms, random))
                .ToList();

            var worldRect = MapGenerationUtility.Expand(MapGenerationUtility.Union(rooms.Select(room => room.bounds)), 6);
            MapGenerationUtility.FillRect(save.tileLayers, "BGColor", worldRect, palette.backgroundTilePath);
            MapGenerationUtility.FillRect(save.tileLayers, "AnimatedWater", worldRect, palette.waterTilePath);

            foreach (var room in rooms)
            {
                var groundTile = room.roomType == RoomType.Resource || room.roomType == RoomType.Reward
                    ? palette.alternateGroundTilePath
                    : palette.flatGroundTilePath;
                MapGenerationUtility.FillRect(save.tileLayers, "FlatGround", room.bounds, groundTile);
            }

            foreach (var edge in layout.edges)
            {
                var from = rooms.First(room => room.id == edge.fromRoomId);
                var to = rooms.First(room => room.id == edge.toRoomId);
                MapGenerationUtility.DrawCorridor(save.tileLayers, "FlatGround", MapGenerationUtility.Center(from.bounds), MapGenerationUtility.Center(to.bounds), 3, palette.flatGroundTilePath);
            }

            var elevationRoom = rooms.FirstOrDefault(room => room.id == "elevation_arena");
            if (elevationRoom != null)
            {
                var elevatedRect = new RectInt(elevationRoom.bounds.xMin + 3, elevationRoom.bounds.yMin + 3, elevationRoom.bounds.width - 6, elevationRoom.bounds.height - 5);
                var shadowRect = new RectInt(elevatedRect.xMin, elevatedRect.yMin - 1, elevatedRect.width, elevatedRect.height);
                MapGenerationUtility.FillRect(save.tileLayers, "Shadow_L1", shadowRect, palette.shadowTilePath);
                MapGenerationUtility.FillRect(save.tileLayers, "ElevatedGround_L1", elevatedRect, palette.elevationTilePath);
            }

            var startRoom = rooms.First(room => room.roomType == RoomType.Start);
            var rewardRoom = rooms.First(room => room.roomType == RoomType.Reward);
            var exitRoom = rooms.First(room => room.roomType == RoomType.Exit);
            var resourceRoom = rooms.First(room => room.roomType == RoomType.Resource);
            var bridgeRoom = rooms.First(room => room.id == "bridge_room");

            MapGenerationUtility.AddMarker(save.markers, "player_start", MarkerType.PlayerStart, ToWorld(startRoom.bounds.center), roomId: startRoom.id);
            MapGenerationUtility.AddMarker(save.markers, "reward_shrine", MarkerType.RewardSpawn, ToWorld(rewardRoom.bounds.center), roomId: rewardRoom.id);
            MapGenerationUtility.AddMarker(save.markers, "exit_gate", MarkerType.ExitSpawn, ToWorld(exitRoom.bounds.center), roomId: exitRoom.id);

            MapGenerationUtility.AddDecoration(save.decorations, "reward_monastery", palette.rewardBuildingPath, ToWorld(rewardRoom.bounds.center + new Vector2(0f, 1.6f)), true, 40, roomId: rewardRoom.id, scale: new Vector3(1.15f, 1.15f, 1f));
            MapGenerationUtility.AddDecoration(save.decorations, "resource_house", palette.resourceBuildingPath, ToWorld(resourceRoom.bounds.center + new Vector2(1.5f, 1.6f)), true, 35, roomId: resourceRoom.id);
            MapGenerationUtility.AddDecoration(save.decorations, "resource_wood", palette.woodResourcePath, ToWorld(resourceRoom.bounds.center + new Vector2(-2f, -1f)), true, 30, roomId: resourceRoom.id);
            MapGenerationUtility.AddDecoration(save.decorations, "resource_gold", palette.goldResourcePath, ToWorld(resourceRoom.bounds.center + new Vector2(2f, -1f)), true, 30, roomId: resourceRoom.id);
            MapGenerationUtility.AddDecoration(save.decorations, "exit_castle", palette.exitBuildingPath, ToWorld(exitRoom.bounds.center + new Vector2(0f, 1.6f)), true, 45, roomId: exitRoom.id, scale: new Vector3(1.25f, 1.25f, 1f));
            MapGenerationUtility.AddDecoration(save.decorations, "bridge_tower", palette.towerPath, ToWorld(bridgeRoom.bounds.center + new Vector2(2f, 1.2f)), true, 36, roomId: bridgeRoom.id);

            AddNature(save, rooms, palette);

            var combatRooms = rooms.Where(room => room.roomType == RoomType.Resource || room.roomType == RoomType.Combat || room.roomType == RoomType.Elite || room.roomType == RoomType.Exit).ToList();
            foreach (var room in combatRooms)
            {
                var encounterId = $"{room.id}_encounter";
                var encounter = MapGenerationUtility.CreateEncounter(
                    encounterId,
                    room.bounds,
                    room.id,
                    string.Empty,
                    isSummonEncounter: room.roomType == RoomType.Elite,
                    new EnemySpawnData { enemyId = "torch_goblin_project_ext", position = ToWorld(room.bounds.center + new Vector2(-1.5f, 0.75f)), count = room.roomType == RoomType.Exit ? 2 : 1 },
                    new EnemySpawnData { enemyId = "tnt_goblin_project_ext", position = ToWorld(room.bounds.center + new Vector2(1.75f, -0.5f)), count = room.roomType == RoomType.Resource ? 0 : 1 });

                encounter.enemies.RemoveAll(enemy => enemy.count <= 0);
                save.encounters.Add(encounter);
            }

            return save;
        }

        private static void AddNature(MapSaveData save, System.Collections.Generic.IEnumerable<RoomNodeDefinition> rooms, MapPalette palette)
        {
            foreach (var room in rooms)
            {
                var rect = room.bounds;
                MapGenerationUtility.AddDecoration(save.decorations, $"{room.id}_tree_a", palette.treePaths[0], ToWorld(new Vector2(rect.xMin + 1.4f, rect.yMax - 1.2f)), true, 50, roomId: room.id);
                MapGenerationUtility.AddDecoration(save.decorations, $"{room.id}_tree_b", palette.treePaths[Math.Min(1, palette.treePaths.Count - 1)], ToWorld(new Vector2(rect.xMax - 1.8f, rect.yMin + 1.5f)), true, 50, roomId: room.id);
                MapGenerationUtility.AddDecoration(save.decorations, $"{room.id}_rock", palette.rockPaths[0], ToWorld(new Vector2(rect.xMax - 1.5f, rect.yMax - 1.3f)), true, 28, roomId: room.id, scale: new Vector3(0.85f, 0.85f, 1f));

                if (palette.bushFrames.Count > 0)
                {
                    MapGenerationUtility.AddAnimated(save.animatedPlacements, $"{room.id}_bush_anim", palette.bushFrames, AnimationChannel.AnimatedVegetation, ActivationPolicy.ByRoom, ToWorld(new Vector2(rect.center.x - 2f, rect.center.y - 1.8f)), 5f, roomId: room.id);
                }

                if (palette.fireFrames.Count > 0 && (room.roomType == RoomType.Reward || room.roomType == RoomType.Exit))
                {
                    MapGenerationUtility.AddAnimated(save.animatedPlacements, $"{room.id}_fire_anim", palette.fireFrames, AnimationChannel.AmbientProps, ActivationPolicy.ByRoom, ToWorld(new Vector2(rect.center.x + 2f, rect.center.y + 1.2f)), 6f, roomId: room.id);
                }

                if (palette.dustFrames.Count > 0 && room.roomType == RoomType.Combat)
                {
                    MapGenerationUtility.AddAnimated(save.animatedPlacements, $"{room.id}_dust_fx", palette.dustFrames, AnimationChannel.ReactiveFX, ActivationPolicy.ByEncounterState, ToWorld(rect.center), 7f, roomId: room.id);
                }
            }
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

            clone.bounds.position += new Vector2Int(random.Next(-2, 3), random.Next(-2, 3));
            return clone;
        }

        private static Vector3 ToWorld(Vector2 position)
        {
            return new Vector3(position.x, position.y, 0f);
        }
    }
}
