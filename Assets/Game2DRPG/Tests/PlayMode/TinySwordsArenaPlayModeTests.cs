#nullable enable
/*
 * Copyright (c) 2026.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game2DRPG.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game2DRPG.Tests.PlayMode
{
    public sealed class TinySwordsArenaPlayModeTests
    {
        private FakePlayerInputSource _inputSource = null!;
        private TopDownPlayerController _player = null!;
        private PlayerCombat _playerCombat = null!;
        private Health _playerHealth = null!;
        private WaveDirector _waveDirector = null!;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("TinySwordsArena", LoadSceneMode.Single);
            yield return null;
            yield return null;

            _inputSource = new FakePlayerInputSource();
            _player = UnityEngine.Object.FindAnyObjectByType<TopDownPlayerController>();
            Assert.That(_player, Is.Not.Null);
            _player.SetInputSource(_inputSource);
            _playerCombat = _player.GetComponent<PlayerCombat>();
            _playerHealth = _player.GetComponent<Health>();
            _waveDirector = UnityEngine.Object.FindAnyObjectByType<WaveDirector>();
            Assert.That(_playerCombat, Is.Not.Null);
            Assert.That(_playerHealth, Is.Not.Null);
            Assert.That(_waveDirector, Is.Not.Null);
            yield return WaitForSeconds(0.65f);
        }

        [UnityTest]
        public IEnumerator Player_CanMoveAndDashFurtherThanNormalMove()
        {
            _player.transform.position = new Vector3(-2.4f, -2.3f, 0f);
            _player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

            var start = _player.transform.position;
            _inputSource.Snapshot = new PlayerInputSnapshot { Move = Vector2.right };
            yield return WaitForSeconds(0.35f);
            var normalDistance = Vector3.Distance(start, _player.transform.position);

            _player.transform.position = start;
            _player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            _inputSource.Snapshot = new PlayerInputSnapshot { Move = Vector2.right, DashPressed = true };
            yield return null;
            _inputSource.Snapshot = new PlayerInputSnapshot { Move = Vector2.right, DashPressed = true };
            yield return null;
            _inputSource.Snapshot = new PlayerInputSnapshot { Move = Vector2.right };
            yield return WaitForSeconds(0.25f);
            var dashDistance = Vector3.Distance(start, _player.transform.position);

            Assert.That(normalDistance, Is.GreaterThan(0.25f));
            Assert.That(dashDistance, Is.GreaterThan(normalDistance * 1.25f));
        }

        [UnityTest]
        public IEnumerator Player_AttackCanClearWaveAndAdvanceProgress()
        {
            _playerCombat.IncreaseAttackPower(20);
            Assert.That(_waveDirector.CurrentWave, Is.EqualTo(1));
            yield return ClearCurrentWave();
            yield return WaitForCondition(() => _waveDirector.CurrentWave >= 2, 3f, "Wave 2 did not start after clearing the first wave.");
            Assert.That(_waveDirector.CurrentWave, Is.GreaterThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator TntGoblin_CanThrowDynamiteAndDealDamage()
        {
            _playerCombat.IncreaseAttackPower(20);
            yield return ClearCurrentWave();
            yield return WaitForCondition(() => _waveDirector.CurrentWave >= 2, 3f, "Wave 2 did not start after clearing the first wave.");
            yield return WaitForCondition(() => UnityEngine.Object.FindObjectsByType<EnemyBrainTntGoblin>(FindObjectsSortMode.None).Length > 0, 3f, "TNT goblin did not spawn in wave 2.");

            foreach (var torch in UnityEngine.Object.FindObjectsByType<EnemyBrainTorchGoblin>(FindObjectsSortMode.None))
            {
                yield return KillEnemy(torch.transform.position);
            }

            var tnt = UnityEngine.Object.FindAnyObjectByType<EnemyBrainTntGoblin>();
            Assert.That(tnt, Is.Not.Null);
            _player.transform.position = tnt!.transform.position + Vector3.left * 2.2f;
            _player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            var healthBefore = _playerHealth.CurrentHealth;
            yield return WaitForCondition(() => UnityEngine.Object.FindAnyObjectByType<DynamiteProjectile>() != null, 3.5f, "TNT goblin never created a dynamite projectile.");
            yield return WaitForCondition(() => _playerHealth.CurrentHealth < healthBefore, 3.5f, "Dynamite explosion did not damage the player.");
            Assert.That(_playerHealth.CurrentHealth, Is.LessThan(healthBefore));
        }

        [UnityTest]
        public IEnumerator ClearingAllWavesAndChoosingReward_TriggersVictory()
        {
            _playerCombat.IncreaseAttackPower(20);
            while (_waveDirector.CurrentWave < _waveDirector.TotalWaves || FindEnemyHealths().Any())
            {
                var enemies = FindEnemyHealths().ToList();
                if (enemies.Count > 0)
                {
                    yield return KillEnemy(enemies[0].transform.position);
                }
                else
                {
                    yield return WaitForSeconds(0.4f);
                }
            }

            yield return WaitForSeconds(0.4f);
            var shrine = UnityEngine.Object.FindAnyObjectByType<RewardShrine>();
            Assert.That(shrine, Is.Not.Null);
            _player.transform.position = shrine!.transform.position;
            _player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            yield return null;
            yield return null;
            _inputSource.Snapshot = new PlayerInputSnapshot { InteractPressed = true };
            yield return null;
            _inputSource.Snapshot = new PlayerInputSnapshot();
            yield return WaitForCondition(() => ArenaGameState.Instance?.State == RunState.RewardSelection, 2f, "Reward selection did not open after interacting with the shrine.");
            _inputSource.Snapshot = new PlayerInputSnapshot { RewardChoice = RewardChoice.AttackBoost };
            yield return null;
            _inputSource.Snapshot = new PlayerInputSnapshot();
            yield return WaitForCondition(() => ArenaGameState.Instance?.State == RunState.Victory, 2f, "Game did not enter Victory after choosing a reward.");

            Assert.That(ArenaGameState.Instance?.State, Is.EqualTo(RunState.Victory));
        }

        [UnityTest]
        public IEnumerator PlayerHealthZero_TriggersDefeat()
        {
            _playerHealth.TakeDamage(999);
            yield return null;
            Assert.That(ArenaGameState.Instance?.State, Is.EqualTo(RunState.Defeat));
        }

        private IEnumerator ClearCurrentWave()
        {
            var timeout = Time.time + 6f;
            while (Time.time < timeout)
            {
                var enemies = FindEnemyHealths().ToList();
                if (enemies.Count == 0)
                {
                    yield break;
                }

                yield return KillEnemy(enemies[0].transform.position);
            }

            Assert.Fail("Failed to clear wave within timeout.");
        }

        private IEnumerator KillEnemy(Vector3 enemyPosition)
        {
            var attackOffset = new Vector3(0.6f, 0f, 0f);
            _player.transform.position = enemyPosition - attackOffset;
            _player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            _inputSource.Snapshot = new PlayerInputSnapshot { Move = Vector2.right };
            yield return null;
            _inputSource.Snapshot = new PlayerInputSnapshot { AttackPressed = true };
            yield return null;
            _inputSource.Snapshot = new PlayerInputSnapshot();
            yield return WaitForSeconds(0.35f);
        }

        private static IEnumerable<Health> FindEnemyHealths()
        {
            return UnityEngine.Object.FindObjectsByType<Health>(FindObjectsSortMode.None).Where(health => health != null && !health.IsPlayer && !health.IsDead);
        }

        private static IEnumerator WaitForCondition(Func<bool> predicate, float timeout, string failureMessage)
        {
            var endTime = Time.time + timeout;
            while (Time.time < endTime)
            {
                if (predicate())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(failureMessage);
        }

        private static IEnumerator WaitForSeconds(float duration)
        {
            var endTime = Time.time + duration;
            while (Time.time < endTime)
            {
                yield return null;
            }
        }

        private sealed class FakePlayerInputSource : IPlayerInputSource
        {
            public PlayerInputSnapshot Snapshot;

            public PlayerInputSnapshot ReadSnapshot()
            {
                var snapshot = Snapshot;
                Snapshot.AttackPressed = false;
                Snapshot.DashPressed = false;
                Snapshot.InteractPressed = false;
                Snapshot.RestartPressed = false;
                Snapshot.RewardChoice = RewardChoice.None;
                return snapshot;
            }
        }
    }
}