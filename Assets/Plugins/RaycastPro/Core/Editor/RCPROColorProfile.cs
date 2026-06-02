using UnityEngine;

#if UNITY_EDITOR
namespace RaycastPro.Editor
{
    [CreateAssetMenu(fileName = "RCProColorProfile", menuName = "RCPRO/ColorProfile", order = 1)]
    public class RCPROColorProfile : ScriptableObject
    {
        [SerializeField] private bool initialized;

        public Color DefaultColor;
        public Color DetectColor = new Color(.3f, 1, .3f, 1f);
        public Color HelperColor = new Color(1f, .7f, .0f, 1f);
        public Color BlockColor = new Color(1f, .2f, .2f, 1f);

        private void OnEnable()
        {
            if (initialized)
            {
                return;
            }

            // ScriptableObject field initializers/constructors must not touch EditorGUIUtility.
            DefaultColor = RCProEditor.Aqua;
            initialized = true;
        }
    }
}
#endif
