using UnityEngine;

namespace GlassRefrain.Presentation
{
    public class M0CombatVisualFeedbackAdapter : MonoBehaviour
    {
        [Header("Renderers")]
        public Renderer playerRenderer;
        public Renderer enemyRenderer;

        [Header("Feedback Materials")]
        public Material playerOriginalMaterial;
        public Material playerLightAttackMaterial;
        public Material playerHeavyAttackMaterial;
        public Material playerParryMaterial;
        public Material playerCounterMaterial;

        public Material enemyOriginalMaterial;
        public Material enemyTelegraphMaterial;
        public Material enemyActiveMaterial;
        public Material enemyRecoveryMaterial;

        private Material playerCurrentMaterial;
        private Material enemyCurrentMaterial;
        private Vector3 playerOriginalScale;
        private float feedbackTimer;
        private string currentFeedbackType;

        private void Awake()
        {
            playerCurrentMaterial = playerOriginalMaterial;
            enemyCurrentMaterial = enemyOriginalMaterial;
            playerOriginalScale = playerRenderer != null ? playerRenderer.transform.localScale : Vector3.one;
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
            if (playerRenderer == null || playerParryMaterial == null) return;
            
            ApplyMaterialFeedback(playerRenderer, playerParryMaterial, "Parry", 0.2f);
        }

        public void TriggerDodgeFeedback()
        {
            if (playerRenderer == null) return;
            
            ApplyScaleFeedback(0.9f, "Dodge", 0.3f);
        }

        public void TriggerCounterFeedback()
        {
            if (playerRenderer == null || playerCounterMaterial == null) return;
            
            ApplyCombinedFeedback(playerCounterMaterial, 1.2f, "Counter", 0.5f);
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
