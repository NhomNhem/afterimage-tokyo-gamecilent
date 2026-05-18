using System.Collections;
using UnityEngine;
using VContainer;
using GlassRefrain.Core;
using GlassRefrain.Enemy;

namespace GlassRefrain.Bootstrap {
    public sealed class M0EnemyIntentLoopDriver : MonoBehaviour {
        [SerializeField] private float idleDuration = 1.5f;
        [SerializeField] private float telegraphDuration = 0.75f;
        [SerializeField] private float commitDuration = 0.2f;
        [SerializeField] private float activeDuration = 0.15f;
        [SerializeField] private float recoveryDuration = 0.6f;
        [SerializeField] private float punishWindowDuration = 0.35f;
        [SerializeField] private string telegraphId = "BasicSlashTelegraph";
        [SerializeField] private string attackId = "BasicSlash";
        [SerializeField] private string attackLabel = "M0BasicSlash";

        private M0EnemyIntentModel? _model;

        [Inject]
        internal void Construct(M0EnemyIntentModel enemyIntentModel) {
            _model = enemyIntentModel;
        }

        private void Start() {
            if (_model == null) {
                Debug.LogWarning("[M0EnemyIntentLoopDriver] M0EnemyIntentModel not injected. Loop will not run.");
                return;
            }

            StartCoroutine(RunLoop());
        }

        private IEnumerator RunLoop() {
            var attackIntent = new EnemyAttackIntentContext(
                attackId,
                attackLabel,
                activeDuration,
                new EnemyAttackTagSet(new[] { "DodgePunishable", "ParryEligible", "CounterOnWhiff" })
            );

            while (true) {
                _model?.EnterIdle("LoopIdle");
                yield return new WaitForSeconds(idleDuration);

                _model?.EnterTelegraph(telegraphId, telegraphDuration, "LoopTelegraph");
                yield return new WaitForSeconds(telegraphDuration);

                _model?.EnterCommit(attackIntent, commitDuration, "LoopCommit");
                yield return new WaitForSeconds(commitDuration);

                _model?.EnterActive(activeDuration, "LoopActive");
                yield return new WaitForSeconds(activeDuration);

                _model?.EnterRecovery(recoveryDuration, "LoopRecovery", true, punishWindowDuration, "RecoveryEnd");
                yield return new WaitForSeconds(recoveryDuration);
            }
        }
    }
}
