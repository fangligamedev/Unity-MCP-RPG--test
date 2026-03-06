#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game2DRPG.Map.Runtime;
using Game2DRPG.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Game2DRPG.Map.Editor
{
    internal static class MapSceneAssembler
    {
        private sealed class MapSceneRoots
        {
            public Transform sceneRoot = null!;
            public Transform mapRoot = null!;
            public Transform gridRoot = null!;
            public Transform decorationRoot = null!;
            public Transform interactiveRoot = null!;
            public Transform markerRoot = null!;
            public Transform ambientFxRoot = null!;
            public Transform gameplayRoot = null!;
            public Transform playerRoot = null!;
            public Transform encounterRoot = null!;
            public Transform rewardRoot = null!;
            public Transform cameraRoot = null!;
            public Transform uiRoot = null!;
            public ArenaGameState arenaGameState = null!;
            public HudPresenter hudPresenter = null!;
            public Camera camera = null!;
            public CameraFollow2D cameraFollow = null!;
            public MapRuntimeBinder runtimeBinder = null!;
            public AnimationActivationService activationService = null!;
            public RegionEncounterController encounterController = null!;
            public Bootstrap bootstrap = null!;
            public Dictionary<string, Tilemap> tilemaps = new();
        }

        public static void BuildRoomChainScene(MapSaveData data, TileLayerRuleAsset rules)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = Path.GetFileNameWithoutExtension(MapAssetPaths.RoomChainScene);
            var roots = CreateSceneRoots(rules);
            ApplySceneData(roots, data.tileLayers, data.markers, data.decorations, data.interactives, data.animatedPlacements);
            ConfigureRuntime(roots, MapMode.RoomChain, data, null);
            EditorSceneManager.SaveScene(scene, MapAssetPaths.RoomChainScene);
        }

        public static void BuildOpenWorldScene(OpenWorldSaveData data, TileLayerRuleAsset rules)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = Path.GetFileNameWithoutExtension(MapAssetPaths.OpenWorldScene);
            var roots = CreateSceneRoots(rules);
            ApplySceneData(roots, data.tileLayers, data.markers, data.decorations, data.interactives, data.animatedPlacements);
            ConfigureRuntime(roots, MapMode.OpenWorld, null, data);
            EditorSceneManager.SaveScene(scene, MapAssetPaths.OpenWorldScene);
        }

        private static MapSceneRoots CreateSceneRoots(TileLayerRuleAsset rules)
        {
            var roots = new MapSceneRoots();
            roots.sceneRoot = new GameObject("SceneRoot").transform;

            roots.mapRoot = CreateChild(roots.sceneRoot, "MapRoot");
            roots.gridRoot = CreateGridRoot(roots.mapRoot, rules, roots.tilemaps);
            roots.decorationRoot = CreateChild(roots.mapRoot, "DecorationRoot");
            roots.interactiveRoot = CreateChild(roots.mapRoot, "InteractiveRoot");
            roots.markerRoot = CreateChild(roots.mapRoot, "MarkerRoot");
            roots.ambientFxRoot = CreateChild(roots.mapRoot, "AmbientFxRoot");

            roots.gameplayRoot = CreateChild(roots.sceneRoot, "GameplayRoot");
            roots.playerRoot = CreateChild(roots.gameplayRoot, "PlayerRoot");
            roots.encounterRoot = CreateChild(roots.gameplayRoot, "EncounterRoot");
            roots.rewardRoot = CreateChild(roots.gameplayRoot, "RewardRoot");
            var runtimeBinderRoot = CreateChild(roots.gameplayRoot, "RuntimeBinder");
            roots.runtimeBinder = runtimeBinderRoot.gameObject.AddComponent<MapRuntimeBinder>();
            roots.activationService = runtimeBinderRoot.gameObject.AddComponent<AnimationActivationService>();
            roots.encounterController = runtimeBinderRoot.gameObject.AddComponent<RegionEncounterController>();
            roots.bootstrap = runtimeBinderRoot.gameObject.AddComponent<Bootstrap>();

            var arenaStateObject = new GameObject("ArenaGameState");
            arenaStateObject.transform.SetParent(roots.gameplayRoot, false);
            roots.arenaGameState = arenaStateObject.AddComponent<ArenaGameState>();

            roots.cameraRoot = CreateChild(roots.sceneRoot, "CameraRoot");
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(roots.cameraRoot, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            roots.camera = cameraObject.AddComponent<Camera>();
            roots.camera.orthographic = true;
            roots.camera.orthographicSize = 7.5f;
            roots.camera.clearFlags = CameraClearFlags.SolidColor;
            roots.camera.backgroundColor = new Color(0.08f, 0.19f, 0.24f, 1f);
            cameraObject.AddComponent<AudioListener>();
            roots.cameraFollow = cameraObject.AddComponent<CameraFollow2D>();

            roots.uiRoot = CreateChild(roots.sceneRoot, "UIRoot");
            roots.hudPresenter = CreateUi(roots.uiRoot);
            return roots;
        }

        private static void ApplySceneData(
            MapSceneRoots roots,
            IEnumerable<PlacedTileLayerData> tileLayers,
            IEnumerable<PlacedMarkerData> markers,
            IEnumerable<PlacedDecorationData> decorations,
            IEnumerable<PlacedInteractiveData> interactives,
            IEnumerable<AnimatedPlacementData> animatedPlacements)
        {
            foreach (var layer in tileLayers)
            {
                if (!roots.tilemaps.TryGetValue(layer.layerId, out var tilemap))
                {
                    continue;
                }

                foreach (var tile in layer.tiles)
                {
                    var tileAsset = AssetDatabase.LoadAssetAtPath<TileBase>(tile.assetPath);
                    if (tileAsset != null)
                    {
                        tilemap.SetTile(tile.position, tileAsset);
                    }
                }
            }

            foreach (var marker in markers)
            {
                var markerObject = new GameObject(marker.id);
                markerObject.transform.SetParent(roots.markerRoot, false);
                markerObject.transform.position = marker.position;
            }

            foreach (var decoration in decorations)
            {
                CreateSpriteObject(
                    roots.decorationRoot,
                    decoration.id,
                    decoration.assetPath,
                    decoration.position,
                    decoration.sortingOrder,
                    decoration.hasCollider,
                    decoration.scale);
            }

            foreach (var interactive in interactives)
            {
                CreateSpriteObject(
                    roots.interactiveRoot,
                    interactive.id,
                    interactive.assetPath,
                    interactive.position,
                    38,
                    hasCollider: true,
                    Vector3.one);
            }

            foreach (var animated in animatedPlacements)
            {
                var parent = animated.channel == AnimationChannel.ReactiveFX ? roots.ambientFxRoot : roots.decorationRoot;
                CreateAnimatedObject(parent, animated);
            }

            CreateWorldBounds(roots.interactiveRoot, tileLayers);
        }

        private static void ConfigureRuntime(MapSceneRoots roots, MapMode mapMode, MapSaveData? roomChainData, OpenWorldSaveData? openWorldData)
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(MapAssetPaths.InputActions);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapAssetPaths.PlayerPrefab);
            var rewardShrinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapAssetPaths.RewardShrinePrefab);
            var torchGoblinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapAssetPaths.TorchGoblinPrefab);
            var tntGoblinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MapAssetPaths.TntGoblinPrefab);

            roots.runtimeBinder.AssignSceneReferences(
                roots.playerRoot,
                roots.encounterRoot,
                roots.rewardRoot,
                roots.arenaGameState,
                roots.hudPresenter,
                roots.camera,
                roots.cameraFollow,
                roots.activationService,
                roots.encounterController,
                roots.bootstrap);

            if (mapMode == MapMode.OpenWorld && openWorldData != null && inputActions != null && playerPrefab != null && rewardShrinePrefab != null && torchGoblinPrefab != null && tntGoblinPrefab != null)
            {
                roots.runtimeBinder.ConfigureOpenWorld(openWorldData, inputActions, playerPrefab, rewardShrinePrefab, torchGoblinPrefab, tntGoblinPrefab);
            }
            else if (roomChainData != null && inputActions != null && playerPrefab != null && rewardShrinePrefab != null && torchGoblinPrefab != null && tntGoblinPrefab != null)
            {
                roots.runtimeBinder.ConfigureRoomChain(roomChainData, inputActions, playerPrefab, rewardShrinePrefab, torchGoblinPrefab, tntGoblinPrefab);
            }

            EditorUtility.SetDirty(roots.runtimeBinder);
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Transform CreateGridRoot(Transform mapRoot, TileLayerRuleAsset rules, Dictionary<string, Tilemap> output)
        {
            var gridRoot = new GameObject("GridRoot");
            gridRoot.transform.SetParent(mapRoot, false);
            gridRoot.AddComponent<Grid>();

            foreach (var staticLayer in rules.staticLayers)
            {
                output[staticLayer.id] = CreateTilemapLayer(gridRoot.transform, staticLayer.id, staticLayer.sortingOrder);
            }

            foreach (var dynamicChannel in rules.dynamicChannels)
            {
                output[dynamicChannel.channel.ToString()] = CreateTilemapLayer(gridRoot.transform, dynamicChannel.channel.ToString(), dynamicChannel.sortingOrder);
            }

            return gridRoot.transform;
        }

        private static Tilemap CreateTilemapLayer(Transform parent, string name, int sortingOrder)
        {
            var layerObject = new GameObject(name);
            layerObject.transform.SetParent(parent, false);
            var tilemap = layerObject.AddComponent<Tilemap>();
            var renderer = layerObject.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return tilemap;
        }

        private static HudPresenter CreateUi(Transform uiRoot)
        {
            var canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(uiRoot, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var hudRoot = new GameObject("HUD", typeof(RectTransform));
            var hudRect = (RectTransform)hudRoot.transform;
            hudRect.SetParent(canvasObject.transform, false);
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            var topPanel = CreatePanel(hudRect, "TopPanel", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 72f), new Vector2(0f, -8f));
            var healthText = CreateText(topPanel, "HealthText", new Vector2(0.12f, 0.5f), new Vector2(0.12f, 0.5f), new Vector2(180f, 28f), TextAnchor.MiddleLeft);
            var waveText = CreateText(topPanel, "WaveText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 28f), TextAnchor.MiddleCenter);
            var enemyText = CreateText(topPanel, "EnemyText", new Vector2(0.88f, 0.5f), new Vector2(0.88f, 0.5f), new Vector2(180f, 28f), TextAnchor.MiddleRight);
            var promptText = CreateText(hudRect, "PromptText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(640f, 28f), TextAnchor.MiddleCenter, anchoredPosition: new Vector2(0f, -88f));

            var statePanel = CreatePanel(hudRect, "StatePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 120f), Vector2.zero);
            statePanel.gameObject.SetActive(false);
            var stateText = CreateText(statePanel, "StateText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, 90f), TextAnchor.MiddleCenter);

            var rewardPanel = CreatePanel(hudRect, "RewardPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420f, 120f), new Vector2(0f, 84f));
            rewardPanel.gameObject.SetActive(false);
            var rewardTitle = CreateText(rewardPanel, "RewardTitle", new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(340f, 30f), TextAnchor.MiddleCenter);
            var rewardOption1 = CreateText(rewardPanel, "RewardOption1", new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(360f, 26f), TextAnchor.MiddleCenter);
            var rewardOption2 = CreateText(rewardPanel, "RewardOption2", new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), new Vector2(360f, 26f), TextAnchor.MiddleCenter);

            var hud = hudRoot.AddComponent<HudPresenter>();
            hud.Configure(healthText, waveText, enemyText, promptText, statePanel.gameObject, stateText, rewardPanel.gameObject, rewardTitle, rewardOption1, rewardOption2);
            hud.SetPrompt("地图加载完成");

            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(uiRoot, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            return hud;
        }

        private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var panel = new GameObject(name, typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            var image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.35f);
            return rect;
        }

        private static Text CreateText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, TextAnchor alignment, Vector2? anchoredPosition = null)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition ?? Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = name;
            return text;
        }

        private static void CreateSpriteObject(Transform parent, string name, string assetPath, Vector3 position, int sortingOrder, bool hasCollider, Vector3 scale)
        {
            var sprite = LoadSprite(assetPath);
            if (sprite == null)
            {
                return;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            if (hasCollider)
            {
                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = sprite.bounds.size;
            }
        }

        private static void CreateAnimatedObject(Transform parent, AnimatedPlacementData animated)
        {
            var frames = animated.frameAssetPaths
                .Select(LoadSprite)
                .Where(sprite => sprite != null)
                .Cast<Sprite>()
                .ToArray();

            if (frames.Length == 0)
            {
                return;
            }

            var go = new GameObject(animated.id);
            go.transform.SetParent(parent, false);
            go.transform.position = animated.position;
            go.transform.localScale = animated.scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = animated.channel == AnimationChannel.ReactiveFX ? 55 : 32;

            var player = go.AddComponent<AnimatedSpritePlayer>();
            player.Configure(frames, animated.framesPerSecond, animated.channel != AnimationChannel.ReactiveFX, true);

            var activationTarget = go.AddComponent<AnimationActivationTarget>();
            activationTarget.Configure(animated.channel, animated.activationPolicy, animated.roomId, animated.regionId, animated.activationRadius);
            if (animated.activationPolicy != ActivationPolicy.AlwaysOn)
            {
                activationTarget.SetRuntimeActive(false);
            }
        }

        private static void CreateWorldBounds(Transform parent, IEnumerable<PlacedTileLayerData> tileLayers)
        {
            var relevantTiles = tileLayers
                .Where(layer => layer.layerId == "FlatGround" || layer.layerId == "ElevatedGround_L1" || layer.layerId == "ElevatedGround_L2")
                .SelectMany(layer => layer.tiles)
                .ToList();

            if (relevantTiles.Count == 0)
            {
                return;
            }

            var minX = relevantTiles.Min(tile => tile.position.x) - 1;
            var maxX = relevantTiles.Max(tile => tile.position.x) + 1;
            var minY = relevantTiles.Min(tile => tile.position.y) - 1;
            var maxY = relevantTiles.Max(tile => tile.position.y) + 1;

            CreateBoundary(parent, "Boundary_Left", new Vector2(minX - 0.5f, (minY + maxY) * 0.5f), new Vector2(1f, maxY - minY + 4f));
            CreateBoundary(parent, "Boundary_Right", new Vector2(maxX + 0.5f, (minY + maxY) * 0.5f), new Vector2(1f, maxY - minY + 4f));
            CreateBoundary(parent, "Boundary_Top", new Vector2((minX + maxX) * 0.5f, maxY + 0.5f), new Vector2(maxX - minX + 4f, 1f));
            CreateBoundary(parent, "Boundary_Bottom", new Vector2((minX + maxX) * 0.5f, minY - 0.5f), new Vector2(maxX - minX + 4f, 1f));
        }

        private static void CreateBoundary(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static Sprite? LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            return AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().FirstOrDefault();
        }
    }
}
