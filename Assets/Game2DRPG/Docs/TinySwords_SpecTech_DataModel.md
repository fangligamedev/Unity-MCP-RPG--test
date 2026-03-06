# Tiny Swords Spec Tech: Data Model

## 1. 文档目标
本文件定义双模式地图系统的数据模型，包括：
- ScriptableObject 权威模型
- JSON 导出与保存模型
- 枚举、路径规则、资产归档规则
- SO -> JSON -> 场景重建的数据流

本文件中的字段设计在实现阶段视为强约束，除非回改 Spec Tech，不允许随意变形。

## 2. 数据源原则
- 设计期真相源：`ScriptableObject`
- 导出和运行时复现：`JSON`
- 编辑器内部所有按钮优先读取 SO
- 保存当前生成结果时统一输出 JSON

## 3. 目录与命名规则
### 3.1 ScriptableObject 存放路径
- `Assets/Game2DRPG/Data/Catalog/ResourceCatalog.asset`
- `Assets/Game2DRPG/Data/Rules/TileLayerRules.asset`
- `Assets/Game2DRPG/Data/Rules/AmbientAnimationProfile.asset`
- `Assets/Game2DRPG/Data/Templates/RoomChain/*.asset`
- `Assets/Game2DRPG/Data/Templates/OpenWorld/*.asset`
- `Assets/Game2DRPG/Data/Layouts/*.asset`
- `Assets/Game2DRPG/Data/Profiles/PCGProfile.asset`

### 3.2 JSON 存放路径
- `Assets/Game2DRPG/Data/Saves/resource-catalog.json`
- `Assets/Game2DRPG/Data/Saves/roomchain-save.json`
- `Assets/Game2DRPG/Data/Saves/openworld-save.json`

### 3.3 资源路径标准
- 目录扫描结果中统一使用 Unity 相对路径，如 `Assets/Tiny Swords/...`
- JSON 中保留 `assetPath` 字段，不存绝对路径
- 所有导出 JSON 都带 `schemaVersion`

## 4. 枚举定义
### 4.1 `MapMode`
```csharp
public enum MapMode
{
    RoomChain = 0,
    OpenWorld = 1
}
```

### 4.2 `ResourceFamily`
```csharp
public enum ResourceFamily
{
    Tile,
    Decoration,
    Building,
    ResourceNode,
    CombatUnit,
    Fx,
    UI
}
```

### 4.3 `AnimationKind`
```csharp
public enum AnimationKind
{
    Static,
    Animated,
    ReactiveAnimated
}
```

### 4.4 `AnimationChannel`
```csharp
public enum AnimationChannel
{
    None,
    AnimatedWater,
    AnimatedShoreline,
    AnimatedVegetation,
    AmbientProps,
    ReactiveFX
}
```

### 4.5 `ActivationPolicy`
```csharp
public enum ActivationPolicy
{
    AlwaysOn,
    ByRoom,
    ByRegion,
    ByCameraProximity,
    ByEncounterState,
    ByInteractionState
}
```

### 4.6 `RoomType`
```csharp
public enum RoomType
{
    Start,
    Combat,
    Resource,
    Reward,
    Connector,
    Elite,
    Exit
}
```

### 4.7 `RegionType`
```csharp
public enum RegionType
{
    SpawnMeadow,
    WetlandBelt,
    ResourceForest,
    RuinedVillage,
    HighPlateauCitadel
}
```

### 4.8 `MarkerType`
```csharp
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
    ResourceNode
}
```

## 5. ScriptableObject 模型
### 5.1 `ResourceCatalogAsset`
职责：
- 记录资源扫描结果
- 作为所有地图构建器的资源入口

建议字段：
```csharp
public sealed class ResourceCatalogAsset : ScriptableObject
{
    public int schemaVersion;
    public string sourceRoot;
    public List<ResourceFamilyDefinition> families;
    public List<ResourceEntryDefinition> entries;
    public List<AnimatedVariantDefinition> animatedVariants;
    public List<ExternalCombatAssetDefinition> externalCombatAssets;
}
```

### 5.2 `ResourceFamilyDefinition`
```csharp
public sealed class ResourceFamilyDefinition
{
    public ResourceFamily family;
    public string displayName;
    public List<string> semanticTags;
    public bool enabledForGeneration;
}
```

