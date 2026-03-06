#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Game2DRPG.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game2DRPG.Editor
{
    public static class Game2DRPGBuilder
    {
        private const string ArtRoot = "Assets/Game2DRPG/Art/TinySwords";
        private const string AnimationRoot = "Assets/Game2DRPG/Animations";
        private const string PrefabRoot = "Assets/Game2DRPG/Prefabs";
        private const string ScenePath = "Assets/Game2DRPG/Scenes/TinySwordsArena.unity";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Tools/Game2DRPG/Build Tiny Swords Arena")]
        public static void BuildFromMenu()
        {
            Debug.Log(BuildAll());
        }

        public static string BuildAll()
        {
            EnsureSortingLayers();
            ConfigureInputActions();
            ConfigureImportSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var assets = GatherAssets();
            CreateAnimations(assets);
            CreatePrefabs(assets);
            CreateScene(assets);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "[Success] Game2DRPG build complete.";
        }

        private static void ConfigureInputActions()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (asset == null)
            {
                throw new InvalidOperationException("InputSystem_Actions.inputactions not found.");
            }

            var attack = asset.FindAction("Player/Attack", true);
            if (!attack.bindings.Any(binding => string.Equals(binding.effectivePath, "<Keyboard>/j", StringComparison.OrdinalIgnoreCase) || string.Equals(binding.path, "<Keyboard>/j", StringComparison.OrdinalIgnoreCase)))
            {
                attack.AddBinding("<Keyboard>/j");
                EditorUtility.SetDirty(asset);
            }
        }

        private static void ConfigureImportSettings()
        {
            foreach (var path in WorldTexturePaths())
            {
                ConfigureTexture(path, 64f, isUi: false);
            }

            foreach (var path in UiTexturePaths())
            {
                ConfigureTexture(path, 100f, isUi: true);
            }
        }

        private static void ConfigureTexture(string assetPath, float ppu, bool isUi)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static BuildAssets GatherAssets()
        {
            var buildAssets = new BuildAssets
            {
                InputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath) ?? throw new InvalidOperationException("Input actions missing."),
                PlayerSprites = LoadSprites($"{ArtRoot}/Warrior_Blue.png"),
                TorchSprites = LoadSprites($"{ArtRoot}/Torch_Red.png"),
                TntSprites = LoadSprites($"{ArtRoot}/TNT_Red.png"),
                DynamiteSprites = LoadSprites($"{ArtRoot}/Dynamite.png"),
                ExplosionSprites = LoadSprites($"{ArtRoot}/Explosions.png"),
                FlatGroundSprites = LoadSprites($"{ArtRoot}/Tilemap_Flat.png").Where(sprite => sprite.rect.width >= 200f).ToArray(),
                ElevationSprites = LoadSprites($"{ArtRoot}/Tilemap_Elevation.png").Where(sprite => sprite.rect.width >= 200f).ToArray(),
                ShadowSprites = LoadSprites($"{ArtRoot}/Shadows.png"),
                WaterSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Water.png") ?? LoadSprites($"{ArtRoot}/Water.png").First(),
                FoamSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Foam.png") ?? LoadSprites($"{ArtRoot}/Foam.png").First(),
                TreeSprites = LoadSprites($"{ArtRoot}/Tree.png"),
                ShrineSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/GoldMine_Active.png") ?? LoadSprites($"{ArtRoot}/GoldMine_Active.png").First(),
                UiPanelSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/UI_Carved_9Slides.png") ?? LoadSprites($"{ArtRoot}/UI_Carved_9Slides.png").First(),
                UiBannerSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/UI_Carved_Regular.png") ?? LoadSprites($"{ArtRoot}/UI_Carved_Regular.png").First(),
                UiButtonBlueSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/UI_Button_Blue_9Slides.png") ?? LoadSprites($"{ArtRoot}/UI_Button_Blue_9Slides.png").First(),
                UiButtonHoverSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/UI_Button_Hover_9Slides.png") ?? LoadSprites($"{ArtRoot}/UI_Button_Hover_9Slides.png").First(),
                UiButtonRedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/UI_Button_Red_9Slides.png") ?? LoadSprites($"{ArtRoot}/UI_Button_Red_9Slides.png").First(),
                RockSprites = new[]
                {
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Rocks_01.png") ?? LoadSprites($"{ArtRoot}/Rocks_01.png").First(),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Rocks_02.png") ?? LoadSprites($"{ArtRoot}/Rocks_02.png").First(),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Rocks_03.png") ?? LoadSprites($"{ArtRoot}/Rocks_03.png").First(),
                    AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/Rocks_04.png") ?? LoadSprites($"{ArtRoot}/Rocks_04.png").First(),
                },
            };

            return buildAssets;
        }

        private static void CreateAnimations(BuildAssets assets)
        {
            assets.PlayerIdleClip = CreateSpriteClip($"{AnimationRoot}/Player_Idle.anim", assets.PlayerSprites, 0, 6, 10f, true);
            assets.PlayerMoveClip = CreateSpriteClip($"{AnimationRoot}/Player_Move.anim", assets.PlayerSprites, 8, 8, 10f, true);
            assets.PlayerAttackClip = CreateSpriteClip($"{AnimationRoot}/Player_Attack.anim", assets.PlayerSprites, 40, 8, 10f, false);
            assets.PlayerDeathClip = CreateSpriteClip($"{AnimationRoot}/Player_Death.anim", assets.PlayerSprites, 60, 8, 10f, false);

            assets.TorchIdleClip = CreateSpriteClip($"{AnimationRoot}/Torch_Idle.anim", assets.TorchSprites, 0, 6, 10f, true);
            assets.TorchMoveClip = CreateSpriteClip($"{AnimationRoot}/Torch_Move.anim", assets.TorchSprites, 8, 8, 10f, true);
            assets.TorchAttackClip = CreateSpriteClip($"{AnimationRoot}/Torch_Attack.anim", assets.TorchSprites, 40, 8, 10f, false);
            assets.TorchDeathClip = CreateSpriteClip($"{AnimationRoot}/Torch_Death.anim", assets.TorchSprites, 68, 7, 10f, false);

            assets.TntIdleClip = CreateSpriteClip($"{AnimationRoot}/TNT_Idle.anim", assets.TntSprites, 0, 4, 10f, true);
            assets.TntMoveClip = CreateSpriteClip($"{AnimationRoot}/TNT_Move.anim", assets.TntSprites, 4, 4, 10f, true);
            assets.TntAttackClip = CreateSpriteClip($"{AnimationRoot}/TNT_Attack.anim", assets.TntSprites, 8, 6, 10f, false);
            assets.TntDeathClip = CreateSpriteClip($"{AnimationRoot}/TNT_Death.anim", assets.TntSprites, 14, 6, 10f, false);
            assets.ExplosionClip = CreateSpriteClip($"{AnimationRoot}/Explosion.anim", assets.ExplosionSprites, 0, assets.ExplosionSprites.Length, 12f, false);

            assets.PlayerController = CreateAnimatorController($"{AnimationRoot}/Player.controller", assets.PlayerIdleClip, assets.PlayerMoveClip, assets.PlayerAttackClip, assets.PlayerDeathClip);
            assets.TorchController = CreateAnimatorController($"{AnimationRoot}/Torch.controller", assets.TorchIdleClip, assets.TorchMoveClip, assets.TorchAttackClip, assets.TorchDeathClip);
            assets.TntController = CreateAnimatorController($"{AnimationRoot}/TNT.controller", assets.TntIdleClip, assets.TntMoveClip, assets.TntAttackClip, assets.TntDeathClip);
            assets.ExplosionController = CreateSingleClipController($"{AnimationRoot}/Explosion.controller", assets.ExplosionClip);
        }

        private static void CreatePrefabs(BuildAssets assets)
        {
            assets.ExplosionPrefab = CreateExplosionPrefab(assets);
            assets.DynamitePrefab = CreateDynamitePrefab(assets);
            assets.PlayerPrefab = CreatePlayerPrefab(assets);
            assets.TorchPrefab = CreateTorchPrefab(assets);
            assets.TntPrefab = CreateTntPrefab(assets);
            assets.ShrinePrefab = CreateShrinePrefab(assets);
        }

        private static void CreateScene(BuildAssets assets)
        {
            var existingScene = SceneManager.GetSceneByPath(ScenePath);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                EditorSceneManager.CloseScene(existingScene, true);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SetActiveScene(scene);

            var environmentRoot = new GameObject("Environment");
            var systemsRoot = new GameObject("Systems");
            var spawnsRoot = new GameObject("Spawns");
            var uiRoot = new GameObject("UIRoot");

            CreateWaterAndGround(environmentRoot.transform, assets);
            CreateObstacles(environmentRoot.transform, assets);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(assets.PlayerPrefab, scene);
            player.name = "Player";
            player.transform.position = new Vector3(0f, -1.1f, 0f);

            var shrine = (GameObject)PrefabUtility.InstantiatePrefab(assets.ShrinePrefab, scene);
            shrine.name = "RewardShrine";
            shrine.transform.position = new Vector3(3.3f, 2.2f, 0f);

            var spawnPoints = new[]
            {
                CreateSpawnPoint(spawnsRoot.transform, "Spawn_A", new Vector3(-2.6f, 1.6f, 0f)),
                CreateSpawnPoint(spawnsRoot.transform, "Spawn_B", new Vector3(2.6f, 1.6f, 0f)),
                CreateSpawnPoint(spawnsRoot.transform, "Spawn_C", new Vector3(0f, 2.5f, 0f)),
                CreateSpawnPoint(spawnsRoot.transform, "Spawn_D", new Vector3(0f, 0.9f, 0f)),
            };

            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.position = new Vector3(0f, -0.25f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 4.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.18f, 0.21f);
            var cameraFollow = cameraObject.AddComponent<CameraFollow2D>();

            var arenaObject = new GameObject("ArenaGameState");
            arenaObject.transform.SetParent(systemsRoot.transform);
            var arenaState = arenaObject.AddComponent<ArenaGameState>();

            var waveObject = new GameObject("WaveDirector");
            waveObject.transform.SetParent(systemsRoot.transform);
            var waveDirector = waveObject.AddComponent<WaveDirector>();
            SetObjectReference(waveDirector, "torchEnemyPrefab", assets.TorchPrefab);
            SetObjectReference(waveDirector, "tntEnemyPrefab", assets.TntPrefab);
            SetObjectReference(waveDirector, "rewardShrine", shrine.GetComponent<RewardShrine>());
            SetArray(waveDirector, "spawnPoints", spawnPoints.Cast<UnityEngine.Object>().ToArray());

            var canvas = CreateCanvas(uiRoot.transform);
            var hud = CreateHud(canvas.transform, assets);

            var bootstrapObject = new GameObject("Bootstrap");
            bootstrapObject.transform.SetParent(systemsRoot.transform);
            var bootstrap = bootstrapObject.AddComponent<Bootstrap>();
            SetObjectReference(bootstrap, "inputActionAsset", assets.InputActions);
            SetObjectReference(bootstrap, "player", player.GetComponent<TopDownPlayerController>());
            SetObjectReference(bootstrap, "playerCombat", player.GetComponent<PlayerCombat>());
            SetObjectReference(bootstrap, "playerHealth", player.GetComponent<Health>());
            SetObjectReference(bootstrap, "waveDirector", waveDirector);
            SetObjectReference(bootstrap, "rewardShrine", shrine.GetComponent<RewardShrine>());
            SetObjectReference(bootstrap, "arenaGameState", arenaState);
            SetObjectReference(bootstrap, "hudPresenter", hud);
            SetObjectReference(bootstrap, "cameraFollow", cameraFollow);

            var eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(uiRoot.transform);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            SetObjectReference(arenaState, "hud", hud);
            SetObjectReference(arenaState, "playerCombat", player.GetComponent<PlayerCombat>());
            SetObjectReference(arenaState, "playerHealth", player.GetComponent<Health>());
            SetObjectReference(waveDirector, "hud", hud);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static GameObject CreatePlayerPrefab(BuildAssets assets)
        {
            var root = new GameObject("Player");
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = assets.PlayerSprites.First();
            spriteRenderer.sortingLayerName = "Characters";
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = assets.PlayerController;
            var rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.6f, 0.55f);
            collider.offset = new Vector2(0f, -0.22f);

            var health = root.AddComponent<Health>();
            health.Configure(8, playerOwned: true, shouldDestroyOnDeath: false);
            var controller = root.AddComponent<TopDownPlayerController>();
            SetObjectReference(controller, "defaultInputActions", assets.InputActions);
            var combat = root.AddComponent<PlayerCombat>();
            SetInt(combat, "attackDamage", 2);
            SetFloat(combat, "attackRange", 0.85f);
            SetFloat(combat, "attackRadius", 0.55f);

            return SavePrefab(root, $"{PrefabRoot}/Player.prefab");
        }

        private static GameObject CreateTorchPrefab(BuildAssets assets)
        {
            var root = new GameObject("TorchGoblin");
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = assets.TorchSprites.First();
            spriteRenderer.sortingLayerName = "Characters";
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = assets.TorchController;
            var rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.58f, 0.5f);
            collider.offset = new Vector2(0f, -0.18f);
            var health = root.AddComponent<Health>();
            health.Configure(4, playerOwned: false, shouldDestroyOnDeath: true);
            root.AddComponent<EnemyBrainTorchGoblin>();
            return SavePrefab(root, $"{PrefabRoot}/TorchGoblin.prefab");
        }

        private static GameObject CreateTntPrefab(BuildAssets assets)
        {
            var root = new GameObject("TntGoblin");
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = assets.TntSprites.First();
            spriteRenderer.sortingLayerName = "Characters";
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = assets.TntController;
            var rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.62f, 0.5f);
            collider.offset = new Vector2(0f, -0.16f);
            var health = root.AddComponent<Health>();
            health.Configure(4, playerOwned: false, shouldDestroyOnDeath: true);
            var throwOrigin = new GameObject("ThrowOrigin");
            throwOrigin.transform.SetParent(root.transform, false);
            throwOrigin.transform.localPosition = new Vector3(0.25f, 0.2f, 0f);
            var brain = root.AddComponent<EnemyBrainTntGoblin>();
            SetObjectReference(brain, "dynamitePrefab", assets.DynamitePrefab);
            SetObjectReference(brain, "throwOrigin", throwOrigin.transform);
            return SavePrefab(root, $"{PrefabRoot}/TntGoblin.prefab");
        }

        private static GameObject CreateDynamitePrefab(BuildAssets assets)
        {
            var root = new GameObject("DynamiteProjectile");
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = assets.DynamiteSprites.First();
            spriteRenderer.sortingLayerName = "Effects";
            var rigidbody = root.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.18f;
            var projectile = root.AddComponent<DynamiteProjectile>();
            SetObjectReference(projectile, "explosionPrefab", assets.ExplosionPrefab);
            return SavePrefab(root, $"{PrefabRoot}/Dynamite.prefab");
        }

        private static GameObject CreateExplosionPrefab(BuildAssets assets)
        {
            var root = new GameObject("Explosion");
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = assets.ExplosionSprites.First();
            spriteRenderer.sortingLayerName = "Effects";
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = assets.ExplosionController;
            root.AddComponent<ExplosionDamage>();
            return SavePrefab(root, $"{PrefabRoot}/Explosion.prefab");
        }

        private static GameObject CreateShrinePrefab(BuildAssets assets)
        {
            var root = new GameObject("RewardShrine");
            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = assets.ShrineSprite;
            spriteRenderer.sortingLayerName = "Props";
            var collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(1.35f, 0.9f);
            collider.offset = new Vector2(0f, -0.1f);
            root.AddComponent<RewardShrine>();
            return SavePrefab(root, $"{PrefabRoot}/RewardShrine.prefab");
        }

        private static void CreateWaterAndGround(Transform parent, BuildAssets assets)
        {
            for (var x = -6; x <= 5; x++)
            {
                for (var y = -4; y <= 3; y++)
                {
                    var water = CreateSpriteObject($"Water_{x}_{y}", assets.WaterSprite, parent, new Vector3(x, y, 0f), "Ground");
                    water.GetComponent<SpriteRenderer>().sortingOrder = -10;
                }
            }

            var groundA = CreateSpriteObject("Ground_A", assets.FlatGroundSprites.ElementAtOrDefault(0) ?? assets.FlatGroundSprites.First(), parent, new Vector3(-1.98f, -0.9f, 0f), "Ground");
            groundA.GetComponent<SpriteRenderer>().sortingOrder = -5;
            var groundB = CreateSpriteObject("Ground_B", assets.FlatGroundSprites.ElementAtOrDefault(1) ?? assets.FlatGroundSprites.First(), parent, new Vector3(2.02f, -0.9f, 0f), "Ground");
            groundB.GetComponent<SpriteRenderer>().sortingOrder = -5;
            var elevation = CreateSpriteObject("Elevation", assets.ElevationSprites.ElementAtOrDefault(0) ?? assets.FlatGroundSprites.First(), parent, new Vector3(0f, 2.05f, 0f), "Props");
            elevation.GetComponent<SpriteRenderer>().sortingOrder = -2;

            if (assets.ShadowSprites.Length > 0)
            {
                var shadow = CreateSpriteObject("Shadow", assets.ShadowSprites[0], parent, new Vector3(0f, 1.85f, 0f), "Ground");
                shadow.GetComponent<SpriteRenderer>().sortingOrder = -1;
            }

            for (var x = -4; x <= 4; x += 2)
            {
                CreateSpriteObject($"FoamTop_{x}", assets.FoamSprite, parent, new Vector3(x, 3.55f, 0f), "Effects");
                CreateSpriteObject($"FoamBottom_{x}", assets.FoamSprite, parent, new Vector3(x, -4.05f, 0f), "Effects");
            }
        }

        private static void CreateObstacles(Transform parent, BuildAssets assets)
        {
            var positions = new[]
            {
                new Vector3(-3.4f, 0.7f, 0f),
                new Vector3(-2.5f, -0.5f, 0f),
                new Vector3(3.4f, 0.7f, 0f),
                new Vector3(2.5f, -0.5f, 0f),
            };

            for (var i = 0; i < positions.Length; i++)
            {
                var treeSprite = assets.TreeSprites[i % assets.TreeSprites.Length];
                var tree = CreateSpriteObject($"Tree_{i}", treeSprite, parent, positions[i], "Props");
                tree.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.6f);
            }

            var rockPositions = new[]
            {
                new Vector3(-1.4f, 1.2f, 0f),
                new Vector3(1.2f, 1.4f, 0f),
                new Vector3(-1.1f, -1.4f, 0f),
                new Vector3(1.5f, -1.6f, 0f),
            };

            for (var i = 0; i < rockPositions.Length; i++)
            {
                var rock = CreateSpriteObject($"Rock_{i}", assets.RockSprites[i % assets.RockSprites.Length], parent, rockPositions[i], "Props");
                rock.AddComponent<BoxCollider2D>().size = new Vector2(0.65f, 0.45f);
            }
        }

        private static HudPresenter CreateHud(Transform parent, BuildAssets assets)
        {
            var hudRoot = new GameObject("HUD");
            hudRoot.transform.SetParent(parent, false);
            var presenter = hudRoot.AddComponent<HudPresenter>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var topBanner = CreateImage("TopBanner", hudRoot.transform, assets.UiBannerSprite, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(480f, 56f));
            topBanner.color = new Color(1f, 1f, 1f, 0.95f);

            var healthText = CreateText("HealthText", hudRoot.transform, font, 22, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -20f), new Vector2(220f, 40f));
            var waveText = CreateText("WaveText", hudRoot.transform, font, 22, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(220f, 40f));
            var enemyText = CreateText("EnemyText", hudRoot.transform, font, 22, TextAnchor.MiddleRight, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -20f), new Vector2(220f, 40f));
            var promptText = CreateText("PromptText", hudRoot.transform, font, 22, TextAnchor.LowerCenter, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(540f, 48f));

            var statePanel = CreateImage("StatePanel", hudRoot.transform, assets.UiPanelSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 180f)).gameObject;
            var stateText = CreateText("StateText", statePanel.transform, font, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 140f));
            statePanel.SetActive(false);

            var rewardPanel = CreateImage("RewardPanel", hudRoot.transform, assets.UiPanelSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(470f, 220f)).gameObject;
            var rewardTitle = CreateText("RewardTitle", rewardPanel.transform, font, 26, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(360f, 40f));
            var option1Background = CreateImage("Option1", rewardPanel.transform, assets.UiButtonBlueSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(360f, 56f));
            option1Background.type = Image.Type.Sliced;
            var rewardOption1 = CreateText("RewardOption1", option1Background.transform, font, 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 40f));
            var option2Background = CreateImage("Option2", rewardPanel.transform, assets.UiButtonRedSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(360f, 56f));
            option2Background.type = Image.Type.Sliced;
            var rewardOption2 = CreateText("RewardOption2", option2Background.transform, font, 22, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 40f));
            rewardPanel.SetActive(false);

            SetObjectReference(presenter, "healthText", healthText);
            SetObjectReference(presenter, "waveText", waveText);
            SetObjectReference(presenter, "enemyText", enemyText);
            SetObjectReference(presenter, "promptText", promptText);
            SetObjectReference(presenter, "statePanel", statePanel);
            SetObjectReference(presenter, "stateText", stateText);
            SetObjectReference(presenter, "rewardPanel", rewardPanel);
            SetObjectReference(presenter, "rewardTitleText", rewardTitle);
            SetObjectReference(presenter, "rewardOption1Text", rewardOption1);
            SetObjectReference(presenter, "rewardOption2Text", rewardOption2);
            return presenter;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            var canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Transform parent, Vector3 position, string sortingLayer)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;
            return gameObject;
        }

        private static Transform CreateSpawnPoint(Transform parent, string name, Vector3 position)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            return gameObject.transform;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            var image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            var text = gameObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = new Color(0.16f, 0.1f, 0.04f);
            text.alignment = anchor;
            text.text = name;
            return text;
        }

        private static AnimationClip CreateSpriteClip(string assetPath, Sprite[] sprites, int startIndex, int count, float frameRate, bool loop)
        {
            if (sprites.Length == 0)
            {
                throw new InvalidOperationException($"No sprites found for clip {assetPath}");
            }

            AssetDatabase.DeleteAsset(assetPath);
            var clip = new AnimationClip { frameRate = frameRate };
            var chosen = new List<Sprite>();
            for (var i = 0; i < count; i++)
            {
                var index = Mathf.Clamp(startIndex + i, 0, sprites.Length - 1);
                chosen.Add(sprites[index]);
            }

            if (chosen.Count == 0)
            {
                chosen.Add(sprites[0]);
            }

            var keyframes = new ObjectReferenceKeyframe[chosen.Count];
            for (var i = 0; i < chosen.Count; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = chosen[i],
                };
            }

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite",
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            SetLoop(clip, loop);
            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }

        private static AnimatorController CreateAnimatorController(string assetPath, AnimationClip idle, AnimationClip move, AnimationClip attack, AnimationClip death)
        {
            AssetDatabase.DeleteAsset(assetPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            var stateMachine = controller.layers[0].stateMachine;
            var idleState = stateMachine.AddState("Idle");
            idleState.motion = idle;
            stateMachine.defaultState = idleState;
            var moveState = stateMachine.AddState("Move");
            moveState.motion = move;
            var attackState = stateMachine.AddState("Attack");
            attackState.motion = attack;
            var deathState = stateMachine.AddState("Death");
            deathState.motion = death;

            var idleToMove = idleState.AddTransition(moveState);
            idleToMove.hasExitTime = false;
            idleToMove.duration = 0.05f;
            idleToMove.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

            var moveToIdle = moveState.AddTransition(idleState);
            moveToIdle.hasExitTime = false;
            moveToIdle.duration = 0.05f;
            moveToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            var anyToAttack = stateMachine.AddAnyStateTransition(attackState);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.02f;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 0.95f;
            attackToIdle.duration = 0.02f;

            var anyToDeath = stateMachine.AddAnyStateTransition(deathState);
            anyToDeath.hasExitTime = false;
            anyToDeath.duration = 0.02f;
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            return controller;
        }

        private static AnimatorController CreateSingleClipController(string assetPath, AnimationClip clip)
        {
            AssetDatabase.DeleteAsset(assetPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState("Play");
            state.motion = clip;
            stateMachine.defaultState = state;
            return controller;
        }

        private static void SetLoop(AnimationClip clip, bool loop)
        {
            var serializedObject = new SerializedObject(clip);
            serializedObject.FindProperty("m_AnimationClipSettings.m_LoopTime").boolValue = loop;
            serializedObject.ApplyModifiedProperties();
        }

        private static Sprite[] LoadSprites(string assetPath)
        {
            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath).OfType<Sprite>().OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
            if (sprites.Length == 0)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    return new[] { sprite };
                }
            }

            return sprites;
        }

        private static GameObject SavePrefab(GameObject root, string prefabPath)
        {
            AssetDatabase.DeleteAsset(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Failed to save prefab {prefabPath}");
            }
            return prefab;
        }

        private static void AddSceneToBuildSettings()
        {
            var currentScenes = EditorBuildSettings.scenes.ToList();
            if (currentScenes.All(scene => scene.path != ScenePath))
            {
                currentScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = currentScenes.ToArray();
            }
        }

        private static void EnsureSortingLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProperty = tagManager.FindProperty("m_SortingLayers");
            foreach (var layerName in new[] { "Ground", "Props", "Characters", "Effects", "UI" })
            {
                if (ContainsSortingLayer(layersProperty, layerName))
                {
                    continue;
                }

                var index = layersProperty.arraySize;
                layersProperty.InsertArrayElementAtIndex(index);
                var element = layersProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("name").stringValue = layerName;
                element.FindPropertyRelative("uniqueID").intValue = GenerateSortingLayerId(layersProperty);
            }
            tagManager.ApplyModifiedProperties();
        }

        private static bool ContainsSortingLayer(SerializedProperty layersProperty, string name)
        {
            for (var i = 0; i < layersProperty.arraySize; i++)
            {
                if (layersProperty.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                {
                    return true;
                }
            }

            return false;
        }

        private static int GenerateSortingLayerId(SerializedProperty layersProperty)
        {
            var existing = new HashSet<int>();
            for (var i = 0; i < layersProperty.arraySize; i++)
            {
                existing.Add(layersProperty.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").intValue);
            }

            var id = 1000;
            while (existing.Contains(id))
            {
                id++;
            }
            return id;
        }

        private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object? value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInt(UnityEngine.Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static IEnumerable<string> WorldTexturePaths()
        {
            yield return $"{ArtRoot}/Warrior_Blue.png";
            yield return $"{ArtRoot}/Torch_Red.png";
            yield return $"{ArtRoot}/TNT_Red.png";
            yield return $"{ArtRoot}/Dynamite.png";
            yield return $"{ArtRoot}/Explosions.png";
            yield return $"{ArtRoot}/Fire.png";
            yield return $"{ArtRoot}/Tilemap_Flat.png";
            yield return $"{ArtRoot}/Tilemap_Elevation.png";
            yield return $"{ArtRoot}/Shadows.png";
            yield return $"{ArtRoot}/Water.png";
            yield return $"{ArtRoot}/Foam.png";
            yield return $"{ArtRoot}/Rocks_01.png";
            yield return $"{ArtRoot}/Rocks_02.png";
            yield return $"{ArtRoot}/Rocks_03.png";
            yield return $"{ArtRoot}/Rocks_04.png";
            yield return $"{ArtRoot}/Tree.png";
            yield return $"{ArtRoot}/GoldMine_Active.png";
        }

        private static IEnumerable<string> UiTexturePaths()
        {
            yield return $"{ArtRoot}/UI_Carved_Regular.png";
            yield return $"{ArtRoot}/UI_Carved_9Slides.png";
            yield return $"{ArtRoot}/UI_Button_Blue_9Slides.png";
            yield return $"{ArtRoot}/UI_Button_Hover_9Slides.png";
            yield return $"{ArtRoot}/UI_Button_Red_9Slides.png";
        }

        private sealed class BuildAssets
        {
            public InputActionAsset InputActions;
            public Sprite[] PlayerSprites;
            public Sprite[] TorchSprites;
            public Sprite[] TntSprites;
            public Sprite[] DynamiteSprites;
            public Sprite[] ExplosionSprites;
            public Sprite[] FlatGroundSprites;
            public Sprite[] ElevationSprites;
            public Sprite[] ShadowSprites;
            public Sprite WaterSprite;
            public Sprite FoamSprite;
            public Sprite[] TreeSprites;
            public Sprite[] RockSprites;
            public Sprite ShrineSprite;
            public Sprite UiPanelSprite;
            public Sprite UiBannerSprite;
            public Sprite UiButtonBlueSprite;
            public Sprite UiButtonHoverSprite;
            public Sprite UiButtonRedSprite;
            public AnimationClip PlayerIdleClip = null!;
            public AnimationClip PlayerMoveClip = null!;
            public AnimationClip PlayerAttackClip = null!;
            public AnimationClip PlayerDeathClip = null!;
            public AnimationClip TorchIdleClip = null!;
            public AnimationClip TorchMoveClip = null!;
            public AnimationClip TorchAttackClip = null!;
            public AnimationClip TorchDeathClip = null!;
            public AnimationClip TntIdleClip = null!;
            public AnimationClip TntMoveClip = null!;
            public AnimationClip TntAttackClip = null!;
            public AnimationClip TntDeathClip = null!;
            public AnimationClip ExplosionClip = null!;
            public RuntimeAnimatorController PlayerController = null!;
            public RuntimeAnimatorController TorchController = null!;
            public RuntimeAnimatorController TntController = null!;
            public RuntimeAnimatorController ExplosionController = null!;
            public GameObject ExplosionPrefab = null!;
            public GameObject DynamitePrefab = null!;
            public GameObject PlayerPrefab = null!;
            public GameObject TorchPrefab = null!;
            public GameObject TntPrefab = null!;
            public GameObject ShrinePrefab = null!;
        }
    }
}