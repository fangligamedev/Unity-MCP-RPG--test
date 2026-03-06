#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game2DRPG.Map.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game2DRPG.Map.Editor
{
    internal static class TinySwordsCatalogScanner
    {
        public static ResourceCatalogAsset Scan(ResourceCatalogAsset catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            catalog.schemaVersion = 1;
            catalog.sourceRoot = MapAssetPaths.TinySwordsRoot;
            catalog.families = CreateFamilyDefinitions();
            catalog.entries.Clear();
            catalog.animatedVariants.Clear();
            catalog.externalCombatAssets = CombatAssetRegistry.CreateDefaultRegistry();

            var guids = AssetDatabase.FindAssets(string.Empty, new[] { MapAssetPaths.TinySwordsRoot });
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!ShouldIncludeAsset(assetPath, out var family))
                {
                    continue;
                }

                var entry = new ResourceEntryDefinition
                {
                    id = BuildId(assetPath),
                    assetPath = assetPath,
                    family = family,
                    animationKind = ClassifyAnimationKind(assetPath, family),
                    semanticTags = ExtractSemanticTags(assetPath),
                    enabledInRoomChain = !assetPath.Contains("/UI Elements/", StringComparison.OrdinalIgnoreCase),
                    enabledInOpenWorld = !assetPath.Contains("/UI Elements/", StringComparison.OrdinalIgnoreCase),
                    enabledInPcg = !assetPath.Contains("/Scenes/", StringComparison.OrdinalIgnoreCase),
                    previewSpritePath = assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? assetPath : string.Empty,
                };

                catalog.entries.Add(entry);

                if (TryCreateAnimatedVariant(entry, out var variant))
                {
                    catalog.animatedVariants.Add(variant);
                }
            }

            catalog.entries = catalog.entries.OrderBy(item => item.family).ThenBy(item => item.assetPath).ToList();
            catalog.animatedVariants = catalog.animatedVariants.OrderBy(item => item.channel).ThenBy(item => item.id).ToList();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static bool ShouldIncludeAsset(string assetPath, out ResourceFamily family)
        {
            family = default;
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            var extension = Path.GetExtension(assetPath);
            if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".asset", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (assetPath.Contains("/Scenes/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (assetPath.Contains("/Buildings/", StringComparison.OrdinalIgnoreCase))
            {
                family = ResourceFamily.Building;
                return true;
            }

            if (assetPath.Contains("/Particle FX/", StringComparison.OrdinalIgnoreCase))
            {
                family = ResourceFamily.Fx;
                return true;
            }

            if (assetPath.Contains("/Pawn and Resources/", StringComparison.OrdinalIgnoreCase))
            {
                family = ResourceFamily.ResourceNode;
                return true;
            }

            if (assetPath.Contains("/Terrain/Tileset/", StringComparison.OrdinalIgnoreCase))
            {
                family = ResourceFamily.Tile;
                return true;
            }

            if (assetPath.Contains("/Terrain/Decorations/", StringComparison.OrdinalIgnoreCase))
            {
                family = ResourceFamily.Decoration;
                return true;
            }

            if (assetPath.Contains("/Units/", StringComparison.OrdinalIgnoreCase))
            {
                family = ResourceFamily.CombatUnit;
                return true;
            }

            if (assetPath.Contains("/UI Elements/", StringComparison.OrdinalIgnoreCase))
            {
                family = ResourceFamily.UI;
                return true;
            }

            return false;
        }

        private static AnimationKind ClassifyAnimationKind(string assetPath, ResourceFamily family)
        {
            if (assetPath.Contains("Water Tile animated.asset", StringComparison.OrdinalIgnoreCase) ||
                assetPath.Contains("/Bushes/", StringComparison.OrdinalIgnoreCase) ||
                assetPath.Contains("/Rocks in the Water/", StringComparison.OrdinalIgnoreCase) ||
                assetPath.Contains("/Rubber Duck/", StringComparison.OrdinalIgnoreCase) ||
                assetPath.Contains("/Sheep/", StringComparison.OrdinalIgnoreCase))
            {
                return AnimationKind.Animated;
            }

            if (family == ResourceFamily.Fx)
            {
                return AnimationKind.ReactiveAnimated;
            }

            return AnimationKind.Static;
        }

        private static bool TryCreateAnimatedVariant(ResourceEntryDefinition entry, out AnimatedVariantDefinition variant)
        {
            variant = new AnimatedVariantDefinition();
            if (entry.animationKind == AnimationKind.Static)
            {
                return false;
            }

            variant.id = entry.id + "_variant";
            variant.sourceAssetPath = entry.assetPath;
            variant.loop = entry.animationKind != AnimationKind.ReactiveAnimated;
            variant.randomizeStartFrame = true;
            variant.estimatedDuration = 0.75f;
            variant.estimatedVisualPriority = 1;

            if (entry.assetPath.Contains("Water Tile animated.asset", StringComparison.OrdinalIgnoreCase))
            {
                variant.channel = AnimationChannel.AnimatedWater;
                variant.activationPolicy = ActivationPolicy.AlwaysOn;
                return true;
            }

            if (entry.assetPath.Contains("/Bushes/", StringComparison.OrdinalIgnoreCase))
            {
                variant.channel = AnimationChannel.AnimatedVegetation;
                variant.activationPolicy = ActivationPolicy.ByCameraProximity;
                return true;
            }

            if (entry.assetPath.Contains("/Rocks in the Water/", StringComparison.OrdinalIgnoreCase) ||
                entry.assetPath.Contains("/Rubber Duck/", StringComparison.OrdinalIgnoreCase))
            {
                variant.channel = AnimationChannel.AmbientProps;
                variant.activationPolicy = ActivationPolicy.ByCameraProximity;
                return true;
            }

            if (entry.assetPath.Contains("/Fire", StringComparison.OrdinalIgnoreCase))
            {
                variant.channel = AnimationChannel.AmbientProps;
                variant.activationPolicy = ActivationPolicy.ByRegion;
                variant.loop = true;
                return true;
            }

            if (entry.assetPath.Contains("/Explosion", StringComparison.OrdinalIgnoreCase) ||
                entry.assetPath.Contains("/Dust", StringComparison.OrdinalIgnoreCase) ||
                entry.assetPath.Contains("/Water Splash", StringComparison.OrdinalIgnoreCase))
            {
                variant.channel = AnimationChannel.ReactiveFX;
                variant.activationPolicy = ActivationPolicy.ByEncounterState;
                variant.loop = false;
                variant.randomizeStartFrame = false;
                variant.estimatedVisualPriority = 3;
                return true;
            }

            if (entry.assetPath.Contains("/Sheep/", StringComparison.OrdinalIgnoreCase))
            {
                variant.channel = AnimationChannel.AmbientProps;
                variant.activationPolicy = ActivationPolicy.ByRegion;
                return true;
            }

            return false;
        }

        private static List<ResourceFamilyDefinition> CreateFamilyDefinitions()
        {
            return new List<ResourceFamilyDefinition>
            {
                CreateFamily(ResourceFamily.Tile, "Terrain/Tileset", "tile", "ground", "water", "shadow", "elevation"),
                CreateFamily(ResourceFamily.Decoration, "Terrain/Decorations", "decoration", "vegetation", "ambient"),
                CreateFamily(ResourceFamily.Building, "Buildings", "building", "landmark", "structure"),
                CreateFamily(ResourceFamily.ResourceNode, "Pawn and Resources", "resource", "node", "interactive"),
                CreateFamily(ResourceFamily.CombatUnit, "Units", "unit", "combat"),
                CreateFamily(ResourceFamily.Fx, "Particle FX", "fx", "reactive", "ambient"),
                CreateFamily(ResourceFamily.UI, "UI Elements", "ui"),
            };
        }

        private static ResourceFamilyDefinition CreateFamily(ResourceFamily family, string displayName, params string[] tags)
        {
            return new ResourceFamilyDefinition
            {
                family = family,
                displayName = displayName,
                semanticTags = tags.ToList(),
                enabledForGeneration = family != ResourceFamily.UI,
            };
        }

        private static List<string> ExtractSemanticTags(string assetPath)
        {
            return assetPath
                .Replace(MapAssetPaths.TinySwordsRoot, string.Empty, StringComparison.OrdinalIgnoreCase)
                .Split(new[] { '/', '\\', '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(tag => tag.Trim().ToLowerInvariant())
                .Where(tag => tag.Length > 1)
                .Distinct()
                .ToList();
        }

        private static string BuildId(string assetPath)
        {
            return assetPath
                .Replace(MapAssetPaths.TinySwordsRoot + "/", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("/", "_", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "_", StringComparison.OrdinalIgnoreCase)
                .Replace("-", "_", StringComparison.OrdinalIgnoreCase)
                .Replace(".", "_", StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
        }
    }
}
