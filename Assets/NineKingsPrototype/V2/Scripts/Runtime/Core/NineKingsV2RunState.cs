#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NineKingsPrototype.V2
{
    [Serializable]
    public sealed class RunState
    {
        public int year = 1;
        public int lives = 3;
        public int gold;
        public int merchantRerollCost = 10;
        public int rewardRerollCost = 10;
        public string playerKingId = string.Empty;
        public string currentEnemyKingId = string.Empty;
        public RunPhase phase = RunPhase.MainMenu;
        public List<string> handCardIds = new();
        public List<string> deckCardIds = new();
        public List<string> discardCardIds = new();
        public List<string> selectedDecreeIds = new();
        public List<PlotState> plots = new();

        [NonSerialized] private Dictionary<BoardCoord, PlotState>? _plotLookup;

        public static RunState CreateNew(ContentDatabase database, string playerKingId)
        {
            var king = database.GetKing(playerKingId) ?? throw new InvalidOperationException($"Missing king: {playerKingId}");
            var run = new RunState
            {
                playerKingId = playerKingId,
                currentEnemyKingId = database.GetDefaultEnemyKingId(playerKingId),
                phase = RunPhase.RunIntro,
            };

            for (var y = 0; y < 5; y++)
            {
                for (var x = 0; x < 5; x++)
                {
                    run.plots.Add(new PlotState
                    {
                        coord = new BoardCoord(x, y),
                        unlocked = x >= 1 && x <= 3 && y >= 1 && y <= 3,
                    });
                }
            }

            if (!string.IsNullOrEmpty(king.baseCardId))
            {
                run.handCardIds.Add(king.baseCardId);
            }

            foreach (var cardId in king.cardIds)
            {
                if (run.handCardIds.Count >= 4)
                {
                    run.deckCardIds.Add(cardId);
                    continue;
                }

                if (!string.Equals(cardId, king.baseCardId, StringComparison.Ordinal))
                {
                    run.handCardIds.Add(cardId);
                }
            }

            run.RebuildLookup();
            return run;
        }

        public void RebuildLookup()
        {
            _plotLookup = plots.ToDictionary(plot => plot.coord, plot => plot);
        }

        public PlotState GetPlot(BoardCoord coord)
        {
            _plotLookup ??= plots.ToDictionary(plot => plot.coord, plot => plot);
            return _plotLookup[coord];
        }

        public IReadOnlyList<PlotState> GetUnlockedPlots()
        {
            return plots.Where(plot => plot.unlocked).ToList();
        }

        public IEnumerable<PlotState> GetAdjacentPlots(BoardCoord coord)
        {
            yield return TryGet(new BoardCoord(coord.x + 1, coord.y));
            yield return TryGet(new BoardCoord(coord.x - 1, coord.y));
            yield return TryGet(new BoardCoord(coord.x, coord.y + 1));
            yield return TryGet(new BoardCoord(coord.x, coord.y - 1));

            PlotState TryGet(BoardCoord target)
            {
                _plotLookup ??= plots.ToDictionary(plot => plot.coord, plot => plot);
                return _plotLookup.TryGetValue(target, out var plot) ? plot : null!;
            }
        }

        public bool TryUnlockNextPlotRing()
        {
            var lockedRingCells = new[]
            {
                new BoardCoord(0, 2), new BoardCoord(2, 0), new BoardCoord(4, 2), new BoardCoord(2, 4),
                new BoardCoord(0, 1), new BoardCoord(0, 3), new BoardCoord(1, 0), new BoardCoord(3, 0),
                new BoardCoord(4, 1), new BoardCoord(4, 3), new BoardCoord(1, 4), new BoardCoord(3, 4),
                new BoardCoord(0, 0), new BoardCoord(0, 4), new BoardCoord(4, 0), new BoardCoord(4, 4),
            };

            foreach (var coord in lockedRingCells)
            {
                var plot = GetPlot(coord);
                if (!plot.unlocked)
                {
                    plot.unlocked = true;
                    return true;
                }
            }

            return false;
        }
    }
}
