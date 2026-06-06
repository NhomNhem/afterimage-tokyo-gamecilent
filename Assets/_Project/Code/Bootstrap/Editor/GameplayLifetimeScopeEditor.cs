using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GlassRefrain.Bootstrap.Editor
{
    [CustomEditor(typeof(GameplayLifetimeScope))]
    public class GameplayLifetimeScopeEditor : UnityEditor.Editor
    {
        private bool _isVContainerFieldsGenerated;

        public override VisualElement CreateInspectorGUI()
        {
            var rootElement = new VisualElement();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/Code/Bootstrap/Editor/GameplayLifetimeScopeEditor.uxml");
            visualTree.CloneTree(rootElement);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/_Project/Code/Bootstrap/Editor/GameplayLifetimeScopeEditor.uss");
            rootElement.styleSheets.Add(styleSheet);

            var vcontainerFoldout = rootElement.Q<Foldout>("vcontainer-foldout");
            if (vcontainerFoldout != null)
            {
                vcontainerFoldout.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue && !_isVContainerFieldsGenerated)
                    {
                        GenerateVContainerFields(rootElement, vcontainerFoldout);
                        rootElement.Bind(serializedObject);
                    }
                });
            }

            rootElement.Bind(serializedObject);
            return rootElement;
        }

        private void GenerateVContainerFields(VisualElement root, Foldout foldout)
        {
            _isVContainerFieldsGenerated = true;

            var customBoundFields = root.Query<PropertyField>().ToList();
            var excludedProperties = new HashSet<string>();
            foreach (var field in customBoundFields)
            {
                if (!string.IsNullOrEmpty(field.bindingPath))
                {
                    excludedProperties.Add(field.bindingPath);
                }
            }
            excludedProperties.Add("m_Script");

            var serializedProp = serializedObject.GetIterator();
            if (serializedProp.NextVisible(true))
            {
                do
                {
                    if (!excludedProperties.Contains(serializedProp.name))
                    {
                        var propField = new PropertyField(serializedProp.Copy());
                        foldout.Add(propField);
                    }
                }
                while (serializedProp.NextVisible(false));
            }
        }
    }
}
