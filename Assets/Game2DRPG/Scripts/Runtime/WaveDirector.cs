#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game2DRPG.Runtime
{
    public sealed class WaveDirector : MonoBehaviour
    {
        [System.Serializable]
        public struct WaveDefinition
        {
            public int TorchCount;
            public int TntCount;
        }

        [SerializeField] private GameObject? torchEnemyPrefab;
        [SerializeField] private GameObject? tntEnemyPrefab;
        [SerializeField] private Transform[] spawnPoints = new Transform[0];
        [SerializeField] private RewardShrine? rewardShrine;
        [SerializeField] private HudPresenter? hud;
        [SerializeField] private float interWaveDelay = 1.1f;
        [SerializeField] private WaveDefinition[] waves = new[]
        {
            new WaveDefinition { TorchCount = 3, TntCount = 0 },
            new WaveDefinition { TorchCount = 2, TntCount = 1 },
            new WaveDefinition { TorchCount = 3, TntCount = 2 },
        };

        private readonly List<Health> _aliveEnemies = new();
        private int _currentWaveIndex;
        private bool _spawning;
        private bool _rewardActivated;

        public int CurrentWave => Mathf.Clamp(_currentWaveIndex == 0 ? 1 : _currentWaveIndex, 1, waves.Length);
        public int TotalWaves => waves.Length;
        public int AliveEnemyCount => _aliveEnemies.Count;

        private void Start()
        {
            StartCoroutine(BeginFirstWave());
        }

        public void SetReferences(HudPresenter presenter, RewardShrine shrine)
        {
            hud = presenter;
            rewardShrine = shrine;
            UpdateHud();
        }

        private IEnumerator BeginFirstWave()
        {
            yield return new WaitForSeconds(0.5f);
            SpawnCurrentWave();
        }

        private void Update()
        {
            _aliveEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
            UpdateHud();

            if (_spawning || ArenaGameState.Instance?.State != RunState.Playing)
            {
                return;
            }

            if (_aliveEnemies.Count > 0)
            {
                return;
            }

            if (_currentWaveIndex >= waves.Length)
            {
                if (!_rewardActivated)
                {
                    _rewardActivated = true;
                    rewardShrine?.ActivateShrine();
                    hud?.SetPrompt("Approach the blessing shrine");
                }
                return;
            }

            StartCoroutine(SpawnAfterDelay());
        }

        private IEnumerator SpawnAfterDelay()
        {
            _spawning = true;
            hud?.SetPrompt("Next wave incoming...");
            yield return new WaitForSeconds(interWaveDelay);
            SpawnCurrentWave();
            _spawning = false;
        }

        private void SpawnCurrentWave()
        {
            if (_currentWaveIndex >= waves.Length || spawnPoints.Length == 0)
            {
                return;
            }

            var wave = waves[_currentWaveIndex];
            SpawnBatch(torchEnemyPrefab, wave.TorchCount, 0);
            SpawnBatch(tntEnemyPrefab, wave.TntCount, wave.TorchCount);
            _currentWaveIndex++;
            hud?.SetPrompt("Clear the room");
            UpdateHud();
        }

        private void SpawnBatch(GameObject? prefab, int count, int offset)
        {
            for (var i = 0; i < count; i++)
            {
                SpawnEnemy(prefab, i + offset);
            }
        }

        private void SpawnEnemy(GameObject? prefab, int index)
        {
            if (prefab == null || spawnPoints.Length == 0)
            {
                return;
            }

            var point = spawnPoints[index % spawnPoints.Length];
            var enemy = Instantiate(prefab, point.position, Quaternion.identity);
            var health = enemy.GetComponent<Health>();
            if (health != null)
            {
                _aliveEnemies.Add(health);
            }
        }

        private void UpdateHud()
        {
            hud?.SetWave(CurrentWave, waves.Length);
            hud?.SetEnemies(_aliveEnemies.Count(enemy => enemy != null && !enemy.IsDead));
        }
    }
}
