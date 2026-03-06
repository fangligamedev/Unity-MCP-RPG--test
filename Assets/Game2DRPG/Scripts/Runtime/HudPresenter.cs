#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;
using UnityEngine.UI;

namespace Game2DRPG.Runtime
{
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private Text? healthText;
        [SerializeField] private Text? waveText;
        [SerializeField] private Text? enemyText;
        [SerializeField] private Text? promptText;
        [SerializeField] private GameObject? statePanel;
        [SerializeField] private Text? stateText;
        [SerializeField] private GameObject? rewardPanel;
        [SerializeField] private Text? rewardTitleText;
        [SerializeField] private Text? rewardOption1Text;
        [SerializeField] private Text? rewardOption2Text;

        public void SetHealth(int current, int max)
        {
            if (healthText != null)
            {
                healthText.text = $"HP {current}/{max}";
            }
        }

        public void SetWave(int currentWave, int totalWaves)
        {
            if (waveText != null)
            {
                waveText.text = $"Wave {currentWave}/{totalWaves}";
            }
        }

        public void SetEnemies(int count)
        {
            if (enemyText != null)
            {
                enemyText.text = $"Enemies {count}";
            }
        }

        public void SetPrompt(string message)
        {
            if (promptText != null)
            {
                promptText.text = message;
            }
        }

        public void ShowState(string message)
        {
            if (statePanel != null)
            {
                statePanel.SetActive(true);
            }

            if (stateText != null)
            {
                stateText.text = message;
            }
        }

        public void HideState()
        {
            if (statePanel != null)
            {
                statePanel.SetActive(false);
            }
        }

        public void ShowRewardPanel()
        {
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(true);
            }

            if (rewardTitleText != null)
            {
                rewardTitleText.text = "Choose Blessing";
            }

            if (rewardOption1Text != null)
            {
                rewardOption1Text.text = "1. Attack +1";
            }

            if (rewardOption2Text != null)
            {
                rewardOption2Text.text = "2. Max HP +1 and heal 2";
            }
        }

        public void HideRewardPanel()
        {
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }
        }
    }
}