### 5.3 `ResourceEntryDefinition`
```csharp
public sealed class ResourceEntryDefinition
{
    public string id;
    public string assetPath;
    public ResourceFamily family;
    public AnimationKind animationKind;
    public List<string> semanticTags;
    public bool enabledInRoomChain;
    public bool enabledInOpenWorld;
    public bool enabledInPcg;
    public string previewSpritePath;
}
```

### 5.4 `AnimatedVariantDefinition`
```csharp
public sealed class AnimatedVariantDefinition
{
    public string id;
    public string sourceAssetPath;
    public AnimationChannel channel;
    public ActivationPolicy activationPolicy;
    public bool loop;
    public float estimatedDuration;
    public bool randomizeStartFrame;
    public int estimatedVisualPriority;
}
```

### 5.5 `TileLayerRuleAsset`
```csharp
public sealed class TileLayerRuleAsset : ScriptableObject
{
    public int schemaVersion;
    public float cellSize;
    public List<StaticLayerDefinition> staticLayers;
    public List<DynamicChannelDefinition> dynamicChannels;
    public NavigationRuleDefinition navigationRules;
    public PlacementRuleDefinition placementRules;
}
```

### 5.6 `AmbientAnimationProfileAsset`
```csharp
public sealed class AmbientAnimationProfileAsset : ScriptableObject
{
    public int maxAlwaysOnAnimations;
    public int maxVisibleAmbientAnimations;
    public int maxReactiveFxPerBurst;
    public float cameraActivationRadius;
    public bool randomizeLoopOffsets;
}
```

### 5.7 `RoomTemplateAsset`
```csharp
public sealed class RoomTemplateAsset : ScriptableObject
{
    public string id;
    public RoomType roomType;
    public Vector2Int minSize;
    public Vector2Int maxSize;
    public bool requiresWater;
    public bool requiresElevation;
    public IntRange encounterCount;
    public IntRange summonCount;
    public List<string> allowedBuildingIds;
    public List<string> allowedDecorationTags;
    public List<string> requiredAnimationChannels;
}
```

### 5.8 `RegionTemplateAsset`
```csharp
public sealed class RegionTemplateAsset : ScriptableObject
{
    public string id;
    public RegionType regionType;
    public Vector2Int minBounds;
    public Vector2Int maxBounds;
    public float waterCoverage;
    public float elevationCoverage;
    public float vegetationDensity;
    public int encounterZoneCount;
    public List<string> requiredLandmarkIds;
    public List<string> preferredAnimationChannels;
}
```

### 5.9 `LevelLayoutAsset`
```csharp
public sealed class LevelLayoutAsset : ScriptableObject
{
    public string id;
    public MapMode mode;
    public List<RoomNodeDefinition> rooms;
    public List<RoomEdgeDefinition> edges;
}
```

### 5.10 `OverworldLayoutAsset`
```csharp
public sealed class OverworldLayoutAsset : ScriptableObject
{
    public string id;
    public MapMode mode;
    public List<RegionNodeDefinition> regions;
    public List<RegionEdgeDefinition> edges;
}
```

### 5.11 `PCGProfileAsset`
```csharp
public sealed class PCGProfileAsset : ScriptableObject
{
    public RoomChainProfile roomChainProfile;
    public OpenWorldProfile openWorldProfile;
}
```

### 5.12 `RoomChainProfile`
```csharp
public sealed class RoomChainProfile
{
    public IntRange roomCount;
    public IntRange branchDepth;
    public float waterRoomChance;
    public float elevationRoomChance;
    public float animationDensity;
}
```

### 5.13 `OpenWorldProfile`
```csharp
public sealed class OpenWorldProfile
{
    public Vector2Int worldSize;
    public float waterCoverage;
    public float elevationCoverage;
    public float vegetationDensity;
    public int encounterZoneCount;
    public float animationDensity;
}
```

## 6. 运行时模型
### 6.1 `MapDefinition`
共享运行时地图定义：
```csharp
public sealed class MapDefinition
{
    public MapMode mode;
    public string sceneName;
    public List<PlacedTileLayerData> tileLayers;
    public List<PlacedMarkerData> markers;
    public List<PlacedDecorationData> decorations;
    public List<PlacedInteractiveData> interactives;
}
```

