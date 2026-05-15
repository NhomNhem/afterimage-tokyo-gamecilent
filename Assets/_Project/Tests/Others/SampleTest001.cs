using GlassRefrain.Core;

namespace _Project.Tests.Others {
    public class SampleTest001 {
        public delegate void EnemyIntentChangedHandler(EnemyIntentSnapshot snapshot);
    }
    
    public sealed class M0EnemyIntentModel {
        public event SampleTest001.EnemyIntentChangedHandler SnapshotChanged;


        private void OnSnapshotChanged(EnemyIntentSnapshot snapshot) {
            SnapshotChanged?.Invoke(snapshot);
        }
        
    }
}