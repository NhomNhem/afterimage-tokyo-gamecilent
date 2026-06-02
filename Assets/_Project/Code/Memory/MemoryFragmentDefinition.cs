using UnityEngine;

namespace GlassRefrain.Memory {
    [CreateAssetMenu(
        fileName = "MemoryFragmentDefinition",
        menuName = "GlassRefrain/Memory/Memory Fragment Definition")]
    public sealed class MemoryFragmentDefinition : ScriptableObject {
        [SerializeField] private string stableId = "memory-fragment";
        [SerializeField] private string title = "Memory Fragment";
        [SerializeField, TextArea] private string shortText = "A fragmented trace remains here.";
        [SerializeField] private Sprite icon;
        [SerializeField] private AudioClip revealSfx;
        [SerializeField] private AnimationClip revealClip;
        [SerializeField] private string presentationProfile = "default";

        public string StableId => stableId;
        public string Title => title;
        public string ShortText => shortText;
        public Sprite Icon => icon;
        public AudioClip RevealSfx => revealSfx;
        public AnimationClip RevealClip => revealClip;
        public string PresentationProfile => presentationProfile;
    }
}
