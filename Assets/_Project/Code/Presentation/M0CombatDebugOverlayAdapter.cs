using UnityEngine;
using UnityEngine.UI;
using GlassRefrain.Core;

namespace GlassRefrain.Presentation
{
    public class M0CombatDebugOverlayAdapter : MonoBehaviour
    {
        [Header("UI References")]
        public Canvas debugCanvas;
        public Text combatStateLabel;
        public Text enemyIntentStateLabel;
        public Text counterWindowLabel;
        public Text lastInputLabel;
        public Text lockOnTargetLabel;

        private bool isVisible = true;

        private void Start()
        {
            if (debugCanvas != null)
            {
                isVisible = debugCanvas.gameObject.activeSelf;
            }
        }

        public void ToggleOverlay()
        {
            isVisible = !isVisible;
            if (debugCanvas != null)
            {
                debugCanvas.gameObject.SetActive(isVisible);
            }
        }

        public void UpdateCombatState(string state)
        {
            if (combatStateLabel != null)
            {
                combatStateLabel.text = $"Combat: {state}";
            }
        }

        public void UpdateEnemyIntentState(string state)
        {
            if (enemyIntentStateLabel != null)
            {
                enemyIntentStateLabel.text = $"Enemy: {state}";
            }
        }

        public void UpdateCounterWindowState(bool isOpen, float elapsed, float duration)
        {
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
            if (lastInputLabel != null)
            {
                lastInputLabel.text = $"Last Input: {action}";
            }
        }

        public void UpdateLockOnTarget(string target)
        {
            if (lockOnTargetLabel != null)
            {
                lockOnTargetLabel.text = $"LockOn: {target}";
            }
        }
    }
}
