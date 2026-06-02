using System;
using UnityEngine;

namespace GlassRefrain.Presentation {
    [Serializable]
    public sealed class M0AnimationClipTransition {
        [SerializeField] private AnimationClip clip;
        [SerializeField, Min(0f)] private float fadeDuration = 0.1f;

        public AnimationClip Clip => clip;
        public float FadeDuration => fadeDuration;
        public bool IsAssigned => clip != null;
    }
}
