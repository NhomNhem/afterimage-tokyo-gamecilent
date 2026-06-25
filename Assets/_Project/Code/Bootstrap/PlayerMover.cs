using GlassRefrain.Application;
using GlassRefrain.Core;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Bootstrap {
    public sealed class PlayerMover : MonoBehaviour {
        private PlayerStateMachine _aggregate;

        [Inject]
        public void Construct(PlayerStateMachine aggregate) {
            _aggregate = aggregate;
        }

        private void LateUpdate() {
            if (_aggregate == null) return;

            var frame = _aggregate.Frame;

            transform.position = frame.Position;

            if (frame.CanRotate && frame.Facing.sqrMagnitude > 0.001f) {
                Quaternion targetRotation = Quaternion.LookRotation(frame.Facing, Vector3.up);
                float speedFactor = Mathf.Clamp01(frame.MoveSpeed / 5.0f);
                float effectiveSpeed = Mathf.Lerp(4.0f, 8.0f, speedFactor);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, effectiveSpeed * Time.deltaTime * 100f);
            }
        }
    }
}
