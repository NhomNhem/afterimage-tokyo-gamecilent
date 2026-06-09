/*using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Sirenix.OdinInspector.Editor;

namespace GlassRefrain.Bootstrap.Editor {
    [CustomEditor(typeof(GameplayLifetimeScope))]
    public class GameplayLifetimeScopeEditor : OdinEditor {
        private bool _isVContainerFieldsGenerated;

        public override VisualElement CreateInspectorGUI() {
            var rootElement = new VisualElement();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/Code/Bootstrap/Editor/GameplayLifetimeScopeEditor.uxml");
            visualTree.CloneTree(rootElement);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/_Project/Code/Bootstrap/Editor/GameplayLifetimeScopeEditor.uss");
            rootElement.styleSheets.Add(styleSheet);

            var vcontainerFoldout = rootElement.Q<Foldout>("vcontainer-foldout");
            if (vcontainerFoldout != null) {
                vcontainerFoldout.RegisterValueChangedCallback(evt => {
                    if (evt.newValue && !_isVContainerFieldsGenerated) {
                        GenerateVContainerFields(rootElement, vcontainerFoldout);
                        rootElement.Bind(serializedObject);
                    }
                });
            }

            rootElement.Bind(serializedObject);
            return rootElement;
        }

        private void GenerateVContainerFields(VisualElement root, Foldout foldout) {
            _isVContainerFieldsGenerated = true;

            // Lấy danh sách các thuộc tính đã được bind thủ công trong UXML để loại trừ
            var customBoundFields = root.Query<PropertyField>().ToList();
            var excludedProperties = new HashSet<string>();
            foreach (var field in customBoundFields) {
                if (!string.IsNullOrEmpty(field.bindingPath)) {
                    excludedProperties.Add(field.bindingPath);
                }
            }

            excludedProperties.Add("m_Script");
            excludedProperties.Add("serializationData"); // Loại trừ cả data nhị phân ẩn của Odin luôn

            // THAY ĐỔI Ở ĐÂY: Dùng IMGUIContainer để nhúng trình vẽ của Odin vào UI Toolkit Foldout
            var odinInspectorContainer = new IMGUIContainer(() => {
                // Gọi hàm vẽ mặc định của OdinEditor, nó sẽ tự quét [OdinSerialize] và vẽ bằng IMGUI
                // Đồng thời Odin cũng tự động loại trừ các field vẽ bằng thuộc tính [TabGroup] nếu cần,
                // Hoặc bạn có thể dùng DrawPropertiesTree() của Odin để tùy biến sâu hơn.
                this.DrawTree();
            });

            foldout.Add(odinInspectorContainer);
        }
    }
}*/
