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

        private Material _playerCurrentMaterial;
        private Material _enemyCurrentMaterial;
        private Vector3 _playerOriginalScale;
        private Color _playerOriginalColor;
        private bool _hadOriginalColor;
        private MaterialPropertyBlock _propertyBlock;
        private float _feedbackTimer;
        private string _currentFeedbackType;

        private void Awake()
        {
            _playerCurrentMaterial = playerOriginalMaterial;
            _enemyCurrentMaterial = enemyOriginalMaterial;
            _playerOriginalScale = playerRenderer != null ? playerRenderer.transform.localScale : Vector3.one;
            _propertyBlock = new MaterialPropertyBlock();
            if (playerRenderer != null && playerRenderer.sharedMaterial != null && playerRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                _playerOriginalColor = playerRenderer.sharedMaterial.GetColor("_BaseColor");
                _hadOriginalColor = true;
            }
        }

        private void Update()
        {
            if (_feedbackTimer > 0f)
            {
                _feedbackTimer -= Time.deltaTime;
                if (_feedbackTimer <= 0f)
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
                playerRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", new Color(0f, 0.8f, 1f));
                playerRenderer.SetPropertyBlock(_propertyBlock);
            }
            playerRenderer.transform.localScale = _playerOriginalScale * 1.06f;
            _feedbackTimer = 0.2f;
            _currentFeedbackType = "Parry";
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
                playerRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", new Color(1f, 0.85f, 0f));
                playerRenderer.SetPropertyBlock(_propertyBlock);
            }
            Vector3 newScale = Vector3.one * 1.2f;
            playerRenderer.transform.localScale = newScale;
            _feedbackTimer = 0.5f;
            _currentFeedbackType = "Counter";
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
                playerRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", new Color(0.25f, 1f, 0.55f));
                playerRenderer.SetPropertyBlock(_propertyBlock);
            }
            playerRenderer.transform.localScale = _playerOriginalScale * 1.1f;
            _feedbackTimer = 0.25f;
            _currentFeedbackType = "CounterAvailable";
        }

        public void SetEnemyTelegraphState()
        {
            if (enemyRenderer == null) return;

            if (enemyTelegraphMaterial != null)
            {
                _enemyCurrentMaterial = enemyTelegraphMaterial;
                enemyRenderer.material = _enemyCurrentMaterial;
                return;
            }

            if (enemyRenderer.sharedMaterial != null && enemyRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                enemyRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", new Color(1f, 0.4f, 0f));
                enemyRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        public void SetEnemyActiveState()
        {
            if (enemyRenderer == null) return;

            if (enemyActiveMaterial != null)
            {
                _enemyCurrentMaterial = enemyActiveMaterial;
                enemyRenderer.material = _enemyCurrentMaterial;
                return;
            }

            if (enemyRenderer.sharedMaterial != null && enemyRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                enemyRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", new Color(1f, 0f, 0f));
                enemyRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        public void SetEnemyRecoveryState()
        {
            if (enemyRenderer == null) return;

            if (enemyRecoveryMaterial != null)
            {
                _enemyCurrentMaterial = enemyRecoveryMaterial;
                enemyRenderer.material = _enemyCurrentMaterial;
                return;
            }

            if (enemyRenderer.sharedMaterial != null && enemyRenderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                enemyRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f));
                enemyRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        public void ResetEnemyState()
        {
            if (enemyRenderer == null) return;

            if (enemyOriginalMaterial != null)
            {
                enemyRenderer.material = enemyOriginalMaterial;
                return;
            }

            enemyRenderer.SetPropertyBlock(null);
        }

        private void ApplyMaterialFeedback(Renderer renderer, Material feedbackMaterial, string feedbackType, float duration)
        {
            _playerCurrentMaterial = feedbackMaterial;
            renderer.material = _playerCurrentMaterial;
            _feedbackTimer = duration;
            _currentFeedbackType = feedbackType;
        }

        private void ApplyScaleFeedback(float targetScale, string feedbackType, float duration)
        {
            if (playerRenderer == null) return;

            Vector3 newScale = Vector3.one * targetScale;
            playerRenderer.transform.localScale = newScale;
            _feedbackTimer = duration;
            _currentFeedbackType = feedbackType;
        }

        private void ApplyCombinedFeedback(Material feedbackMaterial, float targetScale, string feedbackType, float duration)
        {
            if (playerRenderer == null) return;

            _playerCurrentMaterial = feedbackMaterial;
            playerRenderer.material = _playerCurrentMaterial;

            Vector3 newScale = Vector3.one * targetScale;
            playerRenderer.transform.localScale = newScale;

            _feedbackTimer = duration;
            _currentFeedbackType = feedbackType;
        }

        private void ResetFeedback()
        {
            if (playerRenderer != null)
            {
                if (playerOriginalMaterial != null)
                {
                    playerRenderer.material = playerOriginalMaterial;
                }
                else if (_hadOriginalColor)
                {
                    playerRenderer.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetColor("_BaseColor", _playerOriginalColor);
                    playerRenderer.SetPropertyBlock(_propertyBlock);
                }
                else
                {
                    playerRenderer.SetPropertyBlock(null);
                }
                playerRenderer.transform.localScale = _playerOriginalScale;
            }

            _feedbackTimer = 0f;
            _currentFeedbackType = string.Empty;
        }
    }
}
