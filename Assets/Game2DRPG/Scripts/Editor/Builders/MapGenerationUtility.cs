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
    internal static class MapGenerationUtility
    {
        public static RectInt Union(IEnumerable<RectInt> rects)
        {
            var list = rects.ToList();
            if (list.Count == 0)
            {
                return new RectInt(0, 0, 1, 1);
            }

            var xMin = list.Min(rect => rect.xMin);
            var yMin = list.Min(rect => rect.yMin);
            var xMax = list.Max(rect => rect.xMax);
            var yMax = list.Max(rect => rect.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        public static RectInt Expand(RectInt rect, int padding)
        {
            return new RectInt(rect.xMin - padding, rect.yMin - padding, rect.width + padding * 2, rect.height + padding * 2);
        }

        public static Vector2Int Center(RectInt rect)
        {
            return new Vector2Int(rect.xMin + rect.width / 2, rect.yMin + rect.height / 2);
        }

        public static void FillRect(List<PlacedTileLayerData> layers, string layerId, RectInt rect, string assetPath)
        {
            var layer = GetLayer(layers, layerId);
            for (var x = rect.xMin; x < rect.xMax; x++)
            {
                for (var y = rect.yMin; y < rect.yMax; y++)
                {
                    layer.tiles.Add(new PlacedTileData
                    {
                        position = new Vector3Int(x, y, 0),
                        assetPath = assetPath,
                    });
                }
            }
        }

        public static void DrawCorridor(List<PlacedTileLayerData> layers, string layerId, Vector2Int from, Vector2Int to, int width, string assetPath)
        {
            var start = from;
            var mid = new Vector2Int(to.x, from.y);
            FillCorridorSegment(layers, layerId, start, mid, width, assetPath);
            FillCorridorSegment(layers, layerId, mid, to, width, assetPath);
        }

        public static void AddMarker(
            List<PlacedMarkerData> markers,
            string id,
            MarkerType type,
            Vector3 position,
            string roomId = "",
            string regionId = "",
            string linkedEventId = "")
        {
            markers.Add(new PlacedMarkerData
            {
                id = id,
                markerType = type,
                position = position,
                roomId = roomId,
                regionId = regionId,
                linkedEventId = linkedEventId,
            });
        }

        public static void AddDecoration(
            List<PlacedDecorationData> decorations,
            string id,
            string assetPath,
            Vector3 position,
            bool hasCollider,
            int sortingOrder,
            string roomId = "",
            string regionId = "",
            Vector3? scale = null)
        {
            decorations.Add(new PlacedDecorationData
            {
                id = id,
                assetPath = assetPath,
                position = position,
                hasCollider = hasCollider,
                sortingOrder = sortingOrder,
                roomId = roomId,
                regionId = regionId,
                scale = scale ?? Vector3.one,
            });
        }

        public static void AddInteractive(
            List<PlacedInteractiveData> interactives,
            string id,
            string assetPath,
            Vector3 position,
            MarkerType markerType,
            string linkedEventId = "")
        {
            interactives.Add(new PlacedInteractiveData
            {
                id = id,
                assetPath = assetPath,
                position = position,
                markerType = markerType,
                linkedEventId = linkedEventId,
            });
        }

        public static void AddAnimated(
            List<AnimatedPlacementData> placements,
            string id,
            IEnumerable<string> frames,
            AnimationChannel channel,
            ActivationPolicy policy,
            Vector3 position,
            float fps,
            string roomId = "",
            string regionId = "",
            float activationRadius = 8f,
            Vector3? scale = null)
        {
            placements.Add(new AnimatedPlacementData
            {
                id = id,
                assetPath = frames.FirstOrDefault() ?? string.Empty,
                frameAssetPaths = frames.ToList(),
                channel = channel,
                activationPolicy = policy,
                position = position,
                roomId = roomId,
                regionId = regionId,
                activationRadius = activationRadius,
                framesPerSecond = fps,
                scale = scale ?? Vector3.one,
            });
        }

        public static EncounterDefinition CreateEncounter(
            string id,
            RectInt triggerBounds,
            string roomId,
            string regionId,
            bool isSummonEncounter,
            params EnemySpawnData[] enemies)
        {
            return new EncounterDefinition
            {
                id = id,
                triggerType = MarkerType.EventTrigger,
                triggerBounds = new Rect(triggerBounds.xMin + triggerBounds.width * 0.5f, triggerBounds.yMin + triggerBounds.height * 0.5f, triggerBounds.width, triggerBounds.height),
                roomId = roomId,
                regionId = regionId,
                isSummonEncounter = isSummonEncounter,
                isRepeatable = false,
                enemies = enemies.ToList(),
            };
        }

        private static PlacedTileLayerData GetLayer(List<PlacedTileLayerData> layers, string layerId)
        {
            var layer = layers.FirstOrDefault(item => item.layerId == layerId);
            if (layer != null)
            {
                return layer;
            }

            layer = new PlacedTileLayerData { layerId = layerId };
            layers.Add(layer);
            return layer;
        }

        private static void FillCorridorSegment(List<PlacedTileLayerData> layers, string layerId, Vector2Int from, Vector2Int to, int width, string assetPath)
        {
            if (from.x == to.x)
            {
                var minY = Mathf.Min(from.y, to.y);
                var maxY = Mathf.Max(from.y, to.y);
                FillRect(layers, layerId, new RectInt(from.x - width / 2, minY - width / 2, width, maxY - minY + width), assetPath);
                return;
            }

            if (from.y == to.y)
            {
                var minX = Mathf.Min(from.x, to.x);
                var maxX = Mathf.Max(from.x, to.x);
                FillRect(layers, layerId, new RectInt(minX - width / 2, from.y - width / 2, maxX - minX + width, width), assetPath);
            }
        }
    }
}
