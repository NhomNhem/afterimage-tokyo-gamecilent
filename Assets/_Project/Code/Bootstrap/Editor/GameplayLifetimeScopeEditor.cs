using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GlassRefrain.Bootstrap.Editor
{
    [CustomEditor(typeof(GameplayLifetimeScope))]
    public class GameplayLifetimeScopeEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var rootElement = new VisualElement();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/Code/Bootstrap/Editor/GameplayLifetimeScopeEditor.uxml");
            visualTree.CloneTree(rootElement);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/_Project/Code/Bootstrap/Editor/GameplayLifetimeScopeEditor.uss");
            rootElement.styleSheets.Add(styleSheet);

            rootElement.Bind(serializedObject);
            return rootElement;
        }
    }
}
