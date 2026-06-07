using System;
using GlassRefrain.Core;
using Sirenix.OdinInspector;

namespace GlassRefrain.Tests.Others {
    #region Sample001
    public class SampleTest {
        public delegate void EnemyIntentChangedHandler(EnemyIntentSnapshot snapshot);
    }

    public sealed class M0EnemyIntentModel {
        public event SampleTest.EnemyIntentChangedHandler? SnapshotChanged;

        private void OnSnapshotChanged(EnemyIntentSnapshot snapshot) {
            var snapShot = snapshot;
            SnapshotChanged?.Invoke(snapShot);
        }
    }
    #endregion

    #region Sample002

    public class SampleTest002 {
        public delegate int DamageModifier(int rawDamage, IEnemy target);
    }

    public class CombatSystem {
        public SampleTest002.DamageModifier OnCalculateDamage;

        public void DealDamage(IEnemy enemy, int baseDamage) {
            int finalDamage = baseDamage;

            finalDamage = OnCalculateDamage.Invoke(finalDamage, enemy);

            enemy.TakeDamage(finalDamage);
        }
    }

    public interface IEnemy {
        int Health { get; set; }
        void TakeDamage(int damage);
    }

    public class TextBoxUI : SerializedMonoBehaviour {
        public Action<string>? OnSubmit;

        public void UserClickedSubmitButton(string text) => OnSubmit?.Invoke(text);
    }
    #endregion
}
