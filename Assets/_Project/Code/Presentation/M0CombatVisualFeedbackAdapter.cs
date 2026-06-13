using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace GlassRefrain.Presentation
{
    public class M0CombatVisualFeedbackAdapter : SerializedMonoBehaviour
    {
        [Header("Renderers")]
        [OdinSerialize] private Renderer playerRenderer;
        [OdinSerialize] private Renderer enemyRenderer;

        [Header("Feedback Materials")]
        [OdinSerialize] private Material playerOriginalMaterial;
        [OdinSerialize] private Material playerLightAttackMaterial;
        [OdinSerialize] private Material playerHeavyAttackMaterial;
        [OdinSerialize] private Material playerParryMaterial;
        [OdinSerialize] private Material playerCounterAvailableMaterial;
        [OdinSerialize] private Material playerCounterMaterial;

        [OdinSerialize] private Material enemyOriginalMaterial;
        [OdinSerialize] private Material enemyTelegraphMaterial;
        [OdinSerialize] private Material enemyActiveMaterial;
        [OdinSerialize] private Material enemyRecoveryMaterial;

        private Material playerCurrentMaterial;
        private Material enemyCurrentMaterial;
        private Vector3 playerOriginalScale;
        private Color playerOriginalColor;
        private bool hadOriginalColor;
        private MaterialPropertyBlock propertyBlock;
        private float feedbackTimer;
        private string currentFeedbackType;

        private void Awake()
        {
            playerCurrentMaterial = playerOriginalMaterial;
            enemyCurrentMaterial = enemyOriginalMaterial;
            playerOriginalScale = playerRenderer != null ? playerRenderer.transform.localScale : Vector3.one;
            propertyBlock = new MaterialPropertyBlock();
            if (playerRenderer != null && playerRenderer.sharedMaterial != null && playerRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                playerOriginalColor = playerRenderer.sharedMaterial.GetColor("_BaseColor");
                hadOriginalColor = true;
            }
        }

        private void Update()
        {
            if (feedbackTimer > 0f)
            {
                feedbackTimer -= Time.deltaTime;
                if (feedbackTimer <= 0f)
                {
                    ResetFeedback();
                }
            }
        }

        public void TriggerLightAttackFeedback()
        {
            if (playerRenderer == null || playerLightAttackMaterial == null) return;

            ApplyMaterialFeedback(playerRenderer, playerLightAttackMaterial, "LightAttack", 0.2f);
        }

        public void TriggerHeavyAttackFeedback()
        {
            if (playerRenderer == null || playerHeavyAttackMaterial == null) return;

            ApplyMaterialFeedback(playerRenderer, playerHeavyAttackMaterial, "HeavyAttack", 0.3f);
        }

        public void TriggerParryFeedback()
        {
            if (playerRenderer == null) return;

            if (playerParryMaterial != null)
            {
                ApplyCombinedFeedback(playerParryMaterial, 1.06f, "Parry", 0.2f);
                return;
            }

            if (playerRenderer.sharedMaterial != null && playerRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                playerRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", new Color(0f, 0.8f, 1f));
                playerRenderer.SetPropertyBlock(propertyBlock);
            }
            playerRenderer.transform.localScale = playerOriginalScale * 1.06f;
            feedbackTimer = 0.2f;
            currentFeedbackType = "Parry";
        }

        public void TriggerDodgeFeedback()
        {
            if (playerRenderer == null) return;

            ApplyScaleFeedback(0.9f, "Dodge", 0.3f);
        }

        public void TriggerCounterFeedback()
        {
            if (playerRenderer == null) return;

            if (playerCounterMaterial != null)
            {
                ApplyCombinedFeedback(playerCounterMaterial, 1.2f, "Counter", 0.5f);
                return;
            }

            if (playerRenderer.sharedMaterial != null && playerRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                playerRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", new Color(1f, 0.85f, 0f));
                playerRenderer.SetPropertyBlock(propertyBlock);
            }
            Vector3 newScale = Vector3.one * 1.2f;
            playerRenderer.transform.localScale = newScale;
            feedbackTimer = 0.5f;
            currentFeedbackType = "Counter";
        }

        public void TriggerCounterAvailableFeedback()
        {
            if (playerRenderer == null) return;

            if (playerCounterAvailableMaterial != null)
            {
                ApplyCombinedFeedback(playerCounterAvailableMaterial, 1.1f, "CounterAvailable", 0.25f);
                return;
            }

            if (playerRenderer.sharedMaterial != null && playerRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                playerRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", new Color(0.25f, 1f, 0.55f));
                playerRenderer.SetPropertyBlock(propertyBlock);
            }
            playerRenderer.transform.localScale = playerOriginalScale * 1.1f;
            feedbackTimer = 0.25f;
            currentFeedbackType = "CounterAvailable";
        }

        public void SetEnemyTelegraphState()
        {
            if (enemyRenderer == null || enemyTelegraphMaterial == null) return;

            enemyCurrentMaterial = enemyTelegraphMaterial;
            enemyRenderer.material = enemyCurrentMaterial;
        }

        public void SetEnemyActiveState()
        {
            if (enemyRenderer == null || enemyActiveMaterial == null) return;

            enemyCurrentMaterial = enemyActiveMaterial;
            enemyRenderer.material = enemyCurrentMaterial;
        }

        public void SetEnemyRecoveryState()
        {
            if (enemyRenderer == null || enemyRecoveryMaterial == null) return;

            enemyCurrentMaterial = enemyRecoveryMaterial;
            enemyRenderer.material = enemyCurrentMaterial;
        }

        private void ApplyMaterialFeedback(Renderer renderer, Material feedbackMaterial, string feedbackType, float duration)
        {
            playerCurrentMaterial = feedbackMaterial;
            renderer.material = playerCurrentMaterial;
            feedbackTimer = duration;
            currentFeedbackType = feedbackType;
        }

        private void ApplyScaleFeedback(float targetScale, string feedbackType, float duration)
        {
            if (playerRenderer == null) return;

            Vector3 newScale = Vector3.one * targetScale;
            playerRenderer.transform.localScale = newScale;
            feedbackTimer = duration;
            currentFeedbackType = feedbackType;
        }

        private void ApplyCombinedFeedback(Material feedbackMaterial, float targetScale, string feedbackType, float duration)
        {
            if (playerRenderer == null) return;

            playerCurrentMaterial = feedbackMaterial;
            playerRenderer.material = playerCurrentMaterial;

            Vector3 newScale = Vector3.one * targetScale;
            playerRenderer.transform.localScale = newScale;

            feedbackTimer = duration;
            currentFeedbackType = feedbackType;
        }

        private void ResetFeedback()
        {
            if (playerRenderer != null)
            {
                if (playerOriginalMaterial != null)
                {
                    playerRenderer.material = playerOriginalMaterial;
                }
                else if (hadOriginalColor)
                {
                    playerRenderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor("_BaseColor", playerOriginalColor);
                    playerRenderer.SetPropertyBlock(propertyBlock);
                }
                else
                {
                    playerRenderer.SetPropertyBlock(null);
                }
                playerRenderer.transform.localScale = playerOriginalScale;
            }

            if (enemyRenderer != null && enemyOriginalMaterial != null)
            {
                enemyRenderer.material = enemyOriginalMaterial;
            }

            feedbackTimer = 0f;
            currentFeedbackType = string.Empty;
        }
    }
}
