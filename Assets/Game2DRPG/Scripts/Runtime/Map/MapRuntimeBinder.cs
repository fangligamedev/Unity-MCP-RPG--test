#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections.Generic;
using System.Linq;
using Game2DRPG.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game2DRPG.Map.Runtime
{
    public sealed class MapRuntimeBinder : MonoBehaviour
    {
        [SerializeField] private MapMode mapMode;
        [SerializeField] private MapSaveData? roomChainData;
        [SerializeField] private OpenWorldSaveData? openWorldData;
        [SerializeField] private InputActionAsset? inputActionAsset;
        [SerializeField] private GameObject? playerPrefab;
        [SerializeField] private GameObject? rewardShrinePrefab;
        [SerializeField] private GameObject? torchGoblinPrefab;
        [SerializeField] private GameObject? tntGoblinPrefab;
        [SerializeField] private Transform? gameplayRoot;
        [SerializeField] private Transform? encounterRoot;
        [SerializeField] private Transform? rewardRoot;
        [SerializeField] private ArenaGameState? arenaGameState;
        [SerializeField] private HudPresenter? hudPresenter;
        [SerializeField] private Camera? activeCamera;
        [SerializeField] private CameraFollow2D? cameraFollow;
        [SerializeField] private AnimationActivationService? animationActivationService;
        [SerializeField] private RegionEncounterController? encounterController;
        [SerializeField] private Bootstrap? bootstrap;

        private bool _initialized;

        public MapMode Mode => mapMode;
        public MapSaveData? RoomChainData => roomChainData;
        public OpenWorldSaveData? OpenWorldData => openWorldData;

        private void Start()
        {
            Initialize();
        }

        public void ConfigureRoomChain(
            MapSaveData data,
            InputActionAsset actions,
            GameObject player,
            GameObject rewardShrine,
            GameObject torchGoblin,
            GameObject tntGoblin)
        {
            mapMode = MapMode.RoomChain;
            roomChainData = data;
            openWorldData = null;
            inputActionAsset = actions;
            playerPrefab = player;
            rewardShrinePrefab = rewardShrine;
            torchGoblinPrefab = torchGoblin;
            tntGoblinPrefab = tntGoblin;
        }

        public void ConfigureOpenWorld(
            OpenWorldSaveData data,
            InputActionAsset actions,
            GameObject player,
            GameObject rewardShrine,
            GameObject torchGoblin,
            GameObject tntGoblin)
        {
            mapMode = MapMode.OpenWorld;
            openWorldData = data;
            roomChainData = null;
            inputActionAsset = actions;
            playerPrefab = player;
            rewardShrinePrefab = rewardShrine;
            torchGoblinPrefab = torchGoblin;
            tntGoblinPrefab = tntGoblin;
        }

        public void AssignSceneReferences(
            Transform gameplay,
            Transform encounters,
            Transform rewards,
            ArenaGameState gameState,
            HudPresenter hud,
            Camera sceneCamera,
            CameraFollow2D followCamera,
            AnimationActivationService activationService,
            RegionEncounterController controller,
            Bootstrap? gameplayBootstrap)
        {
            gameplayRoot = gameplay;
            encounterRoot = encounters;
            rewardRoot = rewards;
            arenaGameState = gameState;
            hudPresenter = hud;
            activeCamera = sceneCamera;
            cameraFollow = followCamera;
            animationActivationService = activationService;
            encounterController = controller;
            bootstrap = gameplayBootstrap;
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (gameplayRoot == null)
            {
                gameplayRoot = transform;
            }

            if (encounterRoot == null)
            {
                encounterRoot = gameplayRoot;
            }

            if (rewardRoot == null)
            {
                rewardRoot = gameplayRoot;
            }

            if (activeCamera == null)
            {
                activeCamera = Camera.main;
            }

            if (encounterController == null)
            {
                encounterController = FindAnyObjectByType<RegionEncounterController>();
            }

            if (animationActivationService == null)
            {
                animationActivationService = FindAnyObjectByType<AnimationActivationService>();
            }

            var saveData = mapMode == MapMode.OpenWorld
                ? ConvertOpenWorldToMapDefinition(openWorldData)
                : roomChainData;

            if (saveData == null || playerPrefab == null || inputActionAsset == null)
            {
                return;
            }

            var playerMarker = saveData.markers.FirstOrDefault(marker => marker.markerType == MarkerType.PlayerStart);
            var playerInstance = Instantiate(playerPrefab, playerMarker?.position ?? Vector3.zero, Quaternion.identity, gameplayRoot);
            playerInstance.name = "Player";

            var playerController = playerInstance.GetComponent<TopDownPlayerController>();
            var playerCombat = playerInstance.GetComponent<PlayerCombat>();
            var playerHealth = playerInstance.GetComponent<Health>();

            if (playerController != null)
            {
                playerController.SetDefaultInputActions(inputActionAsset);
            }

            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(playerInstance.transform);
            }

            if (arenaGameState != null && hudPresenter != null && playerCombat != null && playerHealth != null)
            {
                arenaGameState.SetReferences(hudPresenter, playerCombat, playerHealth);
                playerHealth.Changed += _ => arenaGameState.NotifyHealthChanged();
                playerHealth.Died += _ => arenaGameState.SetDefeat();
                arenaGameState.SetPlaying();
            }

            RewardShrine? rewardShrine = null;
            var rewardMarker = saveData.markers.FirstOrDefault(marker => marker.markerType == MarkerType.RewardSpawn);
            if (rewardShrinePrefab != null && rewardMarker != null)
            {
                var rewardShrineObject = Instantiate(rewardShrinePrefab, rewardMarker.position, Quaternion.identity, rewardRoot);
                rewardShrineObject.name = "RewardShrine";
                rewardShrine = rewardShrineObject.GetComponent<RewardShrine>();
            }

            if (bootstrap != null && playerController != null && playerCombat != null && playerHealth != null && rewardShrine != null)
            {
                bootstrap.Configure(
                    inputActionAsset,
                    playerController,
                    playerCombat,
                    playerHealth,
                    null,
                    rewardShrine,
                    arenaGameState,
                    hudPresenter,
                    cameraFollow);
                bootstrap.InitializeBindings();
            }

            var prefabMap = new Dictionary<string, GameObject>();
            if (torchGoblinPrefab != null)
            {
                prefabMap["torch_goblin_project_ext"] = torchGoblinPrefab;
            }

            if (tntGoblinPrefab != null)
            {
                prefabMap["tnt_goblin_project_ext"] = tntGoblinPrefab;
            }

            if (encounterController != null)
            {
                var encounterDefinitions = mapMode == MapMode.OpenWorld
                    ? openWorldData?.regionEncounters.SelectMany(region => region.encounters).ToList() ?? new List<EncounterDefinition>()
                    : saveData.encounters;

                encounterController.Initialize(
                    encounterDefinitions,
                    prefabMap,
                    encounterRoot ?? gameplayRoot!,
                    rewardShrine,
                    hudPresenter,
                    activateRewardWhenClear: mapMode == MapMode.RoomChain);
            }

            if (animationActivationService != null && playerController != null && activeCamera != null)
            {
                animationActivationService.Configure(mapMode, playerController, activeCamera);
            }

            _initialized = true;
        }

        private static MapSaveData? ConvertOpenWorldToMapDefinition(OpenWorldSaveData? data)
        {
            if (data == null)
            {
                return null;
            }

            return new MapSaveData
            {
                schemaVersion = data.schemaVersion,
                mode = data.mode,
                seed = data.seed,
                layoutId = data.layoutId,
                tileLayers = data.tileLayers,
                markers = data.markers,
                decorations = data.decorations,
                interactives = data.interactives,
                animatedPlacements = data.animatedPlacements,
                encounters = data.regionEncounters.SelectMany(region => region.encounters).ToList(),
            };
        }
    }
}
