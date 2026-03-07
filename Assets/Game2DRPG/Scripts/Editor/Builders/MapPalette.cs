#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Game2DRPG.Map.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game2DRPG.Map.Editor
{
    internal enum SemanticTileShape
    {
        TopLeftCorner = 0,
        TopEdge = 1,
        TopRightCorner = 2,
        LeftEdge = 3,
        Center = 4,
        RightEdge = 5,
        BottomLeftCorner = 6,
        BottomEdge = 7,
        BottomRightCorner = 8,
        LeftBridge = 9,
        HorizontalBridge = 10,
        RightBridge = 11,
        TopBridge = 12,
        VerticalBridge = 13,
        BottomBridge = 14,
        Isolated = 15,
    }

    internal sealed class AnimatedEnvironmentDefinition
    {
        public string spriteAssetPath = string.Empty;
        public string animatorControllerPath = string.Empty;
        public AnimationChannel channel;
        public ActivationPolicy activationPolicy;
        public int sortingOrder = 36;
    }

    internal sealed class MapPalette
    {
        private const string WaterFoamSpriteSheetPath = "Assets/Tiny Swords/Terrain/Tileset/Water Foam.png";
        private const string WaterFoamTileAssetPath = "Assets/Game2DRPG/Data/Rules/Water Foam Tile.asset";

        private static readonly int[] FlatGuideOrder = { 0, 1, 2, 8, 9, 10, 16, 17, 18, 24, 25, 26, 3, 11, 19, 27 };
        private static readonly int[] ElevatedGuideOrder = { 4, 5, 6, 12, 13, 14, 20, 21, 22, 28, 29, 30, 7, 15, 23, 31 };
        private static readonly int[] CliffUpperGuideOrder = { 34, 35, 36, 37 };
        private static readonly int[] CliffLowerGuideOrder = { 40, 41, 42, 43 };
        private static readonly int[] StairGuideOrder = { 32, 33, 38, 39 };

        public string backgroundTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Water Background color.asset";
        public string waterTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Water Tile animated.asset";
        public string waterFoamTilePath = WaterFoamTileAssetPath;
        public string shadowTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Shadow.asset";
        public string flatGroundTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color1_9.asset";
        public string alternateGroundTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color1_1.asset";
        public string elevationTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color1_13.asset";
        public string rewardBuildingPath = "Assets/Tiny Swords/Buildings/Blue Buildings/Monastery.png";
        public string resourceBuildingPath = "Assets/Tiny Swords/Buildings/Blue Buildings/House1.png";
        public string exitBuildingPath = "Assets/Tiny Swords/Buildings/Blue Buildings/Castle.png";
        public string towerPath = "Assets/Tiny Swords/Buildings/Blue Buildings/Tower.png";
        public string goldResourcePath = "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Resource/Gold_Resource.png";
        public string woodResourcePath = "Assets/Tiny Swords/Pawn and Resources/Wood/Wood Resource/Wood Resource.png";
        public string blockerRockPath = "Assets/Tiny Swords/Terrain/Decorations/Rocks/Rock 1.png";

        public List<string> flatTiles = new();
        public List<string> elevatedTiles = new();
        public List<string> cliffUpperTiles = new();
        public List<string> cliffLowerTiles = new();
        public List<string> stairTiles = new();
        public List<string> treePaths = new();
        public List<string> rockPaths = new();
        public List<string> bushFrames = new();
        public List<string> waterRockFrames = new();
        public List<string> fireFrames = new();
        public List<string> dustFrames = new();
        public List<AnimatedEnvironmentDefinition> treeAnimations = new();
        public List<AnimatedEnvironmentDefinition> bushAnimations = new();
        public List<AnimatedEnvironmentDefinition> waterRockAnimations = new();
        public List<AnimatedEnvironmentDefinition> fireAnimations = new();
        public List<AnimatedEnvironmentDefinition> dustAnimations = new();

        public static MapPalette Create(ResourceCatalogAsset _)
        {
            var palette = new MapPalette();
            palette.flatTiles = BuildIndexedTiles(FlatGuideOrder);
            palette.elevatedTiles = BuildIndexedTiles(ElevatedGuideOrder);
            palette.cliffUpperTiles = BuildIndexedTiles(CliffUpperGuideOrder);
            palette.cliffLowerTiles = BuildIndexedTiles(CliffLowerGuideOrder);
            palette.stairTiles = BuildIndexedTiles(StairGuideOrder);
            palette.flatGroundTilePath = palette.GetFlatTile(SemanticTileShape.Center);
            palette.alternateGroundTilePath = palette.GetFlatTile(SemanticTileShape.TopEdge);
            palette.elevationTilePath = palette.GetElevatedTile(SemanticTileShape.Center);
            palette.waterFoamTilePath = EnsureStaticTileAsset(WaterFoamTileAssetPath, WaterFoamSpriteSheetPath);
            palette.treePaths = new List<string>
            {
                "Assets/Tiny Swords/Pawn and Resources/Wood/Trees/Tree1.png",
                "Assets/Tiny Swords/Pawn and Resources/Wood/Trees/Tree2.png",
                "Assets/Tiny Swords/Pawn and Resources/Wood/Trees/Tree3.png",
                "Assets/Tiny Swords/Pawn and Resources/Wood/Trees/Tree4.png",
            };
            palette.rockPaths = new List<string>
            {
                "Assets/Tiny Swords/Terrain/Decorations/Rocks/Rock 1.png",
                "Assets/Tiny Swords/Terrain/Decorations/Rocks/Rock 2.png",
                "Assets/Tiny Swords/Terrain/Decorations/Rocks/Rock 3.png",
                "Assets/Tiny Swords/Terrain/Decorations/Rocks/Rock 4.png",
            };
            palette.bushFrames = new List<string>
            {
                "Assets/Tiny Swords/Terrain/Decorations/Bushes/Bush 1.png",
                "Assets/Tiny Swords/Terrain/Decorations/Bushes/Bush 2.png",
                "Assets/Tiny Swords/Terrain/Decorations/Bushes/Bush 3.png",
                "Assets/Tiny Swords/Terrain/Decorations/Bushes/Bush 4.png",
            };
            palette.waterRockFrames = new List<string>
            {
                "Assets/Tiny Swords/Terrain/Decorations/Rocks in the Water/Water Rocks_01.png",
                "Assets/Tiny Swords/Terrain/Decorations/Rocks in the Water/Water Rocks_02.png",
                "Assets/Tiny Swords/Terrain/Decorations/Rocks in the Water/Water Rocks_03.png",
                "Assets/Tiny Swords/Terrain/Decorations/Rocks in the Water/Water Rocks_04.png",
            };
            palette.fireFrames = new List<string>
            {
                "Assets/Tiny Swords/Particle FX/Fire_01.png",
                "Assets/Tiny Swords/Particle FX/Fire_02.png",
                "Assets/Tiny Swords/Particle FX/Fire_03.png",
            };
            palette.dustFrames = new List<string>
            {
                "Assets/Tiny Swords/Particle FX/Dust 1.png",
                "Assets/Tiny Swords/Particle FX/Dust 2.png",
            };
            palette.treeAnimations = BuildAnimatedDefinitions(
                palette.treePaths[0],
                new[]
                {
                    "Assets/Tiny Swords/Pawn and Resources/Wood/Trees/Tree 1 Animation/Tree 1.controller",
                    "Assets/Tiny Swords/Pawn and Resources/Wood/Trees/Tree 2 Animation/Tree 2.controller",
                    "Assets/Tiny Swords/Pawn and Resources/Wood/Trees/Tree 3 Animation/Tree 3.controller",
                    "Assets/Tiny Swords/Pawn and Resources/Wood/Trees/Tree 4 Animation/Tree 4.controller",
                },
                palette.treePaths,
                AnimationChannel.AnimatedVegetation,
                ActivationPolicy.ByCameraProximity,
                44);
            palette.bushAnimations = BuildAnimatedDefinitions(
                palette.bushFrames[0],
                new[]
                {
                    "Assets/Tiny Swords/Terrain/Decorations/Bushes/Bush 1 Animation/Bush 1.controller",
                    "Assets/Tiny Swords/Terrain/Decorations/Bushes/Bush 2 Animation/Bush 2.controller",
                    "Assets/Tiny Swords/Terrain/Decorations/Bushes/Bush 3 Animation/Bush 3.controller",
                    "Assets/Tiny Swords/Terrain/Decorations/Bushes/Bush 4 Animation/Bush 4.controller",
                },
                palette.bushFrames,
                AnimationChannel.AnimatedVegetation,
                ActivationPolicy.ByCameraProximity,
                40);
            palette.waterRockAnimations = BuildAnimatedDefinitions(
                palette.waterRockFrames[0],
                new[]
                {
                    "Assets/Tiny Swords/Terrain/Decorations/Rocks in the Water/Rock 1 Animation/Rock 1.controller",
                    "Assets/Tiny Swords/Terrain/Decorations/Rocks in the Water/Rock 2 Animation/Rock 2.controller",
                    "Assets/Tiny Swords/Terrain/Decorations/Rocks in the Water/Rock 3 Animation/Rock 3.controller",
                    "Assets/Tiny Swords/Terrain/Decorations/Rocks in the Water/Rock 4 Animation/Rock 4.controller",
                },
                palette.waterRockFrames,
                AnimationChannel.AmbientProps,
                ActivationPolicy.ByCameraProximity,
                18);
            palette.fireAnimations = BuildAnimatedDefinitions(
                palette.fireFrames[0],
                new[]
                {
                    "Assets/Tiny Swords/Particle FX/Fire 1 Animation/Fire 1.controller",
                    "Assets/Tiny Swords/Particle FX/Fire 2 Animation/Fire 2.controller",
                    "Assets/Tiny Swords/Particle FX/FIre 3 Animation/Fire 3.controller",
                },
                palette.fireFrames,
                AnimationChannel.AmbientProps,
                ActivationPolicy.ByCameraProximity,
                48);
            palette.dustAnimations = BuildAnimatedDefinitions(
                palette.dustFrames[0],
                new[]
                {
                    "Assets/Tiny Swords/Particle FX/Dust 1 Animation/Dust 1.controller",
                    "Assets/Tiny Swords/Particle FX/Dust 2 Animation/Dust 2.controller",
                },
                palette.dustFrames,
                AnimationChannel.ReactiveFX,
                ActivationPolicy.ByEncounterState,
                56);
            return palette;
        }

        public string GetFlatTile(SemanticTileShape shape)
        {
            return flatTiles[(int)shape];
        }

        public string GetElevatedTile(SemanticTileShape shape)
        {
            return elevatedTiles[(int)shape];
        }

        public string GetCliffUpperTile(int slot)
        {
            return cliffUpperTiles[Math.Clamp(slot, 0, cliffUpperTiles.Count - 1)];
        }

        public string GetCliffLowerTile(int slot)
        {
            return cliffLowerTiles[Math.Clamp(slot, 0, cliffLowerTiles.Count - 1)];
        }

        public string GetStairUpperTile(bool slopeOpensRight)
        {
            return stairTiles[slopeOpensRight ? 1 : 0];
        }

        public string GetStairLowerTile(bool slopeOpensRight)
        {
            return stairTiles[slopeOpensRight ? 3 : 2];
        }

        private static List<string> BuildIndexedTiles(IEnumerable<int> indexes)
        {
            var result = new List<string>();
            foreach (var index in indexes)
            {
                result.Add($"Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color1_{index}.asset");
            }

            return result;
        }

        private static List<AnimatedEnvironmentDefinition> BuildAnimatedDefinitions(
            string fallbackSpritePath,
            IReadOnlyList<string> controllers,
            IReadOnlyList<string> sprites,
            AnimationChannel channel,
            ActivationPolicy activationPolicy,
            int sortingOrder)
        {
            var list = new List<AnimatedEnvironmentDefinition>();
            for (var index = 0; index < controllers.Count; index++)
            {
                list.Add(new AnimatedEnvironmentDefinition
                {
                    spriteAssetPath = index < sprites.Count ? sprites[index] : fallbackSpritePath,
                    animatorControllerPath = controllers[index],
                    channel = channel,
                    activationPolicy = activationPolicy,
                    sortingOrder = sortingOrder,
                });
            }

            return list;
        }

        private static string EnsureStaticTileAsset(string assetPath, string spriteSheetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (existing != null)
            {
                return assetPath;
            }

            var sprite = LoadFirstSprite(spriteSheetPath);
            if (sprite == null)
            {
                return assetPath;
            }

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            tile.color = Color.white;
            AssetDatabase.CreateAsset(tile, assetPath);
            AssetDatabase.SaveAssets();
            return assetPath;
        }

        private static Sprite? LoadFirstSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }
}