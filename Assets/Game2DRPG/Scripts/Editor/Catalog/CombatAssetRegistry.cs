#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections.Generic;
using Game2DRPG.Map.Runtime;

namespace Game2DRPG.Map.Editor
{
    internal static class CombatAssetRegistry
    {
        public static List<ExternalCombatAssetDefinition> CreateDefaultRegistry()
        {
            return new List<ExternalCombatAssetDefinition>
            {
                new()
                {
                    id = "torch_goblin_project_ext",
                    assetPath = MapAssetPaths.TinySwordsRoot + "/Units/Enemy Pack - Promo",
                    prefabPath = MapAssetPaths.TorchGoblinPrefab,
                    roleTag = "melee-enemy",
                    enabledInRoomChain = true,
                    enabledInOpenWorld = true,
                },
                new()
                {
                    id = "tnt_goblin_project_ext",
                    assetPath = MapAssetPaths.TinySwordsRoot + "/Units/Enemy Pack - Promo",
                    prefabPath = MapAssetPaths.TntGoblinPrefab,
                    roleTag = "ranged-enemy",
                    enabledInRoomChain = true,
                    enabledInOpenWorld = true,
                },
            };
        }
    }
}
