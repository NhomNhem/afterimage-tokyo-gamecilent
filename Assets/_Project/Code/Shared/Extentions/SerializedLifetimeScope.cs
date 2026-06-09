using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using VContainer.Unity;

namespace GlassRefrain.Code.Shared.Extentions {
    [ShowOdinSerializedPropertiesInInspector]
    [HideMonoScript]
    public abstract class SerializedLifetimeScope : LifetimeScope, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private SerializationData serializationData;

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            this.OnBeforeSerialize();
            UnitySerializationUtility.SerializeUnityObject(this, ref this.serializationData);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            UnitySerializationUtility.DeserializeUnityObject(this, ref this.serializationData);
            this.OnAfterDeserialize();
        }

        protected virtual void OnBeforeSerialize() { }
        protected virtual void OnAfterDeserialize() { }
    }
}