### 6.2 `EncounterDefinition`
```csharp
public sealed class EncounterDefinition
{
    public string id;
    public MarkerType triggerType;
    public List<EnemySpawnData> enemies;
    public bool isSummonEncounter;
    public bool isRepeatable;
}
```

### 6.3 `RegionEncounterDefinition`
```csharp
public sealed class RegionEncounterDefinition
{
    public string regionId;
    public List<EncounterDefinition> encounters;
    public RectInt encounterBounds;
}
```

### 6.4 `AnimationActivationContext`
```csharp
public sealed class AnimationActivationContext
{
    public MapMode mapMode;
    public string currentRoomId;
    public string currentRegionId;
    public Vector3 playerPosition;
    public Vector3 cameraPosition;
    public bool inEncounter;
}
```

### 6.5 `WorldEventMarker`
```csharp
public sealed class WorldEventMarker
{
    public string id;
    public MarkerType markerType;
    public Vector3 position;
    public string linkedEventId;
}
```

## 7. JSON 导出模型
### 7.1 `resource-catalog.json`
用途：
- 供调试、外部检查、版本比较、导入回放

顶层结构：
```json
{
  "schemaVersion": 1,
  "sourceRoot": "Assets/Tiny Swords",
  "families": [],
  "entries": [],
  "animatedVariants": [],
  "externalCombatAssets": []
}
```

### 7.2 `roomchain-save.json`
顶层结构：
```json
{
  "schemaVersion": 1,
  "mode": "RoomChain",
  "seed": 123456,
  "layoutId": "roomchain_showcase_v1",
  "tileLayers": [],
  "markers": [],
  "decorations": [],
  "interactives": [],
  "encounters": []
}
```

### 7.3 `openworld-save.json`
顶层结构：
```json
{
  "schemaVersion": 1,
  "mode": "OpenWorld",
  "seed": 123456,
  "layoutId": "openworld_showcase_v1",
  "regions": [],
  "tileLayers": [],
  "markers": [],
  "decorations": [],
  "interactives": [],
  "regionEncounters": [],
  "animatedPlacements": []
}
```

## 8. SO -> JSON 导出链路
固定流程：
1. EditorWindow 读取 SO
2. 验证 SO 完整性
3. 构造 DTO
4. 写出 JSON
5. 刷新 AssetDatabase
6. 回显摘要到窗口

要求：
- SO 不允许直接引用绝对路径
- JSON 不允许直接存 Unity InstanceID
- 运行时重建必须只依赖 `assetPath`、`id`、`enum`、数值和布尔字段

## 9. JSON -> 场景重建链路
固定流程：
1. 读取 JSON
2. 校验 `schemaVersion`
3. 反序列化为 `MapSaveData` 或 `OpenWorldSaveData`
4. 通过 `MapSceneAssembler` 清空并重建场景
5. 交给 `MapRuntimeBinder` 绑定现有运行时

## 10. 扩展战斗资产注册
`Torch` / `TNT` 不进入 `Assets/Tiny Swords` 原始扫描结果，但必须进入技术模型。

新增：
```csharp
public sealed class ExternalCombatAssetDefinition
{
    public string id;
    public string assetPath;
    public string prefabPath;
    public string roleTag;
    public bool enabledInRoomChain;
    public bool enabledInOpenWorld;
}
```

默认注册：
- `torch_goblin_project_ext`
- `tnt_goblin_project_ext`

## 11. 数据兼容规则
- 所有 JSON 顶层必须有 `schemaVersion`
- 第一版固定 `schemaVersion = 1`
- 若未来字段新增，只允许追加，不允许改名或删除现有关键字段
- `MapMode` 在 JSON 中使用字符串，不使用数字

## 12. 实现阶段最小资产清单
实现开始时，至少要生成这些权威资产：
- `ResourceCatalog.asset`
- `TileLayerRules.asset`
- `AmbientAnimationProfile.asset`
- `RoomChain_Showcase.asset`
- `OpenWorld_Showcase.asset`
- `DefaultPCGProfile.asset`

没有这 6 个资产，不允许进入场景生成实现。
