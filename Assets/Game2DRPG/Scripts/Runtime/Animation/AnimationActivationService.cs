#nullable enable
/*
 * Copyright (c) 2026.
 */

using System.Collections.Generic;
using Game2DRPG.Runtime;
using UnityEngine;

namespace Game2DRPG.Map.Runtime
{
    public sealed class AnimationActivationService : MonoBehaviour
    {
        [SerializeField] private MapMode mapMode;
        [SerializeField] private Camera? targetCamera;
        [SerializeField] private TopDownPlayerController? player;

        private readonly List<AnimationActivationTarget> _targets = new();

        public MapMode MapMode => mapMode;

        private void Start()
        {
            RefreshTargets();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (player == null)
            {
                player = FindAnyObjectByType<TopDownPlayerController>();
            }

            if (targetCamera == null || player == null)
            {
                return;
            }

            var encounterController = FindAnyObjectByType<RegionEncounterController>();
            var context = new AnimationActivationContext
            {
                mapMode = mapMode,
                currentRoomId = encounterController?.CurrentRoomId ?? string.Empty,
                currentRegionId = encounterController?.CurrentRegionId ?? string.Empty,
                playerPosition = player.transform.position,
                cameraPosition = targetCamera.transform.position,
                inEncounter = encounterController != null && encounterController.IsEncounterActive,
            };

            for (var i = _targets.Count - 1; i >= 0; i--)
            {
                var target = _targets[i];
                if (target == null)
                {
                    _targets.RemoveAt(i);
                    continue;
                }

                target.SetRuntimeActive(ShouldBeActive(target, context));
            }
        }

        public void Configure(MapMode newMapMode, TopDownPlayerController targetPlayer, Camera activeCamera)
        {
            mapMode = newMapMode;
            player = targetPlayer;
            targetCamera = activeCamera;
            RefreshTargets();
        }

        public void RefreshTargets()
        {
            _targets.Clear();
            _targets.AddRange(FindObjectsByType<AnimationActivationTarget>(FindObjectsSortMode.None));
        }

        private static bool ShouldBeActive(AnimationActivationTarget target, AnimationActivationContext context)
        {
            switch (target.ActivationPolicy)
            {
                case ActivationPolicy.AlwaysOn:
                    return true;
                case ActivationPolicy.ByRoom:
                    return !string.IsNullOrEmpty(target.RoomId) && target.RoomId == context.currentRoomId;
                case ActivationPolicy.ByRegion:
                    return !string.IsNullOrEmpty(target.RegionId) && target.RegionId == context.currentRegionId;
                case ActivationPolicy.ByCameraProximity:
                {
                    var distance = Vector2.Distance(context.cameraPosition, target.transform.position);
                    return distance <= target.ActivationRadius;
                }
                case ActivationPolicy.ByEncounterState:
                    return context.inEncounter;
                case ActivationPolicy.ByInteractionState:
                    return false;
                default:
                    return target.Channel == AnimationChannel.AnimatedWater || target.Channel == AnimationChannel.AnimatedShoreline;
            }
        }
    }
}
