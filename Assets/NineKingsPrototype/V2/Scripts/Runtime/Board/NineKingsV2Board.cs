#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NineKingsPrototype.V2
{
    [Serializable]
    public struct BoardCoord : IEquatable<BoardCoord>
    {
        public int x;
        public int y;

        public BoardCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(BoardCoord other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object? obj)
        {
            return obj is BoardCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public override string ToString()
        {
            return $"({x},{y})";
        }
    }

    [Serializable]
    public sealed class PlotState
    {
        public BoardCoord coord;
        public bool unlocked;
        public string cardId = string.Empty;
        public int level;
        public int bonusUnitCount;
        public int enchantmentStacks;
        public int shield;
        public float damageMultiplier = 1f;
        public bool blessingMarked;
        public int totalDamage;
        public int totalKills;

        public bool IsEmpty => string.IsNullOrEmpty(cardId);
    }

    [Serializable]
    public sealed class BoardScenePlotState
    {
        public BoardCoord coord;
        public bool highlighted;
        public bool selected;
        public bool illegal;
        public string boardSpriteId = string.Empty;
    }

    [Serializable]
    public sealed class BoardSceneState
    {
        public List<BoardScenePlotState> plots = new();
        public BoardCoord? hoveredCoord;
        public BoardCoord? selectedCoord;
    }
}
