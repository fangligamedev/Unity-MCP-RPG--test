#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Game2DRPG.Map.Runtime;

namespace Game2DRPG.Map.Editor
{
    internal sealed class MapPalette
    {
        public string backgroundTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Water Background color.asset";
        public string waterTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Water Tile animated.asset";
        public string shadowTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Shadow.asset";
        public string flatGroundTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color1_21.asset";
        public string alternateGroundTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color2_21.asset";
        public string elevationTilePath = "Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color5_21.asset";
        public string rewardBuildingPath = "Assets/Tiny Swords/Buildings/Blue Buildings/Monastery.png";
        public string resourceBuildingPath = "Assets/Tiny Swords/Buildings/Blue Buildings/House1.png";
        public string exitBuildingPath = "Assets/Tiny Swords/Buildings/Blue Buildings/Castle.png";
        public string towerPath = "Assets/Tiny Swords/Buildings/Blue Buildings/Tower.png";
        public string goldResourcePath = "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Resource/Gold_Resource.png";
        public string woodResourcePath = "Assets/Tiny Swords/Pawn and Resources/Wood/Wood Resource/Wood Resource.png";
        public List<string> treePaths = new();
        public List<string> rockPaths = new();
        public List<string> bushFrames = new();
        public List<string> waterRockFrames = new();
        public List<string> fireFrames = new();
        public List<string> dustFrames = new();
        public List<string> explosionFrames = new();

        public static MapPalette Create(ResourceCatalogAsset catalog)
        {
            var palette = new MapPalette();
            palette.flatGroundTilePath = PickTile(catalog, "Tilemap_color1_", palette.flatGroundTilePath);
            palette.alternateGroundTilePath = PickTile(catalog, "Tilemap_color2_", palette.alternateGroundTilePath);
            palette.elevationTilePath = PickTile(catalog, "Tilemap_color5_", palette.elevationTilePath);
            palette.backgroundTilePath = PickFirst(catalog, "Water Background color.asset", palette.backgroundTilePath);
            palette.waterTilePath = PickFirst(catalog, "Water Tile animated.asset", palette.waterTilePath);
            palette.shadowTilePath = PickFirst(catalog, "Shadow.asset", palette.shadowTilePath);
            palette.rewardBuildingPath = PickFirst(catalog, "/Monastery.png", palette.rewardBuildingPath);
            palette.resourceBuildingPath = PickFirst(catalog, "/House1.png", palette.resourceBuildingPath);
            palette.exitBuildingPath = PickFirst(catalog, "/Castle.png", palette.exitBuildingPath);
            palette.towerPath = PickFirst(catalog, "/Tower.png", palette.towerPath);
            palette.goldResourcePath = PickFirst(catalog, "Gold_Resource.png", palette.goldResourcePath);
            palette.woodResourcePath = PickFirst(catalog, "Wood Resource.png", palette.woodResourcePath);
            palette.treePaths = PickMany(catalog, "/Trees/Tree", ".png", 4);
            palette.rockPaths = PickMany(catalog, "/Decorations/Rocks/Rock", ".png", 4);
            palette.bushFrames = PickMany(catalog, "/Decorations/Bushes/Bush ", ".png", 4);
            palette.waterRockFrames = PickMany(catalog, "/Rocks in the Water/Water Rocks_", ".png", 4);
            palette.fireFrames = PickMany(catalog, "/Particle FX/Fire_", ".png", 3);
            palette.dustFrames = PickMany(catalog, "/Particle FX/Dust_", ".png", 2);
            palette.explosionFrames = PickMany(catalog, "/Particle FX/Explosion_", ".png", 2);
            return palette;
        }

        private static string PickTile(ResourceCatalogAsset catalog, string prefix, string fallback)
        {
            var matches = catalog.entries
                .Where(entry => entry.assetPath.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.assetPath)
                .Distinct()
                .OrderBy(path => ExtractTrailingNumber(path))
                .ToList();

            if (matches.Count == 0)
            {
                return fallback;
            }

            return matches[Math.Min(matches.Count / 2, matches.Count - 1)];
        }

        private static List<string> PickMany(ResourceCatalogAsset catalog, string contains, string suffix, int maxCount)
        {
            return catalog.entries
                .Where(entry => entry.assetPath.Contains(contains, StringComparison.OrdinalIgnoreCase) &&
                                entry.assetPath.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.assetPath)
                .Distinct()
                .OrderBy(path => path)
                .Take(maxCount)
                .ToList();
        }

        private static string PickFirst(ResourceCatalogAsset catalog, string contains, string fallback)
        {
            return catalog.entries.FirstOrDefault(entry => entry.assetPath.Contains(contains, StringComparison.OrdinalIgnoreCase))?.assetPath ?? fallback;
        }

        private static int ExtractTrailingNumber(string path)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            var digits = new string(fileName.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
            return int.TryParse(digits, out var number) ? number : 0;
        }
    }
}
