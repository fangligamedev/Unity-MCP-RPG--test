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
    internal static class MapJsonExporter
    {
        public static void ExportCatalog(ResourceCatalogAsset catalog)
        {
            WriteJson(MapAssetPaths.ResourceCatalogJson, catalog);
        }

        public static void ExportRoomChain(MapSaveData data)
        {
            WriteJson(MapAssetPaths.RoomChainJson, data);
        }

        public static void ExportOpenWorld(OpenWorldSaveData data)
        {
            WriteJson(MapAssetPaths.OpenWorldJson, data);
        }

        private static void WriteJson(string assetPath, object data)
        {
            var absolutePath = ToAbsolutePath(assetPath);
            File.WriteAllText(absolutePath, JsonUtility.ToJson(data, true));
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

    internal static class MapJsonImporter
    {
        public static MapSaveData? ImportRoomChain()
        {
            return ReadJson<MapSaveData>(MapAssetPaths.RoomChainJson);
        }

        public static OpenWorldSaveData? ImportOpenWorld()
        {
            return ReadJson<OpenWorldSaveData>(MapAssetPaths.OpenWorldJson);
        }

        public static ResourceCatalogAsset? ImportCatalogAsAsset(ResourceCatalogAsset targetAsset)
        {
            var dto = ReadJson<ResourceCatalogAsset>(MapAssetPaths.ResourceCatalogJson);
            if (dto == null)
            {
                return null;
            }

            targetAsset.schemaVersion = dto.schemaVersion;
            targetAsset.sourceRoot = dto.sourceRoot;
            targetAsset.families = dto.families;
            targetAsset.entries = dto.entries;
            targetAsset.animatedVariants = dto.animatedVariants;
            targetAsset.externalCombatAssets = dto.externalCombatAssets;
            EditorUtility.SetDirty(targetAsset);
            AssetDatabase.SaveAssets();
            return targetAsset;
        }

        private static T? ReadJson<T>(string assetPath) where T : class
        {
            var absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                return null;
            }

            return JsonUtility.FromJson<T>(File.ReadAllText(absolutePath));
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
