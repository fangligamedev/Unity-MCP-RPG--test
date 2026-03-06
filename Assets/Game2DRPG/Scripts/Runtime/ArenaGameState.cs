#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game2DRPG.Runtime
{
    public sealed class ArenaGameState : MonoBehaviour
    {
        [SerializeField] private HudPresenter? hud;
        [SerializeField] private PlayerCombat? playerCombat;
        [SerializeField] private Health? playerHealth;

        public static ArenaGameState? Instance { get; private set; }
        public RunState State { get; private set; } = RunState.Playing;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (playerHealth == null || hud == null)
            {
                return;
            }

            if (State == RunState.Defeat || State == RunState.Victory)
            {
                var controller = FindAnyObjectByType<TopDownPlayerController>();
                if (controller != null && controller.CurrentInput.RestartPressed)
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            }

            if (State == RunState.RewardSelection)
            {
                var controller = FindAnyObjectByType<TopDownPlayerController>();
                if (controller == null)
                {
                    return;
                }

                switch (controller.CurrentInput.RewardChoice)
                {
                    case RewardChoice.AttackBoost:
                        playerCombat.IncreaseAttackPower(1);
                        SetVictory();
                        break;
                    case RewardChoice.VitalityBoost:
                        playerHealth.IncreaseMaxHealth(1, alsoHeal: true);
                        playerHealth.Restore(2);
                        SetVictory();
                        break;
                }
            }
        }

        public void SetReferences(HudPresenter presenter, PlayerCombat combat, Health health)
        {
            hud = presenter;
            playerCombat = combat;
            playerHealth = health;
            hud.SetHealth(health.CurrentHealth, health.MaxHealth);
            hud.HideRewardPanel();
            hud.HideState();
        }

        public void SetPlaying()
        {
            State = RunState.Playing;
            hud?.HideRewardPanel();
            hud?.HideState();
        }

        public void OpenRewardSelection()
        {
            State = RunState.RewardSelection;
            hud?.ShowRewardPanel();
            hud?.SetPrompt("Press 1 or 2 to choose a blessing");
        }

        public void SetVictory()
        {
            State = RunState.Victory;
            hud?.HideRewardPanel();
            hud?.ShowState("Victory\nPress Attack or Interact to restart");
            hud?.SetPrompt(string.Empty);
        }

        public void SetDefeat()
        {
            State = RunState.Defeat;
            hud?.HideRewardPanel();
            hud?.ShowState("Defeat\nPress Attack or Interact to restart");
            hud?.SetPrompt(string.Empty);
        }

        public void NotifyHealthChanged()
        {
            if (playerHealth != null)
            {
                hud?.SetHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }
    }
}
