#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;

namespace Game2DRPG.Map.Runtime
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AnimatedSpritePlayer : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField] private float framesPerSecond = 6f;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool randomizeStartFrame = true;
        [SerializeField] private bool playOnEnable = true;

        private SpriteRenderer? _renderer;
        private bool _isPlaying;
        private float _timeOffset;

        public Sprite[] Frames => frames;
        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _timeOffset = randomizeStartFrame ? Random.Range(0f, 1f) : 0f;
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!_isPlaying || _renderer == null || frames.Length == 0 || framesPerSecond <= 0.01f)
            {
                return;
            }

            var frameIndex = Mathf.FloorToInt((Time.time + _timeOffset) * framesPerSecond);
            if (loop)
            {
                frameIndex %= frames.Length;
            }
            else
            {
                frameIndex = Mathf.Min(frameIndex, frames.Length - 1);
            }

            _renderer.sprite = frames[frameIndex];
        }

        public void Configure(Sprite[] animationFrames, float fps, bool shouldLoop, bool randomizeOffset)
        {
            frames = animationFrames;
            framesPerSecond = Mathf.Max(0.1f, fps);
            loop = shouldLoop;
            randomizeStartFrame = randomizeOffset;
            _timeOffset = randomizeStartFrame ? Random.Range(0f, 1f) : 0f;

            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            if (_renderer != null && frames.Length > 0)
            {
                _renderer.sprite = frames[0];
            }
        }

        public void Play()
        {
            _isPlaying = true;
        }

        public void Stop()
        {
            _isPlaying = false;
            if (_renderer != null && frames.Length > 0)
            {
                _renderer.sprite = frames[0];
            }
        }

        public void SetActiveState(bool active)
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            if (_renderer != null)
            {
                _renderer.enabled = active;
            }

            if (active)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed partial class AnimationActivationTarget : MonoBehaviour
    {
        [SerializeField] private AnimationChannel channel;
        [SerializeField] private ActivationPolicy activationPolicy;
        [SerializeField] private string roomId = string.Empty;
        [SerializeField] private string regionId = string.Empty;
        [SerializeField] private float activationRadius = 8f;

        private AnimatedSpritePlayer? _player;
        private SpriteRenderer? _renderer;

        public AnimationChannel Channel => channel;
        public ActivationPolicy ActivationPolicy => activationPolicy;
        public string RoomId => roomId;
        public string RegionId => regionId;
        public float ActivationRadius => activationRadius;

        private void Awake()
        {
            _player = GetComponent<AnimatedSpritePlayer>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        public void Configure(AnimationChannel newChannel, ActivationPolicy newPolicy, string newRoomId, string newRegionId, float newActivationRadius)
        {
            channel = newChannel;
            activationPolicy = newPolicy;
            roomId = newRoomId;
            regionId = newRegionId;
            activationRadius = Mathf.Max(1f, newActivationRadius);
        }

        public void SetRuntimeActive(bool active)
        {
            if (_renderer != null)
            {
                _renderer.enabled = active;
            }

            if (_player != null)
            {
                _player.SetActiveState(active);
            }
        }
    }
}
