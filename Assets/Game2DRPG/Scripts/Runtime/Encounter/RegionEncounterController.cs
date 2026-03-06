#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections.Generic;
using System.Linq;
using Game2DRPG.Runtime;
using UnityEngine;

namespace Game2DRPG.Map.Runtime
{
    public sealed class RegionEncounterController : MonoBehaviour
    {
        private sealed class EncounterRuntimeState
        {
            public EncounterDefinition definition = new();
            public readonly List<Health> aliveEnemies = new();
            public bool activated;
            public bool completed;
        }

        private readonly List<EncounterRuntimeState> _encounters = new();
        private readonly Dictionary<string, GameObject> _enemyPrefabs = new();

        private Transform? _spawnRoot;
        private RewardShrine? _rewardShrine;
        private HudPresenter? _hud;
        private bool _activateRewardWhenClear;

        public bool IsEncounterActive => _encounters.Any(encounter => encounter.activated && !encounter.completed);
        public string CurrentRoomId { get; private set; } = string.Empty;
        public string CurrentRegionId { get; private set; } = string.Empty;

        public void Initialize(
            IEnumerable<EncounterDefinition> encounters,
            Dictionary<string, GameObject> enemyPrefabs,
            Transform spawnRoot,
            RewardShrine? rewardShrine,
            HudPresenter? hud,
            bool activateRewardWhenClear)
        {
            _encounters.Clear();
            _enemyPrefabs.Clear();
            _spawnRoot = spawnRoot;
            _rewardShrine = rewardShrine;
            _hud = hud;
            _activateRewardWhenClear = activateRewardWhenClear;

            foreach (var pair in enemyPrefabs)
            {
                _enemyPrefabs[pair.Key] = pair.Value;
            }

            foreach (var encounter in encounters)
            {
                _encounters.Add(new EncounterRuntimeState { definition = encounter });
                CreateTrigger(encounter);
            }

            UpdateHud();
        }

        private void Update()
        {
            foreach (var encounter in _encounters)
            {
                if (!encounter.activated || encounter.completed)
                {
                    continue;
                }

                encounter.aliveEnemies.RemoveAll(enemy => enemy == null || enemy.IsDead);
                if (encounter.aliveEnemies.Count > 0)
                {
                    continue;
                }

                encounter.completed = true;
                _hud?.SetPrompt("区域已清空");

                if (_activateRewardWhenClear && _encounters.All(item => item.completed))
                {
                    _rewardShrine?.ActivateShrine();
                }
            }

            UpdateHud();
        }

        private void CreateTrigger(EncounterDefinition definition)
        {
            if (_spawnRoot == null)
            {
                return;
            }

            var triggerObject = new GameObject($"EncounterTrigger_{definition.id}");
            triggerObject.transform.SetParent(_spawnRoot, false);
            triggerObject.transform.position = definition.triggerBounds.center;

            var collider = triggerObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = definition.triggerBounds.size;

            var trigger = triggerObject.AddComponent<EncounterTriggerVolume>();
            trigger.Configure(this, definition.id);
        }

        private void ActivateEncounter(string encounterId)
        {
            var encounter = _encounters.FirstOrDefault(item => item.definition.id == encounterId);
            if (encounter == null || encounter.activated || encounter.completed)
            {
                return;
            }

            encounter.activated = true;
            CurrentRoomId = encounter.definition.roomId;
            CurrentRegionId = encounter.definition.regionId;
            _hud?.SetPrompt(encounter.definition.isSummonEncounter ? "召唤来袭" : "击败区域敌人");

            foreach (var enemy in encounter.definition.enemies)
            {
                if (!_enemyPrefabs.TryGetValue(enemy.enemyId, out var prefab) || prefab == null)
                {
                    continue;
                }

                for (var index = 0; index < Mathf.Max(1, enemy.count); index++)
                {
                    var offset = new Vector3(index * 0.5f, 0f, 0f);
                    var spawned = Instantiate(prefab, enemy.position + offset, Quaternion.identity, _spawnRoot);
                    var health = spawned.GetComponent<Health>();
                    if (health != null)
                    {
                        encounter.aliveEnemies.Add(health);
                    }
                }
            }

            UpdateHud();
        }

        private void UpdateHud()
        {
            if (_hud == null)
            {
                return;
            }

            var aliveCount = _encounters
                .Where(item => item.activated && !item.completed)
                .SelectMany(item => item.aliveEnemies)
                .Count(health => health != null && !health.IsDead);
            var completedCount = _encounters.Count(item => item.completed);
            var totalCount = Mathf.Max(1, _encounters.Count);

            _hud.SetEnemies(aliveCount);
            _hud.SetWave(Mathf.Clamp(completedCount + 1, 1, totalCount), totalCount);
        }

        private sealed class EncounterTriggerVolume : MonoBehaviour
        {
            private RegionEncounterController? _controller;
            private string _encounterId = string.Empty;

            public void Configure(RegionEncounterController controller, string encounterId)
            {
                _controller = controller;
                _encounterId = encounterId;
            }

            private void OnTriggerEnter2D(Collider2D other)
            {
                var health = other.GetComponentInParent<Health>();
                if (health == null || !health.IsPlayer)
                {
                    return;
                }

                _controller?.ActivateEncounter(_encounterId);
            }
        }
    }
}
