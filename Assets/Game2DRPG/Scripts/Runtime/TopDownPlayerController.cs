#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game2DRPG.Runtime
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class TopDownPlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset? defaultInputActions;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float dashSpeed = 12f;
        [SerializeField] private float dashDuration = 0.24f;
        [SerializeField] private float dashCooldown = 0.55f;

        private IPlayerInputSource? _inputSource;
        private Rigidbody2D? _rigidbody2D;
        private SpriteRenderer? _spriteRenderer;
        private Vector2 _moveDirection;
        private Vector2 _velocity;
        private Vector2 _lastFacingDirection = Vector2.right;
        private float _dashEndTime;
        private float _nextDashTime;
        private bool _isDashing;

        public PlayerInputSnapshot CurrentInput { get; private set; }
        public Vector2 FacingDirection => _lastFacingDirection;
        public bool IsDashing => _isDashing;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_inputSource == null && defaultInputActions != null)
            {
                _inputSource = new DefaultInputSource(defaultInputActions);
            }
        }

        private void Update()
        {
            var snapshot = _inputSource?.ReadSnapshot() ?? default;
            if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
            {
                snapshot.AttackPressed = true;
            }

            if (ArenaGameState.Instance != null && ArenaGameState.Instance.State != RunState.Playing && ArenaGameState.Instance.State != RunState.RewardSelection)
            {
                snapshot.Move = Vector2.zero;
            }

            CurrentInput = snapshot;
            _moveDirection = snapshot.Move.normalized;
            if (_moveDirection.sqrMagnitude > 0.0001f)
            {
                _lastFacingDirection = _moveDirection;
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.flipX = _lastFacingDirection.x < -0.01f;
                }
            }

            if (!_isDashing && snapshot.DashPressed && Time.time >= _nextDashTime && ArenaGameState.Instance?.State == RunState.Playing)
            {
                _isDashing = true;
                _dashEndTime = Time.time + dashDuration;
                _nextDashTime = Time.time + dashCooldown;
            }

            if (_isDashing && Time.time >= _dashEndTime)
            {
                _isDashing = false;
            }
        }

        private void FixedUpdate()
        {
            var speed = _isDashing ? dashSpeed : moveSpeed;
            _velocity = _moveDirection * speed;
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = _velocity;
            }
        }

        public void SetInputSource(IPlayerInputSource inputSource)
        {
            _inputSource = inputSource;
        }

        public void SetDefaultInputActions(InputActionAsset actions)
        {
            defaultInputActions = actions;
            _inputSource = new DefaultInputSource(actions);
        }

        private sealed class DefaultInputSource : IPlayerInputSource, IDisposable
        {
            private readonly InputActionAsset _asset;
            private readonly InputAction _moveAction;
            private readonly InputAction _attackAction;
            private readonly InputAction _dashAction;
            private readonly InputAction _jumpAction;
            private readonly InputAction _interactAction;

            public DefaultInputSource(InputActionAsset asset)
            {
                _asset = UnityEngine.Object.Instantiate(asset);
                _asset.Enable();
                _moveAction = _asset.FindAction("Player/Move", true);
                _attackAction = _asset.FindAction("Player/Attack", true);
                _dashAction = _asset.FindAction("Player/Sprint", true);
                _jumpAction = _asset.FindAction("Player/Jump", true);
                _interactAction = _asset.FindAction("Player/Interact", true);
            }

            public PlayerInputSnapshot ReadSnapshot()
            {
                var snapshot = new PlayerInputSnapshot
                {
                    Move = _moveAction.ReadValue<Vector2>(),
                    AttackPressed = _attackAction.WasPressedThisFrame(),
                    DashPressed = _dashAction.WasPressedThisFrame() || _jumpAction.WasPressedThisFrame(),
                    InteractPressed = _interactAction.WasPressedThisFrame(),
                    RestartPressed = _attackAction.WasPressedThisFrame() || _interactAction.WasPressedThisFrame(),
                };

                if (Keyboard.current != null)
                {
                    if (Keyboard.current.digit1Key.wasPressedThisFrame)
                    {
                        snapshot.RewardChoice = RewardChoice.AttackBoost;
                    }
                    else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                    {
                        snapshot.RewardChoice = RewardChoice.VitalityBoost;
                    }
                }

                return snapshot;
            }

            public void Dispose()
            {
                _asset.Disable();
                UnityEngine.Object.Destroy(_asset);
            }
        }
    }
}
