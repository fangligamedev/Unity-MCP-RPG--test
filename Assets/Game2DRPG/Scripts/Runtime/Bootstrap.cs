#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace Game2DRPG.Runtime
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private InputActionAsset? inputActionAsset;
        [SerializeField] private TopDownPlayerController? player;
        [SerializeField] private PlayerCombat? playerCombat;
        [SerializeField] private Health? playerHealth;
        [SerializeField] private WaveDirector? waveDirector;
        [SerializeField] private RewardShrine? rewardShrine;
        [SerializeField] private ArenaGameState? arenaGameState;
        [SerializeField] private HudPresenter? hudPresenter;
        [SerializeField] private CameraFollow2D? cameraFollow;
        private bool _initialized;

        private void Awake()
        {
            InitializeBindings();
        }

        public void Configure(
            InputActionAsset? actions,
            TopDownPlayerController? targetPlayer,
            PlayerCombat? combat,
            Health? health,
            WaveDirector? director,
            RewardShrine? shrine,
            ArenaGameState? gameState,
            HudPresenter? presenter,
            CameraFollow2D? follow)
        {
            inputActionAsset = actions;
            player = targetPlayer;
            playerCombat = combat;
            playerHealth = health;
            waveDirector = director;
            rewardShrine = shrine;
            arenaGameState = gameState;
            hudPresenter = presenter;
            cameraFollow = follow;
        }

        public void InitializeBindings()
        {
            if (_initialized)
            {
                return;
            }

            if (inputActionAsset != null && player != null)
            {
                player.SetDefaultInputActions(inputActionAsset);
            }

            if (arenaGameState != null && hudPresenter != null && playerCombat != null && playerHealth != null)
            {
                arenaGameState.SetReferences(hudPresenter, playerCombat, playerHealth);
                playerHealth.Changed += _ => arenaGameState.NotifyHealthChanged();
                playerHealth.Died += _ => arenaGameState.SetDefeat();
            }

            if (waveDirector != null && hudPresenter != null && rewardShrine != null)
            {
                waveDirector.SetReferences(hudPresenter, rewardShrine);
            }

            if (cameraFollow != null && player != null)
            {
                cameraFollow.SetTarget(player.transform);
            }

            arenaGameState?.SetPlaying();
            _initialized = true;
        }
    }
}
