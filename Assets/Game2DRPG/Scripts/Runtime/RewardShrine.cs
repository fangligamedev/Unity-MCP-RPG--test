#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;

namespace Game2DRPG.Runtime
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class RewardShrine : MonoBehaviour
    {
        [SerializeField] private string promptMessage = "Press E to accept a blessing";
        [SerializeField] private float interactDistance = 1.35f;

        private bool _isActive;
        private bool _playerInRange;

        private void Start()
        {
            SetVisualState(false);
        }

        private void Update()
        {
            if (!_isActive || ArenaGameState.Instance?.State != RunState.Playing)
            {
                return;
            }

            var player = FindAnyObjectByType<TopDownPlayerController>();
            if (player == null)
            {
                return;
            }

            var isNearby = Vector2.Distance(player.transform.position, transform.position) <= interactDistance;
            if (isNearby && !_playerInRange)
            {
                _playerInRange = true;
                FindAnyObjectByType<HudPresenter>()?.SetPrompt(promptMessage);
            }
            else if (!isNearby && _playerInRange)
            {
                _playerInRange = false;
                FindAnyObjectByType<HudPresenter>()?.SetPrompt("Clear the room");
            }

            if (_playerInRange && player.CurrentInput.InteractPressed)
            {
                _isActive = false;
                _playerInRange = false;
                ArenaGameState.Instance?.OpenRewardSelection();
                SetVisualState(false);
            }
        }

        public void ActivateShrine()
        {
            _isActive = true;
            SetVisualState(true);
            FindAnyObjectByType<HudPresenter>()?.SetPrompt("Approach the blessing shrine");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (!_isActive || health == null || !health.IsPlayer)
            {
                return;
            }

            _playerInRange = true;
            FindAnyObjectByType<HudPresenter>()?.SetPrompt(promptMessage);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health == null || !health.IsPlayer)
            {
                return;
            }

            _playerInRange = false;
            if (ArenaGameState.Instance?.State == RunState.Playing)
            {
                FindAnyObjectByType<HudPresenter>()?.SetPrompt("Clear the room");
            }
        }

        private void SetVisualState(bool visible)
        {
            var renderer = GetComponent<SpriteRenderer>();
            var color = renderer.color;
            color.a = visible ? 1f : 0.45f;
            renderer.color = color;
        }
    }
}
