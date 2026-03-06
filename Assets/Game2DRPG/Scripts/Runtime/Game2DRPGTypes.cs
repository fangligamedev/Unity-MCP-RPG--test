#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;

namespace Game2DRPG.Runtime
{
    public enum RunState
    {
        Playing,
        RewardSelection,
        Victory,
        Defeat,
    }

    public enum RewardChoice
    {
        None,
        AttackBoost,
        VitalityBoost,
    }

    public struct PlayerInputSnapshot
    {
        public Vector2 Move;
        public bool AttackPressed;
        public bool DashPressed;
        public bool InteractPressed;
        public bool RestartPressed;
        public RewardChoice RewardChoice;
    }

    public interface IPlayerInputSource
    {
        PlayerInputSnapshot ReadSnapshot();
    }
}
