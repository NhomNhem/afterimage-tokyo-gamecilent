using UnityEngine;
using UnityEngine.UIElements;
using GlassRefrain.Core;

namespace GlassRefrain.Presentation
{
    public class M0CombatDebugOverlayAdapter : MonoBehaviour
    {
        [Header("UI References")]
        public UIDocument uiDocument;

        private VisualElement root;
        private Label combatStateLabel;
        private Label enemyIntentStateLabel;
        private Label counterWindowLabel;
        private Label lastInputLabel;
        private Label lockOnTargetLabel;
        private VisualElement interactionPrompt;
        private Label interactionPromptLabel;
        private VisualElement memoryRevealFeedback;
        private Label memoryRevealFeedbackLabel;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                root = uiDocument.rootVisualElement.Q<VisualElement>("debug-overlay");
                if (root == null) return;
                combatStateLabel = root.Q<Label>("combat-state-label");
                enemyIntentStateLabel = root.Q<Label>("enemy-intent-label");
                counterWindowLabel = root.Q<Label>("counter-window-label");
                lastInputLabel = root.Q<Label>("last-input-label");
                lockOnTargetLabel = root.Q<Label>("lock-on-target-label");
                interactionPrompt = uiDocument.rootVisualElement.Q<VisualElement>("interaction-prompt");
                interactionPromptLabel = uiDocument.rootVisualElement.Q<Label>("interaction-prompt-label");
                memoryRevealFeedback = uiDocument.rootVisualElement.Q<VisualElement>("memory-reveal-feedback");
                memoryRevealFeedbackLabel = uiDocument.rootVisualElement.Q<Label>("memory-reveal-feedback-label");
            }
        }

        public void ToggleOverlay()
        {
            if (uiDocument != null)
            {
                uiDocument.gameObject.SetActive(!uiDocument.gameObject.activeSelf);
            }
        }

        public void UpdateCombatState(string state)
        {
            Initialize();
            if (combatStateLabel != null)
            {
                combatStateLabel.text = $"Combat: {state}";
            }
        }

        public void UpdateEnemyIntentState(string state)
        {
            Initialize();
            if (enemyIntentStateLabel != null)
            {
                enemyIntentStateLabel.text = $"Enemy: {state}";
            }
        }

        public void UpdateCounterWindowState(bool isOpen, float elapsed, float duration)
        {
            Initialize();
            if (counterWindowLabel != null)
            {
                if (isOpen)
                {
                    counterWindowLabel.text = $"CounterWindow: Open {elapsed:F1}s/{duration:F1}s";
                }
                else
                {
                    counterWindowLabel.text = "CounterWindow: Closed";
                }
            }
        }

        public void UpdateLastInputAction(string action)
        {
            Initialize();
            if (lastInputLabel != null)
            {
                lastInputLabel.text = $"Last Input: {action}";
            }
        }

        public void UpdateLockOnTarget(string target)
        {
            Initialize();
            if (lockOnTargetLabel != null)
            {
                lockOnTargetLabel.text = $"LockOn: {target}";
            }
        }

        public void UpdateInteractionPrompt(bool isVisible, string fragmentId)
        {
            Initialize();
            if (interactionPrompt == null)
            {
                return;
            }

            interactionPrompt.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (interactionPromptLabel != null)
            {
                interactionPromptLabel.text = "Press F to Interact";
            }
        }

        public void UpdateMemoryRevealFeedback(IMemoryVFXResponseSnapshot snapshot)
        {
            Initialize();
            if (memoryRevealFeedback == null)
            {
                return;
            }

            bool isVisible = snapshot != null &&
                (snapshot.State == MemoryVFXResponseState.Requested ||
                 snapshot.State == MemoryVFXResponseState.Playing);
            memoryRevealFeedback.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (memoryRevealFeedbackLabel != null && isVisible)
            {
                var context = snapshot.SourceAcceptedContext;
                memoryRevealFeedbackLabel.text = context == null || string.IsNullOrEmpty(context.MemoryId)
                    ? "Memory Revealed"
                    : "Memory Revealed: " + context.MemoryId;
            }
        }
    }
}
