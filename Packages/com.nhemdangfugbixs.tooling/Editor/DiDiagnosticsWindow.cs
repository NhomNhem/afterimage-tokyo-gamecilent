using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NhemDangFugBixs.Editor {
    public sealed class DiDiagnosticsWindow : EditorWindow {
        private readonly List<ReportSnapshot> _reports = new();
        private Vector2 _scopeScroll;
        private Vector2 _detailsScroll;
        private int _selectedReportIndex;
        private string? _selectedScope;

        [MenuItem("Window/Nhem/DI Diagnostics")]
        public static void OpenWindow() {
            var window = GetWindow<DiDiagnosticsWindow>("DI Diagnostics");
            window.minSize = new Vector2(900f, 520f);
            window.Refresh();
        }

        private void OnEnable() {
            Refresh();
        }

        private void OnGUI() {
            DrawToolbar();

            if (_reports.Count == 0) {
                EditorGUILayout.HelpBox("No generated RegistrationReport types were found in the current AppDomain. Build or refresh generation, then reopen this window.", MessageType.Info);
                return;
            }

            _selectedReportIndex = Mathf.Clamp(_selectedReportIndex, 0, _reports.Count - 1);
            var report = _reports[_selectedReportIndex];
            var scopes = report.Entries.Select(entry => entry.Scope).Distinct(StringComparer.Ordinal).OrderBy(scope => scope, StringComparer.Ordinal).ToList();
            if (_selectedScope == null || !scopes.Contains(_selectedScope, StringComparer.Ordinal)) {
                _selectedScope = scopes.FirstOrDefault();
            }

            using (new EditorGUILayout.HorizontalScope()) {
                DrawScopePanel(report, scopes);
                DrawDetailsPanel(report);
            }
        }

        private void DrawToolbar() {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton)) {
                    Refresh();
                }

                EditorGUI.BeginDisabledGroup(_reports.Count == 0);
                if (GUILayout.Button("Run Preflight", EditorStyles.toolbarButton)) {
                    RunPreflight();
                }

                if (GUILayout.Button("Generate Report", EditorStyles.toolbarButton)) {
                    ExportSelectedReport();
                }

                if (GUILayout.Button("Open Config", EditorStyles.toolbarButton)) {
                    OpenConfig();
                }

                if (GUILayout.Button("Open Generated File", EditorStyles.toolbarButton)) {
                    OpenGeneratedOutputLocation();
                }

                GUILayout.FlexibleSpace();
                _selectedReportIndex = EditorGUILayout.Popup(
                    _selectedReportIndex,
                    _reports.Select(report => report.DisplayName).ToArray(),
                    EditorStyles.toolbarPopup,
                    GUILayout.Width(280f));
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawScopePanel(ReportSnapshot report, List<string> scopes) {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260f))) {
                EditorGUILayout.LabelField("Scopes", EditorStyles.boldLabel);
                _scopeScroll = EditorGUILayout.BeginScrollView(_scopeScroll);
                foreach (var scope in scopes) {
                    var services = report.Entries.Count(entry => string.Equals(entry.Scope, scope, StringComparison.Ordinal));
                    var label = $"{scope} ({services})";
                    var selected = string.Equals(_selectedScope, scope, StringComparison.Ordinal);
                    if (GUILayout.Toggle(selected, label, "Button")) {
                        _selectedScope = scope;
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawDetailsPanel(ReportSnapshot report) {
            using (new EditorGUILayout.VerticalScope()) {
                EditorGUILayout.LabelField("Scope Details", EditorStyles.boldLabel);
                if (string.IsNullOrWhiteSpace(_selectedScope)) {
                    EditorGUILayout.HelpBox("Select a scope to inspect its registrations.", MessageType.Info);
                    return;
                }

                var entries = report.Entries
                    .Where(entry => string.Equals(entry.Scope, _selectedScope, StringComparison.Ordinal))
                    .OrderBy(entry => entry.Service, StringComparer.Ordinal)
                    .ToList();
                var mappings = report.ScopeMappings
                    .Where(mapping => string.Equals(mapping.Scope, _selectedScope, StringComparison.Ordinal))
                    .ToList();
                var consumers = report.Consumers
                    .Where(consumer => string.Equals(consumer.Scope, _selectedScope, StringComparison.Ordinal))
                    .OrderBy(consumer => consumer.Service, StringComparer.Ordinal)
                    .ToList();
                var loggerRoots = report.LoggerRoots
                    .Where(root => string.Equals(root.Scope, _selectedScope, StringComparison.Ordinal))
                    .ToList();
                var loggerConsumers = report.LoggerConsumers
                    .Where(consumer => string.Equals(consumer.Scope, _selectedScope, StringComparison.Ordinal))
                    .ToList();

                _detailsScroll = EditorGUILayout.BeginScrollView(_detailsScroll);

                EditorGUILayout.LabelField("Scope Mappings", EditorStyles.miniBoldLabel);
                if (mappings.Count == 0) {
                    EditorGUILayout.HelpBox("No explicit marker-to-scope mappings were emitted for this scope.", MessageType.None);
                } else {
                    foreach (var mapping in mappings) {
                        var aliasSuffix = string.IsNullOrWhiteSpace(mapping.Alias) ? string.Empty : $" (alias: {mapping.Alias})";
                        EditorGUILayout.LabelField($"{GetSimpleName(mapping.Marker)} -> {GetSimpleName(mapping.Scope)}{aliasSuffix}");
                    }
                }

                EditorGUILayout.Space(10f);

                EditorGUILayout.LabelField("Services", EditorStyles.miniBoldLabel);
                foreach (var entry in entries) {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(GetSimpleName(entry.Service), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Lifetime", entry.Lifetime);
                    EditorGUILayout.LabelField("Kind", entry.Kind);
                    if (!string.IsNullOrWhiteSpace(entry.MessageType)) {
                        EditorGUILayout.LabelField("Message Type", GetSimpleName(entry.MessageType));
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("MessagePipe Events", EditorStyles.miniBoldLabel);
                if (consumers.Count == 0) {
                    EditorGUILayout.HelpBox("No MessagePipe consumer metadata recorded for this scope.", MessageType.None);
                } else {
                    foreach (var consumer in consumers) {
                        EditorGUILayout.LabelField($"{GetSimpleName(consumer.Service)} -> {consumer.Role}<{GetSimpleName(consumer.MessageType)}>");
                    }
                }

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Diagnostics Summary", EditorStyles.miniBoldLabel);
                if (loggerRoots.Count == 0 && loggerConsumers.Count == 0) {
                    EditorGUILayout.HelpBox("No logger/root diagnostics metadata recorded for this scope.", MessageType.None);
                } else {
                    foreach (var root in loggerRoots) {
                        EditorGUILayout.LabelField($"Logger Root: factory={root.HasLoggerFactory}, adapter={root.HasLoggerAdapter}");
                    }

                    foreach (var loggerConsumer in loggerConsumers) {
                        EditorGUILayout.LabelField($"Logger Consumer: {GetSimpleName(loggerConsumer.Service)} -> {GetSimpleName(loggerConsumer.CategoryType)}");
                    }
                }

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Source", EditorStyles.miniBoldLabel);
                EditorGUILayout.SelectableLabel(report.AssemblyLocation, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                EditorGUILayout.EndScrollView();
            }
        }

        private void Refresh() {
            _reports.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(assembly => assembly.GetName().Name, StringComparer.Ordinal)) {
                var reportType = assembly.GetTypes().FirstOrDefault(type => type.IsClass && type.IsAbstract && type.IsSealed && type.Name == "RegistrationReport");
                if (reportType == null) {
                    continue;
                }

                _reports.Add(ReportSnapshot.From(reportType));
            }

            _selectedReportIndex = Mathf.Clamp(_selectedReportIndex, 0, Mathf.Max(_reports.Count - 1, 0));
            Repaint();
        }

        private void RunPreflight() {
            var packageRoot = ResolvePackageRoot();
            var cliProject = Path.Combine(packageRoot, "Source~", "DangFugBixs.Tools~", "DangFugBixs.Cli", "DangFugBixs.Cli.csproj");
            if (!File.Exists(cliProject)) {
                UnityEngine.Debug.LogWarning("CLI project was not found. Unable to run preflight from the editor window.");
                return;
            }

            var assemblyLocation = _reports.Count > 0 ? _reports[_selectedReportIndex].AssemblyLocation : null;
            if (string.IsNullOrWhiteSpace(assemblyLocation) || !File.Exists(assemblyLocation)) {
                UnityEngine.Debug.LogWarning("No generated assembly is available for validation.");
                return;
            }

            StartProcess("dotnet", $"run --project \"{cliProject}\" -- validate \"{assemblyLocation}\"", packageRoot);
        }

        private void ExportSelectedReport() {
            if (_reports.Count == 0) {
                return;
            }

            var report = _reports[_selectedReportIndex];
            var path = EditorUtility.SaveFilePanel("Export DI Report", ResolvePackageRoot(), $"{report.DisplayName}.md", "md");
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }

            File.WriteAllText(path, report.Markdown);
            EditorUtility.RevealInFinder(path);
        }

        private void OpenConfig() {
            var configPath = Path.Combine(ResolvePackageRoot(), ".nhem-di.json");
            if (File.Exists(configPath)) {
                EditorUtility.RevealInFinder(configPath);
                return;
            }

            EditorUtility.DisplayDialog("Config Not Found", "No .nhem-di.json file exists at the detected package root.", "OK");
        }

        private void OpenGeneratedOutputLocation() {
            if (_reports.Count == 0) {
                return;
            }

            var location = _reports[_selectedReportIndex].AssemblyLocation;
            if (File.Exists(location)) {
                EditorUtility.RevealInFinder(location);
                return;
            }

            EditorUtility.DisplayDialog("Generated Output Not Found", "The selected report assembly location is not available on disk.", "OK");
        }

        private static void StartProcess(string fileName, string arguments, string workingDirectory) {
            try {
                using var process = Process.Start(new ProcessStartInfo {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                });

                if (process == null) {
                    UnityEngine.Debug.LogWarning("Failed to start editor tooling process.");
                    return;
                }

                process.OutputDataReceived += (_, args) => {
                    if (!string.IsNullOrWhiteSpace(args.Data)) {
                        UnityEngine.Debug.Log(args.Data);
                    }
                };
                process.ErrorDataReceived += (_, args) => {
                    if (!string.IsNullOrWhiteSpace(args.Data)) {
                        UnityEngine.Debug.LogError(args.Data);
                    }
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            } catch (Exception ex) {
                UnityEngine.Debug.LogError($"Failed to start editor tooling process: {ex.Message}");
            }
        }

        private static string ResolvePackageRoot() {
            var asmdefGuid = AssetDatabase.FindAssets("NhemDangFugBixs.Editor t:asmdef").FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(asmdefGuid)) {
                var asmdefPath = AssetDatabase.GUIDToAssetPath(asmdefGuid);
                var editorDirectory = Path.GetDirectoryName(asmdefPath);
                if (!string.IsNullOrWhiteSpace(editorDirectory)) {
                    return Path.GetFullPath(Path.Combine(editorDirectory, ".."));
                }
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetSimpleName(string value) {
            return string.IsNullOrWhiteSpace(value) || !value.Contains('.')
                ? value
                : value.Split('.').Last();
        }

        private sealed record ReportEntry(string Scope, string Service, string Lifetime, string Kind, string MessageType);
        private sealed record ScopeMapping(string Marker, string Scope, string Alias);
        private sealed record ReportConsumer(string Scope, string Service, string Role, string MessageType);
        private sealed record LoggerRoot(string Scope, bool HasLoggerFactory, bool HasLoggerAdapter);
        private sealed record LoggerConsumer(string Scope, string Service, string CategoryType);

        private sealed class ReportSnapshot {
            public string DisplayName { get; }
            public string AssemblyLocation { get; }
            public string Markdown { get; }
            public IReadOnlyList<ScopeMapping> ScopeMappings { get; }
            public IReadOnlyList<ReportEntry> Entries { get; }
            public IReadOnlyList<ReportConsumer> Consumers { get; }
            public IReadOnlyList<LoggerRoot> LoggerRoots { get; }
            public IReadOnlyList<LoggerConsumer> LoggerConsumers { get; }

            private ReportSnapshot(
                string displayName,
                string assemblyLocation,
                string markdown,
                IReadOnlyList<ScopeMapping> scopeMappings,
                IReadOnlyList<ReportEntry> entries,
                IReadOnlyList<ReportConsumer> consumers,
                IReadOnlyList<LoggerRoot> loggerRoots,
                IReadOnlyList<LoggerConsumer> loggerConsumers) {
                DisplayName = displayName;
                AssemblyLocation = assemblyLocation;
                Markdown = markdown;
                ScopeMappings = scopeMappings;
                Entries = entries;
                Consumers = consumers;
                LoggerRoots = loggerRoots;
                LoggerConsumers = loggerConsumers;
            }

            public static ReportSnapshot From(Type reportType) {
                var assemblyName = reportType.Assembly.GetName().Name ?? reportType.Assembly.FullName ?? "UnknownAssembly";
                var assemblyLocation = reportType.Assembly.Location;
                var markdown = ReadStringField(reportType, "Markdown") ?? string.Empty;
                var scopeMappings = (ReadStringArrayField(reportType, "ScopeMappings") ?? Array.Empty<string>())
                    .Select(ParseScopeMapping)
                    .ToList();
                var entries = (ReadStringArrayField(reportType, "Entries") ?? Array.Empty<string>())
                    .Select(ParseEntry)
                    .ToList();
                var consumers = (ReadStringArrayField(reportType, "Consumers") ?? Array.Empty<string>())
                    .Select(ParseConsumer)
                    .ToList();
                var loggerRoots = (ReadStringArrayField(reportType, "LoggerRoots") ?? Array.Empty<string>())
                    .Select(ParseLoggerRoot)
                    .ToList();
                var loggerConsumers = (ReadStringArrayField(reportType, "LoggerConsumers") ?? Array.Empty<string>())
                    .Select(ParseLoggerConsumer)
                    .ToList();

                return new ReportSnapshot(assemblyName, assemblyLocation, markdown, scopeMappings, entries, consumers, loggerRoots, loggerConsumers);
            }

            private static string[]? ReadStringArrayField(Type reportType, string fieldName) {
                return reportType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string[];
            }

            private static string? ReadStringField(Type reportType, string fieldName) {
                return reportType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string;
            }

            private static ReportEntry ParseEntry(string value) {
                var parts = value.Split('|');
                return new ReportEntry(
                    parts.ElementAtOrDefault(0) ?? string.Empty,
                    parts.ElementAtOrDefault(1) ?? string.Empty,
                    parts.ElementAtOrDefault(2) ?? string.Empty,
                    parts.ElementAtOrDefault(3) ?? string.Empty,
                    parts.ElementAtOrDefault(4) ?? string.Empty);
            }

            private static ScopeMapping ParseScopeMapping(string value) {
                var parts = value.Split('|');
                return new ScopeMapping(
                    parts.ElementAtOrDefault(0) ?? string.Empty,
                    parts.ElementAtOrDefault(1) ?? string.Empty,
                    parts.ElementAtOrDefault(2) ?? string.Empty);
            }

            private static ReportConsumer ParseConsumer(string value) {
                var parts = value.Split('|');
                return new ReportConsumer(
                    parts.ElementAtOrDefault(0) ?? string.Empty,
                    parts.ElementAtOrDefault(1) ?? string.Empty,
                    parts.ElementAtOrDefault(2) ?? string.Empty,
                    parts.ElementAtOrDefault(3) ?? string.Empty);
            }

            private static LoggerRoot ParseLoggerRoot(string value) {
                var parts = value.Split('|');
                return new LoggerRoot(
                    parts.ElementAtOrDefault(0) ?? string.Empty,
                    bool.TryParse(parts.ElementAtOrDefault(1), out var hasLoggerFactory) && hasLoggerFactory,
                    bool.TryParse(parts.ElementAtOrDefault(2), out var hasLoggerAdapter) && hasLoggerAdapter);
            }

            private static LoggerConsumer ParseLoggerConsumer(string value) {
                var parts = value.Split('|');
                return new LoggerConsumer(
                    parts.ElementAtOrDefault(0) ?? string.Empty,
                    parts.ElementAtOrDefault(1) ?? string.Empty,
                    parts.ElementAtOrDefault(2) ?? string.Empty);
            }
        }
    }
}

namespace System.Runtime.CompilerServices {
    internal static class IsExternalInit {}
}
