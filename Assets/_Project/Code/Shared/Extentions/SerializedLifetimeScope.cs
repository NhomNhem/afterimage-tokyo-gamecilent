using Sirenix.Serialization;
using UnityEngine;
using VContainer.Unity;
using Sirenix.OdinInspector;

namespace GlassRefrain.Code.Shared.Extentions {
    public class SerializedLifetimeScope : LifetimeScope, ISerializationCallbackReceiver {
        [SerializeField, HideInInspector]
        private SerializationData _serializationData;

        SerializationData ISupportSerializationCallbackReceiver.SerializationData
        {
            get => this.serializationData;
            set => this.serializationData = value;
        }

        public void OnBeforeSerialize() {
        }
        public void OnAfterDeserialize() {
        }
    }
}
