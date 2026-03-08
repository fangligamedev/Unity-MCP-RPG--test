#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NineKingsPrototype
{
    public sealed class NineKingsGameController : MonoBehaviour
    {
        private enum NKSelectionMode
        {
            None,
            BlessingTarget,
        }

        private NineKingsContentDatabase? _database;
        private NineKingsRuntimeUI? _ui;
        private NineKingsBattleController? _battle;
        private NKGMCommandService? _gm;
        private NKRunState? _state;
        private NKRunPhase _phase = NKRunPhase.Boot;
        private readonly Queue<NKYearEventType> _eventQueue = new();
        private readonly System.Random _random = new();
        private string _yearStartSnapshotJson = string.Empty;
        private string _activeBlessingId = string.Empty;
        private bool _battleWasFinal;
        private bool _gmVisible;
        private bool _isPaused;

        public int Year => _state?.Year ?? 0;
        public int Lives => _state?.Lives ?? 0;
        public int Gold => _state?.Gold ?? 0;
        public int RerollCost => _state?.RewardRerollCost ?? 0;
        public int HandCount => _state?.HandCardIds.Count ?? 0;
        public bool IsPaused => _isPaused;
        public NKRunPhase CurrentPhase => _phase;
        public string CurrentEnemyKingId => _state?.CurrentEnemyKingId ?? string.Empty;

        private string SavePath => Path.Combine(Application.persistentDataPath, "ninekings_run.json");

        private void Awake()
        {
            EnsureCamera();
            _database = Resources.Load<NineKingsContentDatabase>("NineKingsContentDatabase");
            _ui = gameObject.AddComponent<NineKingsRuntimeUI>();
            _ui.Initialize(this);
            _battle = gameObject.AddComponent<NineKingsBattleController>();
            _gm = gameObject.AddComponent<NKGMCommandService>();
            _gm.Initialize(this);

            if (_database == null)
            {
                _ui.ShowMessageModal("九王原型", "缺少 Resources/NineKingsContentDatabase 资源。", "确定", () => { });
                return;
            }

            ShowMainMenu();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                _gmVisible = !_gmVisible;
                _ui?.ToggleGM(_gmVisible);
            }

            if (_gmVisible && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame && _ui != null)
            {
                _ui.UpdateGmLog(_gm!.Logs);
            }
        }

        public void ExecuteGmCommand(string? command)
        {
            if (_gm == null)
            {
                return;
            }

            _gm.Execute(command ?? string.Empty);
            _ui?.UpdateGmLog(_gm.Logs);
            RefreshUi();
        }

        public void SetPausedState(bool paused)
        {
            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            RefreshUi();
        }

        public void ResetRun()
        {
            Time.timeScale = 1f;
            _isPaused = false;
            StartNewRun();
        }

        public void RestartCurrentYear()
        {
            if (_database == null || string.IsNullOrEmpty(_yearStartSnapshotJson))
            {
                return;
            }

            var save = JsonUtility.FromJson<NKSaveGameState>(_yearStartSnapshotJson);
            if (save == null)
            {
                return;
            }

            _state = new NKRunState(save.runState);
            _phase = save.phase;
            _battleWasFinal = save.battleWasFinal;
            RefreshUi();
            ContinuePhaseAfterReload();
        }

        public void SetYear(int year)
        {
            if (_state == null)
            {
                return;
            }

            _state.Year = year;
            BeginYear();
        }

        public void SetGold(int gold)
        {
            if (_state == null)
            {
                return;
            }

            _state.Gold = gold;
            RefreshUi();
        }

        public void AddGold(int amount)
        {
            if (_state == null)
            {
                return;
            }

            _state.Gold += amount;
            RefreshUi();
        }

        public void SetLives(int lives)
        {
            if (_state == null)
            {
                return;
            }

            _state.Lives = lives;
            RefreshUi();
        }

        public void AddLives(int amount)
        {
            if (_state == null)
            {
                return;
            }

            _state.Lives += amount;
            RefreshUi();
        }

        public void AddCardToHand(string cardId)
        {
            if (_state == null || _database?.GetCard(cardId) == null)
            {
                return;
            }

            _state.AddCardToHand(cardId);
            RefreshUi();
        }

        public void RemoveCardFromHand(string cardId)
        {
            if (_state == null)
            {
                return;
            }

            _state.RemoveCardFromHand(cardId);
            RefreshUi();
        }

        public void UnlockPlot(int x, int y)
        {
            if (_state == null)
            {
                return;
            }

            var plot = _state.GetPlot(x, y);
            plot.unlocked = true;
            RefreshUi();
        }

        public void ClearPlot(int x, int y)
        {
            if (_state == null)
            {
                return;
            }

            var plot = _state.GetPlot(x, y);
            plot.cardId = string.Empty;
            plot.level = 0;
            plot.shieldCharges = 0;
            plot.unitBonus = 0;
            plot.damageMultiplier = 1f;
            plot.blessingMarked = false;
            RefreshUi();
        }

        public void SetPlotCard(int x, int y, string cardId, int level)
        {
            if (_state == null)
            {
                return;
            }

            var plot = _state.GetPlot(x, y);
            plot.unlocked = true;
            plot.cardId = cardId;
            plot.level = Mathf.Clamp(level, 1, 3);
            RefreshUi();
        }

        public void SetPlotLevel(int x, int y, int level)
        {
            if (_state == null)
            {
                return;
            }

            _state.GetPlot(x, y).level = Mathf.Clamp(level, 0, 3);
            RefreshUi();
        }

        public void ForceEvent(NKYearEventType eventType)
        {
            ShowEvent(eventType);
        }

        public void ForceEnemy(string kingId)
        {
            if (_state == null)
            {
                return;
            }

            _state.CurrentEnemyKingId = kingId;
            RefreshUi();
        }

        public void ForceBattleOutcome(bool victory)
        {
            _battle?.ForceFinish(victory);
        }

        public void SetRerollCost(int value)
        {
            if (_state == null)
            {
                return;
            }

            _state.RewardRerollCost = value;
            RefreshUi();
        }

        public bool TryPlayCardFromUI(string cardId, int x, int y)
        {
            if (_database == null || _state == null || _phase != NKRunPhase.CardPlay)
            {
                return false;
            }

            var definition = _database.GetCard(cardId);
            if (definition == null)
            {
                return false;
            }

            var plot = _state.GetPlot(x, y);
            var played = _state.TryPlayCardToPlot(cardId, definition, plot);
            if (!played)
            {
                return false;
            }

            _ui?.AppendLog($"已将 {NKChineseText.CardName(definition)} 放入地块 ({x},{y})。" );
            RefreshUi();
            TryAutoStartBattle();
            return true;
        }

        public void TryDiscardCardFromUI(string cardId)
        {
            if (_state == null || _phase != NKRunPhase.CardPlay)
            {
                return;
            }

            var discarded = _state.DiscardCard(cardId);
            if (!discarded)
            {
                return;
            }

            if (_state.Data.selectedDecreeIds.Contains("golden_well"))
            {
                _state.Gold += 3;
            }

            _ui?.AppendLog($"已将 {NKChineseText.CardName(cardId, cardId)} 丢进井中。" );
            RefreshUi();
            TryAutoStartBattle();
        }

        public void OnPlotClicked(int x, int y)
        {
            if (_state == null)
            {
                return;
            }

            if (_selectionMode == NKSelectionMode.BlessingTarget)
            {
                var plot = _state.GetPlot(x, y);
                if (!plot.unlocked)
                {
                    _ui?.AppendLog("祝福目标必须是已解锁地块。");
                    return;
                }

                _state.SetPendingBlessing(_activeBlessingId, x, y);
                _selectionMode = NKSelectionMode.None;
                _activeBlessingId = string.Empty;
                _ui?.AppendLog($"祝福目标已设置为 ({x},{y})。" );
                ContinueEventsOrCardPhase();
                return;
            }

            var currentPlot = _state.GetPlot(x, y);
            var definition = currentPlot.IsEmpty || _database == null ? null : _database.GetCard(currentPlot.cardId);
            _ui?.SetDetails(BuildPlotDetails(currentPlot, definition));
        }

        public void OnBaseBreached(bool finalBattle)
        {
            if (_state == null)
            {
                return;
            }

            if (finalBattle)
            {
                _state.Lives = 0;
                _ui?.AppendLog("最终决战中基地被突破，王国立即崩溃。" );
            }
            else
            {
                _state.Lives = Mathf.Max(0, _state.Lives - 1);
                _ui?.AppendLog("基地被突破，失去 1 点生命。" );
            }

            RefreshUi();
        }

        public void SaveRun()
        {
            if (_state == null)
            {
                return;
            }

            var save = _state.CreateSave(_phase, _battleWasFinal);
            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            _ui?.AppendLog($"存档已保存到：{SavePath}" );
        }

        public void DebugStartNewRun()
        {
            StartNewRun();
        }

        public NKRunDebugSnapshot? GetDebugSnapshot()
        {
            return _state?.CreateDebugSnapshot(_phase);
        }

        private NKSelectionMode _selectionMode;

        private void ShowMainMenu()
        {
            _phase = NKRunPhase.MainMenu;
            RefreshUi();
            _ui?.ShowChoiceModal(
                "九王原型",
                "选择开始方式。这个模块与旧玩法系统隔离，专注 33 年单王国 Alpha。",
                new[] { "新开局", "读取存档" },
                index =>
                {
                    if (index == 0)
                    {
                        StartNewRun();
                    }
                    else
                    {
                        LoadRun();
                    }
                },
                false,
                null,
                null);
        }

        private void StartNewRun()
        {
            if (_database == null)
            {
                return;
            }

            _state = NKRunState.CreateNew(_database);
            _battleWasFinal = false;
            _selectionMode = NKSelectionMode.None;
            _activeBlessingId = string.Empty;
            BeginYear();
        }

        private void LoadRun()
        {
            if (!File.Exists(SavePath))
            {
                StartNewRun();
                return;
            }

            var json = File.ReadAllText(SavePath);
            var save = JsonUtility.FromJson<NKSaveGameState>(json);
            if (save == null)
            {
                StartNewRun();
                return;
            }

            _state = new NKRunState(save.runState);
            _phase = save.phase;
            _battleWasFinal = save.battleWasFinal;
            RefreshUi();
            ContinuePhaseAfterReload();
        }

        private void ContinuePhaseAfterReload()
        {
            if (_phase == NKRunPhase.Battle)
            {
                BeginBattle();
                return;
            }

            if (_phase == NKRunPhase.CardPlay || _phase == NKRunPhase.Event || _phase == NKRunPhase.YearStart)
            {
                BeginYear();
            }
        }

        private void BeginYear()
        {
            if (_database == null || _state == null)
            {
                return;
            }

            Time.timeScale = 1f;
            _isPaused = false;
            _eventQueue.Clear();
            _phase = NKRunPhase.YearStart;
            _battleWasFinal = _state.Year == 33;

            if (!_state.RemainingEnemyKingIds.Contains(_state.CurrentEnemyKingId) && _state.RemainingEnemyKingIds.Count > 0)
            {
                _state.CurrentEnemyKingId = _state.RemainingEnemyKingIds[0];
            }
            else if (_state.RemainingEnemyKingIds.Count > 0)
            {
                _state.CurrentEnemyKingId = _state.RemainingEnemyKingIds[_random.Next(_state.RemainingEnemyKingIds.Count)];
            }

            foreach (var eventType in _database.GetEventsForYear(_state.Year))
            {
                _eventQueue.Enqueue(eventType);
            }

            ApplyAnnualGrowth();
            RefreshUi();
            SnapshotYearStart();
            ContinueEventsOrCardPhase();
        }

        private void SnapshotYearStart()
        {
            if (_state == null)
            {
                return;
            }

            var snapshot = _state.CreateSave(_phase, _battleWasFinal);
            _yearStartSnapshotJson = JsonUtility.ToJson(snapshot, true);
        }

        private void ContinueEventsOrCardPhase()
        {
            if (_eventQueue.Count > 0)
            {
                _phase = NKRunPhase.Event;
                ShowEvent(_eventQueue.Dequeue());
                RefreshUi();
                return;
            }

            EnterCardPhase();
        }

        private void EnterCardPhase()
        {
            if (_state == null)
            {
                return;
            }

            _phase = NKRunPhase.CardPlay;
            _ui?.AppendLog($"第 {_state.Year} 年开始。请出牌或弃牌，直到手中只剩 {GetRequiredRemainingHandCount()} 张牌。" );
            RefreshUi();
            TryAutoStartBattle();
        }

        private int GetRequiredRemainingHandCount()
        {
            return _battleWasFinal ? 0 : 2;
        }

        private bool HasBasePlaced()
        {
            return _state != null && _state.Plots.Any(plot => plot.HasCard(_database!.playerKing.baseCardId));
        }

        private void TryAutoStartBattle()
        {
            if (_state == null || _phase != NKRunPhase.CardPlay)
            {
                return;
            }

            if (!HasBasePlaced())
            {
                return;
            }

            if (_state.HandCardIds.Count <= GetRequiredRemainingHandCount())
            {
                BeginBattle();
            }
        }

        private void BeginBattle()
        {
            if (_database == null || _state == null || _battle == null || _ui == null)
            {
                return;
            }

            _phase = NKRunPhase.Battle;
            RefreshUi();
            _ui.AppendLog($"与 {GetCurrentEnemyDisplayName()} 的战斗开始。{(_battleWasFinal ? "最终决战！" : string.Empty)}");
            _battle.BeginBattle(this, _ui, _database, _state, _battleWasFinal, OnBattleFinished);
        }

        private void OnBattleFinished(bool victory)
        {
            if (_state == null || _database == null)
            {
                return;
            }

            _phase = NKRunPhase.Reward;
            _state.Gold += 9;
            RefreshUi();

            if (_battleWasFinal)
            {
                if (victory && _state.Lives > 0)
                {
                    _phase = NKRunPhase.Victory;
                    RefreshUi();
                    _ui?.ShowMessageModal("胜利", "你已经打通 33 年主循环。", "新开局", StartNewRun);
                }
                else
                {
                    ShowGameOver();
                }
                return;
            }

            if (_state.Lives <= 0)
            {
                ShowGameOver();
                return;
            }

            ShowRewardDraft(victory);
        }

        private void ShowGameOver()
        {
            _phase = NKRunPhase.GameOver;
            RefreshUi();
            _ui?.ShowChoiceModal(
                "失败",
                "王国已经崩溃。你可以重开、读档或回主菜单。",
                new[] { "新开局", "读取存档", "返回主菜单" },
                index =>
                {
                    if (index == 0)
                    {
                        StartNewRun();
                    }
                    else if (index == 1)
                    {
                        LoadRun();
                    }
                    else
                    {
                        ShowMainMenu();
                    }
                },
                false,
                null,
                null);
        }

        private void ShowRewardDraft(bool victory)
        {
            if (_state == null || _database == null)
            {
                return;
            }

            var source = victory
                ? (_database.GetOpponentKing(_state.CurrentEnemyKingId)?.rewardPoolIds ?? new List<string>())
                : _database.playerKing.cardIds;
            var offers = PickCards(source, 3);
            ShowRewardChoices(victory, offers);
        }

        private void ShowRewardChoices(bool victory, List<string> offers)
        {
            var labels = offers.Select(BuildCardLabel).ToList();
            _ui?.ShowChoiceModal(
                victory ? "战后奖励" : "战败补给",
                victory ? "从被击败敌王手中掠夺一张牌。" : "战败后从自己王国中补回一张牌。",
                labels,
                index =>
                {
                    if (_state != null)
                    {
                        _state.AddCardToHand(offers[index]);
                        AdvanceToNextYear();
                    }
                },
                true,
                $"重掷（{_state!.RewardRerollCost} 金币）",
                () => RerollReward(victory));
        }

        private void RerollReward(bool victory)
        {
            if (_state == null || _database == null || _state.Gold < _state.RewardRerollCost)
            {
                return;
            }

            _state.Gold -= _state.RewardRerollCost;
            _state.RewardRerollCost += 10;
            var source = victory
                ? (_database.GetOpponentKing(_state.CurrentEnemyKingId)?.rewardPoolIds ?? new List<string>())
                : _database.playerKing.cardIds;
            ShowRewardChoices(victory, PickCards(source, 3));
            RefreshUi();
        }

        private void AdvanceToNextYear()
        {
            if (_state == null)
            {
                return;
            }

            _battleWasFinal = false;
            _state.Year += 1;
            SaveRun();
            if (_state.Year > 33)
            {
                _phase = NKRunPhase.Victory;
                RefreshUi();
                _ui?.ShowMessageModal("胜利", "王国已经撑过完整的 33 年循环。", "新开局", StartNewRun);
                return;
            }

            BeginYear();
        }

        private void ShowEvent(NKYearEventType eventType)
        {
            switch (eventType)
            {
                case NKYearEventType.RoyalCouncil:
                    ShowRoyalCouncil();
                    break;
                case NKYearEventType.BlessingReveal:
                    ShowBlessingReveal();
                    break;
                case NKYearEventType.BlessingResolve:
                    ResolveBlessing();
                    break;
                case NKYearEventType.DiplomatWar:
                    ShowDiplomatWar();
                    break;
                case NKYearEventType.DiplomatPeace:
                    ShowDiplomatPeace();
                    break;
                case NKYearEventType.Merchant:
                    ShowMerchant();
                    break;
                case NKYearEventType.TowerExpand:
                    ShowTowerExpansion();
                    break;
                case NKYearEventType.FinalBattle:
                    _battleWasFinal = true;
                    _ui?.ShowMessageModal("最终决战", "处理完剩余手牌后，终局之战将立即开始。", "继续", ContinueEventsOrCardPhase);
                    break;
                default:
                    ContinueEventsOrCardPhase();
                    break;
            }
        }

        private void ShowRoyalCouncil()
        {
            if (_database == null || _state == null)
            {
                return;
            }

            var decrees = _database.royalDecrees.OrderBy(_ => _random.Next()).Take(3).ToList();
            _ui?.ShowChoiceModal(
                "王室议会",
                "选择一条本局永久生效的王令。",
                decrees.Select(item => $"{NKChineseText.DecreeName(item.decreeId, item.displayName)} - {NKChineseText.DecreeDescription(item.decreeId, item.description)}").ToList(),
                index =>
                {
                    var selected = decrees[index];
                    _state.Data.selectedDecreeIds.Add(selected.decreeId);
                    if (selected.decreeId == "blessed_stone")
                    {
                        _state.Lives += 1;
                    }
                    ContinueEventsOrCardPhase();
                },
                false,
                null,
                null);
        }

        private void ShowBlessingReveal()
        {
            if (_database == null)
            {
                return;
            }

            var blessing = _database.blessings[_random.Next(_database.blessings.Count)];
            _selectionMode = NKSelectionMode.BlessingTarget;
            _activeBlessingId = blessing.blessingId;
            _ui?.ShowMessageModal("祝福显现", $"{NKChineseText.BlessingName(blessing.blessingId, blessing.displayName)}\n{NKChineseText.BlessingDescription(blessing.blessingId, blessing.description)}\n\n点击一个已解锁地块作为祝福目标。", "确定", () => { });
        }

        private void ResolveBlessing()
        {
            if (_database == null || _state == null || _state.Data.pendingBlessing == null)
            {
                ContinueEventsOrCardPhase();
                return;
            }

            var pending = _state.Data.pendingBlessing;
            var plot = _state.GetPlot(pending.targetX, pending.targetY);
            var blessing = _database.GetBlessing(pending.blessingId);
            if (blessing != null)
            {
                ApplyEffectToPlot(plot, blessing.effect);
                plot.blessingMarked = false;
                _ui?.AppendLog($"祝福已在 ({plot.x},{plot.y}) 生效。" );
            }
            _state.ResolvePendingBlessing();
            ContinueEventsOrCardPhase();
        }

        private void ShowDiplomatWar()
        {
            if (_database == null || _state == null)
            {
                return;
            }

            var kings = _database.opponentKings.Select(item => item.kingId).ToList();
            _ui?.ShowChoiceModal(
                "外交：宣战",
                "选择你未来更想掠夺的敌王。",
                kings.Select(GetOpponentLabel).ToList(),
                index =>
                {
                    _state.CurrentEnemyKingId = kings[index];
                    ContinueEventsOrCardPhase();
                },
                false,
                null,
                null);
        }

        private void ShowDiplomatPeace()
        {
            if (_database == null || _state == null || _state.RemainingEnemyKingIds.Count <= 1)
            {
                ContinueEventsOrCardPhase();
                return;
            }

            var options = _state.RemainingEnemyKingIds.ToList();
            _ui?.ShowChoiceModal(
                "外交：议和",
                "选择一个敌王从剩余循环中移除。",
                options.Select(GetOpponentLabel).ToList(),
                index =>
                {
                    var current = _state.RemainingEnemyKingIds.Where(id => id != options[index]).ToList();
                    _state.SetAvailableEnemies(current);
                    if (!_state.RemainingEnemyKingIds.Contains(_state.CurrentEnemyKingId) && _state.RemainingEnemyKingIds.Count > 0)
                    {
                        _state.CurrentEnemyKingId = _state.RemainingEnemyKingIds[0];
                    }
                    ContinueEventsOrCardPhase();
                },
                false,
                null,
                null);
        }

        private void ShowMerchant()
        {
            if (_database == null || _state == null || _database.merchants.Count == 0)
            {
                ContinueEventsOrCardPhase();
                return;
            }

            var merchant = _database.merchants[(_state.Year / 10) % _database.merchants.Count];
            var offers = BuildMerchantOffers(merchant, 3);
            ShowMerchantOffers(merchant, offers, merchant.baseBuyCost);
        }

        private void ShowMerchantOffers(NKMerchantDefinition merchant, List<string> offers, int currentBuyCost)
        {
            var labels = offers.Select(cardId => $"{BuildCardLabel(cardId)} - 价格 {currentBuyCost} 金币").ToList();
            _ui?.ShowMerchantModal(
                NKChineseText.MerchantName(merchant.merchantId, merchant.displayName),
                $"继续购买会让本次商人里的下一张牌更贵。当前重掷费用：{_state!.MerchantRerollCost} 金币",
                labels,
                index =>
                {
                    if (_state == null)
                    {
                        return;
                    }

                    if (_state.Gold < currentBuyCost)
                    {
                        _ui?.AppendLog("金币不足。" );
                        ShowMerchantOffers(merchant, offers, currentBuyCost);
                        return;
                    }

                    _state.Gold -= currentBuyCost;
                    _state.AddCardToHand(offers[index]);
                    offers.RemoveAt(index);
                    if (offers.Count == 0)
                    {
                        ContinueEventsOrCardPhase();
                        return;
                    }

                    ShowMerchantOffers(merchant, offers, currentBuyCost + merchant.additionalBuyCost);
                    RefreshUi();
                },
                () =>
                {
                    if (_state == null)
                    {
                        return;
                    }

                    if (_state.Gold < _state.MerchantRerollCost)
                    {
                        return;
                    }

                    _state.Gold -= _state.MerchantRerollCost;
                    _state.MerchantRerollCost += merchant.rerollCostStep;
                    ShowMerchantOffers(merchant, BuildMerchantOffers(merchant, 3), currentBuyCost);
                    RefreshUi();
                },
                ContinueEventsOrCardPhase,
                $"重掷（{_state!.MerchantRerollCost} 金币）");
        }

        private void ShowTowerExpansion()
        {
            if (_state == null)
            {
                return;
            }

            var unlocked = _state.TryUnlockNextExpansionTier();
            _ui?.ShowMessageModal(
                "塔楼扩地",
                unlocked ? "王国外圈地块扩展了一层。" : "王国已达到最大 5x5。",
                "继续",
                ContinueEventsOrCardPhase);
            RefreshUi();
        }

        private void ApplyAnnualGrowth()
        {
            if (_database == null || _state == null)
            {
                return;
            }

            foreach (var plot in _state.GetUnlockedPlots())
            {
                plot.damageMultiplier = 1f;
                if (plot.IsEmpty)
                {
                    continue;
                }

                var card = _database.GetCard(plot.cardId);
                if (card == null || card.cardType != NKCardType.Building)
                {
                    continue;
                }

                foreach (var neighbor in _state.GetAdjacentPlots(plot.x, plot.y))
                {
                    if (neighbor.IsEmpty)
                    {
                        continue;
                    }

                    var neighborCard = _database.GetCard(neighbor.cardId);
                    if (neighborCard == null)
                    {
                        continue;
                    }

                    if (card.cardId == "farm" && neighborCard.cardType == NKCardType.Troop)
                    {
                        neighbor.unitBonus += Mathf.Max(1, plot.level);
                    }
                    else if (card.cardId == "blacksmith" && (neighborCard.cardType == NKCardType.Troop || neighborCard.cardType == NKCardType.Tower))
                    {
                        neighbor.damageMultiplier *= 1f + (0.02f * plot.level);
                    }
                }
            }
        }

        private void ApplyEffectToPlot(NKPlotState plot, NKCardEffectDefinition effect)
        {
            switch (effect.effectType)
            {
                case NKEffectType.UpgradePlot:
                    plot.level = Mathf.Clamp(plot.level + Mathf.RoundToInt(effect.amount), 1, 3);
                    break;
                case NKEffectType.AddUnits:
                    plot.unitBonus += Mathf.RoundToInt(effect.amount);
                    break;
                case NKEffectType.AddShield:
                    plot.shieldCharges += Mathf.RoundToInt(effect.amount);
                    break;
                case NKEffectType.AddDamagePercent:
                    plot.damageMultiplier *= 1f + effect.amount;
                    break;
            }
        }

        private List<string> BuildMerchantOffers(NKMerchantDefinition merchant, int count)
        {
            if (_database == null)
            {
                return new List<string>();
            }

            var pool = _database.cards
                .Where(card => merchant.supportedCardTypes.Contains(card.cardType) && card.cardType != NKCardType.Base)
                .Select(card => card.cardId)
                .OrderBy(_ => _random.Next())
                .Take(count)
                .ToList();
            return pool;
        }

        private List<string> PickCards(IReadOnlyList<string> source, int count)
        {
            return source.OrderBy(_ => _random.Next()).Take(Mathf.Min(count, source.Count)).ToList();
        }

        private string BuildCardLabel(string cardId)
        {
            var definition = _database?.GetCard(cardId);
            return definition == null ? cardId : $"{NKChineseText.CardName(definition)} [{NKChineseText.CardType(definition.cardType)}]";
        }

        private string GetOpponentLabel(string kingId)
        {
            var definition = _database?.GetOpponentKing(kingId);
            return definition == null ? kingId : NKChineseText.KingName(definition.kingId, definition.displayName);
        }

        private string GetCurrentEnemyDisplayName()
        {
            return GetOpponentLabel(_state?.CurrentEnemyKingId ?? string.Empty);
        }

        private string BuildPlotDetails(NKPlotState plot, NKCardDefinition? definition)
        {
            if (definition == null)
            {
                return $"地块 ({plot.x},{plot.y})\n已解锁：{NKChineseText.Bool(plot.unlocked)}\n空地块";
            }

            return $"地块 ({plot.x},{plot.y})\n{NKChineseText.CardName(definition)}\n类型：{NKChineseText.CardType(definition.cardType)}\n等级：{plot.level}\n护盾：{plot.shieldCharges}\n单位加成：{plot.unitBonus}\n伤害倍率：x{plot.damageMultiplier:0.00}\n累计伤害：{plot.totalDamage}\n击杀：{plot.totalKills}\n{NKChineseText.CardDescription(definition)}";
        }

        private void RefreshUi()
        {
            _ui?.Refresh(_database, _state, _phase, GetCurrentEnemyDisplayName(), _isPaused);
            if (_state != null)
            {
                _ui?.UpdateGmLog(_gm?.Logs ?? Array.Empty<string>());
            }
        }

        private void EnsureCamera()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                return;
            }

            var cameraGo = new GameObject("Main Camera");
            camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f, 1f);
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
        }
    }
}
