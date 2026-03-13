#nullable enable
using System;
using UnityEngine;

namespace NineKingsPrototype.V2
{
    [Serializable]
    public sealed class SaveGameState
    {
        public RunState runState = new();
        public BoardSceneState boardSceneState = new();
        public BattleSceneState battleSceneState = new();
        public string saveVersion = "2";
    }

    public static class JsonSnapshotUtility
    {
        public static string ToJson<T>(T data, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(data, prettyPrint);
        }

        public static T FromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
