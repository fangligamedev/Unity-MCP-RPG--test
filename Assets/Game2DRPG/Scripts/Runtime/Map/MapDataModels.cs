#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game2DRPG.Map.Runtime
{
    public enum MapMode
    {
        RoomChain = 0,
        OpenWorld = 1,
    }

    public enum ResourceFamily
    {
        Tile,
        Decoration,
        Building,
        ResourceNode,
        CombatUnit,
        Fx,
        UI,
    }

    public enum AnimationKind
    {
        Static,
        Animated,
        ReactiveAnimated,
    }

    public enum AnimationChannel
    {
        None,
        AnimatedWater,
        AnimatedShoreline,
        AnimatedVegetation,
        AmbientProps,
        ReactiveFX,
    }

    public enum ActivationPolicy
    {
        AlwaysOn,
        ByRoom,
        ByRegion,
        ByCameraProximity,
        ByEncounterState,
        ByInteractionState,
    }

    public enum RoomType
    {
        Start,
        Combat,
        Resource,
        Reward,
        Connector,
        Elite,
        Exit,
    }

    public enum RegionType
    {
        SpawnMeadow,
        WetlandBelt,
        ResourceForest,
        RuinedVillage,
        HighPlateauCitadel,
    }

    public enum MarkerType
    {
        PlayerStart,
        EnemySpawn,
        SummonSpawn,
        EliteSpawn,
        RewardSpawn,
        ExitSpawn,
        PatrolAnchor,
        EventTrigger,
        ResourceNode,
    }

    public enum TerrainSemantic
    {
        None,
        Water,
        FlatGround,
        ElevatedTopL1,
        ElevatedTopL2,
        CliffToGroundL1,
        CliffToWaterL1,
        CliffToGroundL2,
        CliffToWaterL2,
        StairsL1,
        StairsL2,
        ShadowL1,
        ShadowL2,
        BlockedDecoration,
    }

    [Serializable]
    public struct IntRange
    {
        public int Min;
        public int Max;

        public int Clamp(int value)
        {
            return Mathf.Clamp(value, Min, Max);
        }

        public int Pick(System.Random random)
        {
            return random.Next(Math.Min(Min, Max), Math.Max(Min, Max) + 1);
        }
    }

    [Serializable]
    public sealed class ResourceFamilyDefinition
    {
        public ResourceFamily family;
        public string displayName = string.Empty;
        public List<string> semanticTags = new();
        public bool enabledForGeneration = true;
    }

    [Serializable]
    public sealed class ResourceEntryDefinition
    {
        public string id = string.Empty;
        public string assetPath = string.Empty;
        public ResourceFamily family;
        public AnimationKind animationKind;
        public List<string> semanticTags = new();
        public bool enabledInRoomChain = true;
        public bool enabledInOpenWorld = true;
        public bool enabledInPcg = true;
        public string previewSpritePath = string.Empty;
    }

    [Serializable]
    public sealed class AnimatedVariantDefinition
    {
        public string id = string.Empty;
        public string sourceAssetPath = string.Empty;
        public AnimationChannel channel;
        public ActivationPolicy activationPolicy;
        public bool loop = true;
        public float estimatedDuration = 0.8f;
        public bool randomizeStartFrame = true;
        public int estimatedVisualPriority = 1;
    }

    [Serializable]
    public sealed class ExternalCombatAssetDefinition
    {
        public string id = string.Empty;
        public string assetPath = string.Empty;
        public string prefabPath = string.Empty;
        public string roleTag = string.Empty;
        public bool enabledInRoomChain = true;
        public bool enabledInOpenWorld = true;
    }

    [Serializable]
    public sealed class StaticLayerDefinition
    {
        public string id = string.Empty;
        public int sortingOrder;
        public bool hasCollider;
        public bool participatesInNavigation;
    }

    [Serializable]
    public sealed class DynamicChannelDefinition
    {
        public AnimationChannel channel;
        public ActivationPolicy activationPolicy;
        public int sortingOrder;
        public bool allowRuntimeToggle = true;
    }

    [Serializable]
    public sealed class NavigationRuleDefinition
    {
        public int spawnSafeRadius = 3;
        public int minimumCombatClearRadius = 4;
        public bool blockWater = true;
    }

    [Serializable]
    public sealed class PlacementRuleDefinition
    {
        public float maxDecorationDensity = 0.45f;
        public int minimumRewardDistanceFromSpawn = 6;
        public int minimumEncounterDistanceFromReward = 4;
    }

    [Serializable]
    public sealed class RoomNodeDefinition
    {
        public string id = string.Empty;
        public RoomType roomType;
        public RectInt bounds;
    }

    [Serializable]
    public sealed class RoomEdgeDefinition
    {
        public string fromRoomId = string.Empty;
        public string toRoomId = string.Empty;
        public bool isPrimaryPath = true;
    }

    [Serializable]
    public sealed class RegionNodeDefinition
    {
        public string id = string.Empty;
        public RegionType regionType;
        public RectInt bounds;
    }

    [Serializable]
    public sealed class RegionEdgeDefinition
    {
        public string fromRegionId = string.Empty;
        public string toRegionId = string.Empty;
    }

    [Serializable]
    public sealed class PlacedTileData
    {
        public Vector3Int position;
        public string assetPath = string.Empty;
    }

    [Serializable]
    public sealed class PlacedTileLayerData
    {
        public string layerId = string.Empty;
        public List<PlacedTileData> tiles = new();
    }

    [Serializable]
    public sealed class PlacedMarkerData
    {
        public string id = string.Empty;
        public MarkerType markerType;
        public Vector3 position;
        public string roomId = string.Empty;
        public string regionId = string.Empty;
        public string linkedEventId = string.Empty;
    }

    [Serializable]
    public sealed class PlacedDecorationData
    {
        public string id = string.Empty;
        public string assetPath = string.Empty;
        public Vector3 position;
        public Vector3 scale = Vector3.one;
        public string roomId = string.Empty;
        public string regionId = string.Empty;
        public int sortingOrder;
        public bool hasCollider;
    }

    [Serializable]
    public sealed class PlacedInteractiveData
    {
        public string id = string.Empty;
        public string assetPath = string.Empty;
        public Vector3 position;
        public MarkerType markerType;
        public string linkedEventId = string.Empty;
    }

    [Serializable]
    public sealed class AnimatedPlacementData
    {
        public string id = string.Empty;
        public string assetPath = string.Empty;
        public string animatorControllerPath = string.Empty;
        public bool useAnimatorController;
        public AnimationChannel channel;
        public ActivationPolicy activationPolicy;
        public Vector3 position;
        public Vector3 scale = Vector3.one;
        public string roomId = string.Empty;
        public string regionId = string.Empty;
        public float activationRadius = 8f;
        public float framesPerSecond = 6f;
        public List<string> frameAssetPaths = new();
    }

    [Serializable]
    public sealed class TerrainCellData
    {
        public Vector3Int position;
        public TerrainSemantic semantic;
        public bool walkable;
        public string roomId = string.Empty;
    }

    [Serializable]
    public sealed class OccupancyCellData
    {
        public Vector3Int position;
        public bool walkable;
        public TerrainSemantic semantic;
        public string sourceId = string.Empty;
    }

    [Serializable]
    public sealed class TileSemanticRuleSet
    {
        public string id = string.Empty;
        public List<string> flatTopTiles = new();
        public List<string> elevatedTopTiles = new();
        public List<string> cliffUpperTiles = new();
        public List<string> cliffLowerTiles = new();
        public List<string> stairTiles = new();
        public string waterFoamTilePath = string.Empty;
        public string shadowTilePath = string.Empty;
    }

    [Serializable]
    public sealed class RoomTerrainTemplate
    {
        public string id = string.Empty;
        public RoomType roomType;
        public bool supportsSecondElevation;
        public RectInt plateauBounds;
    }

    [Serializable]
    public sealed class OccupancyProfile
    {
        public List<OccupancyCellData> cells = new();
    }

    [Serializable]
    public sealed class EnemySpawnData
    {
        public string enemyId = string.Empty;
        public Vector3 position;
        public int count = 1;
    }

    [Serializable]
    public sealed class EncounterDefinition
    {
        public string id = string.Empty;
        public MarkerType triggerType = MarkerType.EventTrigger;
        public Rect triggerBounds;
        public List<EnemySpawnData> enemies = new();
        public bool isSummonEncounter;
        public bool isRepeatable;
        public string roomId = string.Empty;
        public string regionId = string.Empty;
    }

    [Serializable]
    public sealed class RegionEncounterDefinition
    {
        public string regionId = string.Empty;
        public List<EncounterDefinition> encounters = new();
        public RectInt encounterBounds;
    }

    [Serializable]
    public sealed class MapDefinition
    {
        public MapMode mode;
        public string sceneName = string.Empty;
        public List<PlacedTileLayerData> tileLayers = new();
        public List<PlacedMarkerData> markers = new();
        public List<PlacedDecorationData> decorations = new();
        public List<PlacedInteractiveData> interactives = new();
        public List<AnimatedPlacementData> animatedPlacements = new();
        public List<EncounterDefinition> encounters = new();
        public List<RegionEncounterDefinition> regionEncounters = new();
        public List<TerrainCellData> terrainCells = new();
        public List<OccupancyCellData> occupancyCells = new();
    }

    [Serializable]
    public sealed class AnimationActivationContext
    {
        public MapMode mapMode;
        public string currentRoomId = string.Empty;
        public string currentRegionId = string.Empty;
        public Vector3 playerPosition;
        public Vector3 cameraPosition;
        public bool inEncounter;
    }

    [Serializable]
    public sealed class WorldEventMarker
    {
        public string id = string.Empty;
        public MarkerType markerType;
        public Vector3 position;
        public string linkedEventId = string.Empty;
    }

    [Serializable]
    public sealed class MapSaveData
    {
        public int schemaVersion = 1;
        public string mode = nameof(MapMode.RoomChain);
        public int seed;
        public string layoutId = string.Empty;
        public List<PlacedTileLayerData> tileLayers = new();
        public List<PlacedMarkerData> markers = new();
        public List<PlacedDecorationData> decorations = new();
        public List<PlacedInteractiveData> interactives = new();
        public List<EncounterDefinition> encounters = new();
        public List<AnimatedPlacementData> animatedPlacements = new();
        public List<TerrainCellData> terrainCells = new();
        public List<OccupancyCellData> occupancyCells = new();
    }

    [Serializable]
    public sealed class OpenWorldSaveData
    {
        public int schemaVersion = 1;
        public string mode = nameof(MapMode.OpenWorld);
        public int seed;
        public string layoutId = string.Empty;
        public List<RegionNodeDefinition> regions = new();
        public List<PlacedTileLayerData> tileLayers = new();
        public List<PlacedMarkerData> markers = new();
        public List<PlacedDecorationData> decorations = new();
        public List<PlacedInteractiveData> interactives = new();
        public List<RegionEncounterDefinition> regionEncounters = new();
        public List<AnimatedPlacementData> animatedPlacements = new();
        public List<TerrainCellData> terrainCells = new();
        public List<OccupancyCellData> occupancyCells = new();
    }

    [CreateAssetMenu(fileName = "ResourceCatalog", menuName = "Game2DRPG/Map/Resource Catalog")]
    public sealed partial class ResourceCatalogAsset : ScriptableObject
    {
        public int schemaVersion = 1;
        public string sourceRoot = "Assets/Tiny Swords";
        public List<ResourceFamilyDefinition> families = new();
        public List<ResourceEntryDefinition> entries = new();
        public List<AnimatedVariantDefinition> animatedVariants = new();
        public List<ExternalCombatAssetDefinition> externalCombatAssets = new();
    }

    [CreateAssetMenu(fileName = "TileLayerRules", menuName = "Game2DRPG/Map/Tile Layer Rules")]
    public sealed partial class TileLayerRuleAsset : ScriptableObject
    {
        public int schemaVersion = 1;
        public float cellSize = 1f;
        public List<StaticLayerDefinition> staticLayers = new();
        public List<DynamicChannelDefinition> dynamicChannels = new();
        public NavigationRuleDefinition navigationRules = new();
        public PlacementRuleDefinition placementRules = new();
    }

    [CreateAssetMenu(fileName = "AmbientAnimationProfile", menuName = "Game2DRPG/Map/Ambient Animation Profile")]
    public sealed partial class AmbientAnimationProfileAsset : ScriptableObject
    {
        public int maxAlwaysOnAnimations = 40;
        public int maxVisibleAmbientAnimations = 24;
        public int maxReactiveFxPerBurst = 10;
        public float cameraActivationRadius = 12f;
        public bool randomizeLoopOffsets = true;
    }

    [CreateAssetMenu(fileName = "RoomTemplate", menuName = "Game2DRPG/Map/Room Template")]
    public sealed partial class RoomTemplateAsset : ScriptableObject
    {
        public string id = string.Empty;
        public RoomType roomType;
        public Vector2Int minSize = new(8, 8);
        public Vector2Int maxSize = new(16, 12);
        public bool requiresWater;
        public bool requiresElevation;
        public IntRange encounterCount;
        public IntRange summonCount;
        public List<string> allowedBuildingIds = new();
        public List<string> allowedDecorationTags = new();
        public List<string> requiredAnimationChannels = new();
    }

    [CreateAssetMenu(fileName = "RegionTemplate", menuName = "Game2DRPG/Map/Region Template")]
    public sealed partial class RegionTemplateAsset : ScriptableObject
    {
        public string id = string.Empty;
        public RegionType regionType;
        public Vector2Int minBounds = new(24, 24);
        public Vector2Int maxBounds = new(48, 48);
        public float waterCoverage = 0.25f;
        public float elevationCoverage = 0.2f;
        public float vegetationDensity = 0.3f;
        public int encounterZoneCount = 2;
        public List<string> requiredLandmarkIds = new();
        public List<string> preferredAnimationChannels = new();
    }

    [CreateAssetMenu(fileName = "LevelLayout", menuName = "Game2DRPG/Map/Level Layout")]
    public sealed partial class LevelLayoutAsset : ScriptableObject
    {
        public string id = string.Empty;
        public MapMode mode = MapMode.RoomChain;
        public List<RoomNodeDefinition> rooms = new();
        public List<RoomEdgeDefinition> edges = new();
    }

    [CreateAssetMenu(fileName = "OverworldLayout", menuName = "Game2DRPG/Map/Overworld Layout")]
    public sealed partial class OverworldLayoutAsset : ScriptableObject
    {
        public string id = string.Empty;
        public MapMode mode = MapMode.OpenWorld;
        public List<RegionNodeDefinition> regions = new();
        public List<RegionEdgeDefinition> edges = new();
    }

    [Serializable]
    public sealed class RoomChainProfile
    {
        public IntRange roomCount = new() { Min = 7, Max = 9 };
        public IntRange branchDepth = new() { Min = 1, Max = 3 };
        public float waterRoomChance = 0.45f;
        public float elevationRoomChance = 0.35f;
        public float animationDensity = 0.45f;
    }

    [Serializable]
    public sealed class OpenWorldProfile
    {
        public Vector2Int worldSize = new(96, 40);
        public float waterCoverage = 0.28f;
        public float elevationCoverage = 0.22f;
        public float vegetationDensity = 0.35f;
        public int encounterZoneCount = 6;
        public float animationDensity = 0.55f;
    }

    [CreateAssetMenu(fileName = "DefaultPCGProfile", menuName = "Game2DRPG/Map/PCG Profile")]
    public sealed partial class PCGProfileAsset : ScriptableObject
    {
        public RoomChainProfile roomChainProfile = new();
        public OpenWorldProfile openWorldProfile = new();
    }
}
