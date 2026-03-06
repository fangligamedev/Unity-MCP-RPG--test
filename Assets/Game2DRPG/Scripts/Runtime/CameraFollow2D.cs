#nullable enable
/*
 * Copyright (c) 2026.
 */

using UnityEngine;

namespace Game2DRPG.Runtime
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform? target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        private Vector3 _velocity;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
            transform.position = followTarget.position + offset;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothTime);
        }
    }
}
