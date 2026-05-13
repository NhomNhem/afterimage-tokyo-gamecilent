using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace NhemDangFugBixs.Editor
{
    [InitializeOnLoad]
    public static class SceneInjectedProcessor
    {
        [MenuItem("Tools/NhemDangFugBixs/Force Sync Scene Injection")]
        public static void ForceSync()
        {
            ProcessScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void ProcessScene(UnityEngine.SceneManagement.Scene scene)
        {
            var lifetimeScope = UnityEngine.Object
                .FindObjectsByType<LifetimeScope>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(s => s.gameObject.scene == scene);

            if (lifetimeScope == null) return;

            // Use reflection to find the generated blueprint (fixing CS0234)
            var blueprintClass = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("NhemDangFugBixs.Generated.SceneInjectionBlueprint"))
                .FirstOrDefault(t => t != null);

            if (blueprintClass == null)
            {
                Debug.LogWarning(
                    "[AutoInject] SceneInjectionBlueprint not found in assemblies. Make sure the generator is working.");
                return;
            }

            var componentTypesField = blueprintClass.GetField("ComponentTypes",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var blueprintTypes = componentTypesField?.GetValue(null) as Type[];

            if (blueprintTypes == null || blueprintTypes.Length == 0)
            {
                Debug.Log("[AutoInject] No component types found in SceneInjectionBlueprint.");
                return;
            }

            Debug.Log(
                $"[AutoInject] Found {blueprintTypes.Length} types in blueprint: {string.Join(", ", blueprintTypes.Select(t => t.Name))}");

            bool changed = false;
            var autoInjectListField = typeof(LifetimeScope).GetField("autoInjectGameObjects",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            var autoInjectList =
                autoInjectListField?.GetValue(lifetimeScope) as System.Collections.Generic.List<GameObject>;

            if (autoInjectList == null)
            {
                Debug.LogError(
                    "[AutoInject] Could not find 'autoInjectGameObjects' field on LifetimeScope via reflection.");
                return;
            }

            // 1. Remove missing or invalid references
            int removedCount = autoInjectList.RemoveAll(go => go == null);
            if (removedCount > 0)
            {
                changed = true;
                Debug.Log($"[AutoInject] Removed {removedCount} null references from {lifetimeScope.name}");
            }

            // 2. Find and add components marked with [AutoInjectScene]
            foreach (var type in blueprintTypes)
            {
                var foundObjects = UnityEngine.Object
                    .FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Cast<Component>()
                    .Where(c => c.gameObject.scene == scene)
                    .Select(c => c.gameObject)
                    .Distinct()
                    .ToList();

                Debug.Log($"[AutoInject] Type {type.Name}: Found {foundObjects.Count} objects in scene.");

                foreach (var go in foundObjects)
                {
                    if (!autoInjectList.Contains(go))
                    {
                        autoInjectList.Add(go);
                        changed = true;
                        Debug.Log($"[AutoInject] Added {go.name} to {lifetimeScope.name}");
                    }
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(lifetimeScope);
                Debug.Log(
                    $"[AutoInject] Successfully synced injection for {lifetimeScope.name} in scene {scene.name}.");
            }
        }
    }
}
