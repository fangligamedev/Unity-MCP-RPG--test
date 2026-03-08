#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace NineKingsPrototype
{
    public sealed class NKGMCommandService : MonoBehaviour
    {
        private NineKingsGameController? _game;
        private readonly Queue<string> _logs = new();

        public void Initialize(NineKingsGameController game)
        {
            _game = game;
        }

        public IReadOnlyCollection<string> Logs => _logs;

        public string Execute(string commandLine)
        {
            if (_game == null)
            {
                return Log("[错误] 游戏控制器尚未就绪。", false);
            }

            if (string.IsNullOrWhiteSpace(commandLine))
            {
                return Log("[提示] 请输入 GM 指令。", false);
            }

            var parts = commandLine
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrEmpty(part))
                .ToArray();
            var command = NormalizeAlias(parts[0].ToLowerInvariant());

            try
            {
                switch (command)
                {
                    case "pause":
                        _game.SetPausedState(true);
                        return Log("[成功] 已暂停。", true);
                    case "resume":
                        _game.SetPausedState(false);
                        return Log("[成功] 已继续。", true);
                    case "reset_run":
                        _game.ResetRun();
                        return Log("[成功] 已重置本局。", true);
                    case "restart_year":
                        _game.RestartCurrentYear();
                        return Log("[成功] 已重开当前年份。", true);
                    case "set_year":
                        _game.SetYear(ParseInt(parts, 1));
                        return Log($"[成功] 年份已设置为 {_game.Year}。", true);
                    case "set_gold":
                        _game.SetGold(ParseInt(parts, 1));
                        return Log($"[成功] 金币已设置为 {_game.Gold}。", true);
                    case "add_gold":
                        _game.AddGold(ParseInt(parts, 1));
                        return Log($"[成功] 当前金币：{_game.Gold}。", true);
                    case "set_lives":
                        _game.SetLives(ParseInt(parts, 1));
                        return Log($"[成功] 生命已设置为 {_game.Lives}。", true);
                    case "add_lives":
                        _game.AddLives(ParseInt(parts, 1));
                        return Log($"[成功] 当前生命：{_game.Lives}。", true);
                    case "draw_card":
                    case "add_card":
                        _game.AddCardToHand(parts[1]);
                        return Log($"[成功] 已加入卡牌：{parts[1]}。", true);
                    case "remove_card":
                        _game.RemoveCardFromHand(parts[1]);
                        return Log($"[成功] 已移除卡牌：{parts[1]}。", true);
                    case "unlock_plot":
                        _game.UnlockPlot(ParseInt(parts, 1), ParseInt(parts, 2));
                        return Log("[成功] 已解锁地块。", true);
                    case "clear_plot":
                        _game.ClearPlot(ParseInt(parts, 1), ParseInt(parts, 2));
                        return Log("[成功] 已清空地块。", true);
                    case "set_plot_card":
                        _game.SetPlotCard(ParseInt(parts, 1), ParseInt(parts, 2), parts[3], ParseInt(parts, 4));
                        return Log("[成功] 已修改地块卡牌。", true);
                    case "set_plot_level":
                        _game.SetPlotLevel(ParseInt(parts, 1), ParseInt(parts, 2), ParseInt(parts, 3));
                        return Log("[成功] 已修改地块等级。", true);
                    case "force_event":
                        _game.ForceEvent(ParseEvent(parts[1]));
                        return Log($"[成功] 已强制触发事件：{parts[1]}。", true);
                    case "force_enemy":
                        _game.ForceEnemy(parts[1]);
                        return Log($"[成功] 已切换敌方王国：{parts[1]}。", true);
                    case "win_battle":
                        _game.ForceBattleOutcome(true);
                        return Log("[成功] 已强制战斗胜利。", true);
                    case "lose_battle":
                        _game.ForceBattleOutcome(false);
                        return Log("[成功] 已强制战斗失败。", true);
                    case "set_reroll_cost":
                        _game.SetRerollCost(ParseInt(parts, 1));
                        return Log($"[成功] 重掷费用已设置为 {_game.RerollCost}。", true);
                    default:
                        return Log($"[错误] 未知指令：{command}", false);
                }
            }
            catch (Exception ex)
            {
                return Log($"[错误] {ex.Message}", false);
            }
        }

        private static string NormalizeAlias(string command)
        {
            return command switch
            {
                "暂停" => "pause",
                "继续" => "resume",
                "重置本局" => "reset_run",
                "重开本年" => "restart_year",
                _ => command,
            };
        }

        private int ParseInt(string[] parts, int index)
        {
            if (parts.Length <= index)
            {
                throw new InvalidOperationException("缺少整数参数。");
            }

            if (!int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                throw new InvalidOperationException($"非法整数：{parts[index]}");
            }

            return value;
        }

        private NKYearEventType ParseEvent(string value)
        {
            return value.ToLowerInvariant() switch
            {
                "royal_council" => NKYearEventType.RoyalCouncil,
                "blessing_reveal" => NKYearEventType.BlessingReveal,
                "blessing_resolve" => NKYearEventType.BlessingResolve,
                "diplomat_war" => NKYearEventType.DiplomatWar,
                "diplomat_peace" => NKYearEventType.DiplomatPeace,
                "merchant" => NKYearEventType.Merchant,
                "tower_expand" => NKYearEventType.TowerExpand,
                "final_battle" => NKYearEventType.FinalBattle,
                _ => throw new InvalidOperationException($"未知事件：{value}"),
            };
        }

        private string Log(string message, bool alsoConsole)
        {
            _logs.Enqueue(message);
            while (_logs.Count > 16)
            {
                _logs.Dequeue();
            }

            if (alsoConsole)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogWarning(message);
            }
            return message;
        }
    }
}