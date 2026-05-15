using System;
using System.Collections.Generic;
using System.Threading;
using NhemBootStrap.Editor.Core;
using NhemBootstrap.Editor.Config;
using NhemBootstrap.Editor.Steps;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NhemBootstrap.Editor {
    /// <summary>Main editor window for the NhemBootstrap tool.</summary>
    public class BootstrapWindow : EditorWindow {
        // ── State ─────────────────────────────────────────────────────────────
        private List<StepViewModel> _steps = new();
        private BootstrapConfig _config;
        private ConfigProfile _selectedProfile;
        private CancellationTokenSource _cts;
        private bool _isRunning;

        // ── UI Elements ───────────────────────────────────────────────────────
        private ScrollView _list;
        private ScrollView _log;
        private ProgressBar _progress;
        private Label _currentStepLabel;
        private Button _runBtn;
        private Button _cancelBtn;
        private VisualElement _statusPanel;
        private ObjectField _configField;
        private EnumField _profileDropdown;

        // ── Legacy fields (kept for backward compat) ──────────────────────────
        private string _projectName = "TinyMonsterArena";
        private bool _forceUpdateAsmdef = false;

        /// <summary>Opens the Bootstrap Window from the Tools menu.</summary>
        [MenuItem("Tools/Nhem Bootstrap/Open Window")]
        public static void Open() {
            GetWindow<BootstrapWindow>("Nhem Bootstrap");
        }

        /// <summary>Builds the UIElements-based GUI for the window.</summary>
        public void CreateGUI() {
            var root = rootVisualElement;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;

            // ── Title ──────────────────────────────────────────────────────────
            var title = new Label("🚀 Nhem Bootstrap") {
                style = {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 8
                }
            };
            root.Add(title);

            // ── System Status Panel (8.5) ──────────────────────────────────────
            _statusPanel = new VisualElement {
                style = {
                    marginBottom = 8,
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 4,
                    paddingBottom = 4,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f)
                }
            };
            root.Add(_statusPanel);
            RefreshStatusPanel();

            // ── Config Object Field (8.1) ──────────────────────────────────────
            _configField = new ObjectField("Bootstrap Config") { objectType = typeof(BootstrapConfig) };
            _configField.tooltip = "Drag-and-drop a BootstrapConfig asset to configure the bootstrap run";
            _configField.RegisterValueChangedCallback(e => {
                _config = e.newValue as BootstrapConfig;
                if (_config != null) {
                    _selectedProfile = _config.profile;
                    _profileDropdown.SetValueWithoutNotify(_selectedProfile);
                    ProfilePersistence.Save(_selectedProfile);
                }
                Init();
                Draw();
            });
            root.Add(_configField);

            // ── Profile Dropdown (8.2) ─────────────────────────────────────────
            _selectedProfile = ProfilePersistence.Load();
            _profileDropdown = new EnumField("Profile", _selectedProfile);
            _profileDropdown.tooltip = "Select a predefined configuration profile";
            _profileDropdown.RegisterValueChangedCallback(e => {
                _selectedProfile = (ConfigProfile)e.newValue;
                ProfilePersistence.Save(_selectedProfile);
                LoadProfileConfig(_selectedProfile);
            });
            root.Add(_profileDropdown);

            // ── Load Sample Config Dropdown (8.7) ─────────────────────────────
            var sampleRow = new VisualElement {
                style = { flexDirection = FlexDirection.Row, marginBottom = 4 }
            };
            var sampleLabel = new Label("Load Sample:") {
                style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft }
            };
            var sampleBtn = new Button(ShowSampleConfigMenu) { text = "▾ Sample Configs" };
            sampleRow.Add(sampleLabel);
            sampleRow.Add(sampleBtn);
            root.Add(sampleRow);

            // ── Project Name & Force Toggle ────────────────────────────────────
            var nameField = new TextField("Project Name");
            nameField.value = _projectName;
            nameField.RegisterValueChangedCallback(e => _projectName = e.newValue);
            root.Add(nameField);

            var forceToggle = new Toggle("Force Update Asmdefs");
            forceToggle.value = _forceUpdateAsmdef;
            forceToggle.RegisterValueChangedCallback(e => {
                _forceUpdateAsmdef = e.newValue;
                OnRefreshClick();
            });
            root.Add(forceToggle);

            // ── Step List ──────────────────────────────────────────────────────
            _list = new ScrollView { style = { height = 200, marginBottom = 4 } };
            root.Add(_list);

            // ── Progress Bar + Step Label (8.4) ───────────────────────────────
            _progress = new ProgressBar { style = { marginBottom = 2 } };
            root.Add(_progress);
            _currentStepLabel = new Label("") { style = { fontSize = 10, marginBottom = 4 } };
            root.Add(_currentStepLabel);

            // ── Buttons ────────────────────────────────────────────────────────
            var btnRow = new VisualElement {
                style = { flexDirection = FlexDirection.Row, marginBottom = 4 }
            };

            var refreshBtn = new Button(OnRefreshClick) { text = "Refresh", style = { flexGrow = 1 } };
            var fixAsmdefBtn = new Button(FixAsmdefs) { text = "Fix Asmdefs", style = { flexGrow = 1 } };
            _runBtn = new Button(RunAsync) { text = "Apply Selected Steps", style = { flexGrow = 2 } };

            // 8.3: Cancel button — enabled only during async execution
            _cancelBtn = new Button(CancelRun) { text = "Cancel", style = { flexGrow = 1 } };
            _cancelBtn.SetEnabled(false);

            // 8.8: Help button
            var helpBtn = new Button(OpenHelp) { text = "Help", style = { flexGrow = 1 } };

            btnRow.Add(refreshBtn);
            btnRow.Add(fixAsmdefBtn);
            btnRow.Add(_runBtn);
            btnRow.Add(_cancelBtn);
            btnRow.Add(helpBtn);
            root.Add(btnRow);

            // ── Log ────────────────────────────────────────────────────────────
            _log = new ScrollView {
                style = { height = 150, backgroundColor = new Color(0.1f, 0.1f, 0.1f) }
            };
            root.Add(_log);

            Init();
            Draw();
            Log("BootstrapWindow GUI created");
        }

        // ── Initialization ────────────────────────────────────────────────────

        private void Init() {
            if (_config != null) {
                // Config-driven: build steps from config
                _steps = new List<StepViewModel> {
                    Create(new CreateFolderStep()),
                    Create(new GenerateAsmdefStep()),
                    Create(new InstallPackageStep(_config.packages))
                };
            }
            else {
                // Fallback: use hardcoded defaults
                var packages = new List<string> {
                    "com.fishnet.fishnet",
                    "com.enemic.messagepipe",
                    "cysharp.unitask",
                    "com.vcontainer.vcontainer",
                    "com.vcontainer.vcontainer.unity",
                    "com.github.xinaoranged.zlogger",
                    "com.nmediacorp.r3",
                    "com.nmediacorp.r3.unity"
                };
                _steps = new List<StepViewModel> {
                    Create(new CreateFolderStep()),
                    Create(new GenerateAsmdefStep()),
                    Create(new InstallPackageStep(packages))
                };
            }
        }

        private StepViewModel Create(IBootstrapStep step) {
            var completed = step.CheckCompleted();
            return new StepViewModel { Step = step, Completed = completed, Enabled = !completed };
        }

        // ── Status Panel (8.5, 8.6) ───────────────────────────────────────────

        private void RefreshStatusPanel() {
            _statusPanel.Clear();
            _statusPanel.Add(new Label("System Status") {
                style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 }
            });

            try {
                // EnvironmentValidator is created in task 9; gracefully handle if not yet available.
                var validatorType = Type.GetType(
                    "NhemBootstrap.Editor.Core.EnvironmentValidator, Assembly-CSharp-Editor");
                if (validatorType != null) {
                    var resultProp = validatorType.GetProperty("LastResult");
                    var result = resultProp?.GetValue(null);
                    if (result != null) {
                        var isValidProp = result.GetType().GetProperty("IsValid");
                        var errorsProp = result.GetType().GetProperty("Errors");
                        var warningsProp = result.GetType().GetProperty("Warnings");

                        bool isValid = (bool)(isValidProp?.GetValue(result) ?? true);
                        var errors = errorsProp?.GetValue(result) as IEnumerable<string>;
                        var warnings = warningsProp?.GetValue(result) as IEnumerable<string>;

                        if (errors != null) {
                            foreach (var err in errors)
                                _statusPanel.Add(new Label($"❌ {err}") { style = { color = Color.red } });
                        }
                        if (warnings != null) {
                            foreach (var warn in warnings)
                                _statusPanel.Add(new Label($"⚠️ {warn}") { style = { color = Color.yellow } });
                        }
                        if (isValid && _statusPanel.childCount == 1)
                            _statusPanel.Add(new Label("✅ Environment OK") { style = { color = Color.green } });

                        // 8.6: disable run button on critical errors
                        if (_runBtn != null) _runBtn.SetEnabled(isValid);
                        return;
                    }
                }
            }
            catch {
                // EnvironmentValidator not yet compiled — fall through to pending message
            }

            _statusPanel.Add(new Label("✅ Environment check pending") { style = { color = Color.gray } });
        }

        // ── Profile Loading (8.2) ─────────────────────────────────────────────

        private void LoadProfileConfig(ConfigProfile profile) {
            string assetName = profile switch {
                ConfigProfile.Minimal => "MinimalBootstrapConfig",
                ConfigProfile.Full    => "FullBootstrapConfig",
                _                     => "CustomBootstrapConfig"
            };

            string[] guids = AssetDatabase.FindAssets(
                $"{assetName} t:BootstrapConfig",
                new[] { "Assets/NhemBootstrap/Sample" });

            if (guids.Length > 0) {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _config = AssetDatabase.LoadAssetAtPath<BootstrapConfig>(path);
                _configField?.SetValueWithoutNotify(_config);
                Init();
                Draw();
                Log($"Loaded profile: {profile}");
            }
            else {
                Log($"⚠️ Sample config for profile '{profile}' not found. " +
                    "Run Tools > Nhem Bootstrap > Generate Sample Configs first.");
            }
        }

        // ── Sample Config Dropdown (8.7) ──────────────────────────────────────

        private void ShowSampleConfigMenu() {
            string[] guids = AssetDatabase.FindAssets(
                "t:BootstrapConfig",
                new[] { "Assets/NhemBootstrap/Sample" });

            if (guids.Length == 0) {
                Log("No sample configs found. Run Tools > Nhem Bootstrap > Generate Sample Configs.");
                return;
            }

            var menu = new GenericMenu();
            foreach (var guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                // Capture loop variables for the closure
                string capturedPath = path;
                string capturedName = name;
                menu.AddItem(new GUIContent(capturedName), false, () => {
                    _config = AssetDatabase.LoadAssetAtPath<BootstrapConfig>(capturedPath);
                    _configField?.SetValueWithoutNotify(_config);
                    if (_config != null) {
                        _selectedProfile = _config.profile;
                        _profileDropdown?.SetValueWithoutNotify(_selectedProfile);
                    }
                    Init();
                    Draw();
                    Log($"Loaded sample config: {capturedName}");
                });
            }
            menu.ShowAsContext();
        }

        // ── Refresh & Draw ────────────────────────────────────────────────────

        private void OnRefreshClick() {
            foreach (var vm in _steps) {
                vm.Completed = vm.Step.CheckCompleted();
                vm.Enabled = !vm.Completed;

                if (vm.Step is InstallPackageStep packageStep) {
                    var installed = packageStep.GetInstalledPackages();
                    foreach (var pkg in packageStep.Packages) {
                        pkg.IsInstalled = installed != null && installed.Contains(pkg.Name);
                    }
                }
            }
            RefreshStatusPanel();
            Draw();
            Log("Status refreshed");
        }

        private void Draw() {
            _list.Clear();

            foreach (var vm in _steps) {
                var row = new VisualElement {
                    style = { flexDirection = FlexDirection.Column, marginBottom = 5 }
                };
                var header = new VisualElement {
                    style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
                };

                var toggle = new Toggle { value = !vm.Completed };
                toggle.SetEnabled(!vm.Completed);
                toggle.RegisterValueChangedCallback(e => vm.Enabled = e.newValue);
                vm.Enabled = toggle.value;

                var name = new Label(vm.Step.Name) {
                    style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Bold }
                };
                var status = new Label(vm.Completed ? "✅" : "⏳");

                header.Add(toggle);
                header.Add(name);
                header.Add(status);
                row.Add(header);

                if (vm.Step is InstallPackageStep pkgStep) {
                    var pkgList = new VisualElement {
                        style = { marginLeft = 20, marginTop = 2 }
                    };
                    foreach (var pkg in pkgStep.Packages) {
                        var pkgRow = new VisualElement {
                            style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
                        };
                        var pkgToggle = new Toggle { value = pkg.Selected };
                        pkgToggle.SetEnabled(!pkg.IsInstalled);
                        pkgToggle.RegisterValueChangedCallback(e => {
                            pkg.Selected = e.newValue;
                            vm.Completed = vm.Step.CheckCompleted();
                            status.text = vm.Completed ? "✅" : "⏳";
                        });
                        var pkgName = new Label(pkg.Name) { style = { flexGrow = 1, fontSize = 11 } };
                        var pkgVer = new Label(pkg.IsInstalled ? $"✓ {pkg.InstalledVersion}" : "") {
                            style = { fontSize = 10 }
                        };
                        pkgRow.Add(pkgToggle);
                        pkgRow.Add(pkgName);
                        pkgRow.Add(pkgVer);
                        pkgList.Add(pkgRow);
                    }
                    row.Add(pkgList);
                }

                _list.Add(row);
            }
        }

        // ── Async Run (8.3, 8.4) ──────────────────────────────────────────────

        private void RunAsync() {
            _ = RunAsyncInternal();
        }

        private async Awaitable RunAsyncInternal() {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();
            _runBtn.SetEnabled(false);
            _cancelBtn.SetEnabled(true); // 8.3: enable cancel during run

            var ctx = new BootstrapContext {
                ProjectName = _projectName,
                ForceUpdateAsmdef = _forceUpdateAsmdef,
                Config = _config,
                CancellationToken = _cts.Token,
                Progress = new Progress<BootstrapProgress>(p => {
                    _progress.value = (float)p.StepIndex / Mathf.Max(1, p.TotalSteps) * 100f;
                    _currentStepLabel.text = p.StepName;
                })
            };

            var runner = new BootstrapRunner(new List<IBootstrapStep>());

            // 8.4: wire runner events to update progress bar and step label
            runner.OnStepStarted += (stepName, index, total) => {
                _progress.value = (float)index / Mathf.Max(1, total) * 100f;
                _currentStepLabel.text = $"Running: {stepName} ({index + 1}/{total})";
                Log($"▶ {stepName}");
            };
            runner.OnStepCompleted += (stepName, success, error) => {
                Log(success ? $"✅ {stepName}" : $"❌ {stepName}: {error}");
            };
            runner.OnAllCompleted += summary => {
                _progress.value = 100f;
                _currentStepLabel.text = summary.WasCancelled ? "Cancelled" : "Complete";
                Log($"Done — {summary.SucceededSteps}/{summary.TotalSteps} succeeded, " +
                    $"{summary.FailedSteps} failed, {summary.SkippedSteps} skipped.");
                Draw();
            };

            var enabledSteps = new List<IBootstrapStep>();
            foreach (var vm in _steps) {
                if (vm.Enabled && !vm.Step.CheckCompleted())
                    enabledSteps.Add(vm.Step);
            }

            try {
                await runner.RunAsync(enabledSteps, ctx, _cts.Token);
            }
            catch (OperationCanceledException) {
                Log("⚠️ Run cancelled.");
            }
            catch (Exception ex) {
                Log($"❌ Unexpected error: {ex.Message}");
            }
            finally {
                _isRunning = false;
                _runBtn.SetEnabled(true);
                _cancelBtn.SetEnabled(false); // 8.3: disable cancel when not running
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void CancelRun() {
            _cts?.Cancel();
            Log("⚠️ Cancellation requested...");
        }

        // ── Help (8.8) ────────────────────────────────────────────────────────

        private void OpenHelp() {
            string readmePath = "Assets/NhemBootstrap/README.md";
            string fullPath = System.IO.Path.GetFullPath(readmePath);
            if (System.IO.File.Exists(fullPath)) {
                EditorUtility.RevealInFinder(fullPath);
            }
            else {
                Application.OpenURL("https://github.com/your-org/NhemBootstrap");
                Log("README.md not found — opening GitHub page.");
            }
        }

        // ── Legacy Methods ────────────────────────────────────────────────────

        private void FixAsmdefs() {
            var ctx = new BootstrapContext {
                ProjectName = _projectName,
                ForceUpdateAsmdef = true,
                Config = _config
            };
            Log("Force updating Asmdefs...");
            var step = new GenerateAsmdefStep();
            step.Execute(ctx);
            Log("Asmdefs updated.");
            OnRefreshClick();
        }

        private void Log(string msg) {
            _log?.Add(new Label(msg));
            Debug.Log(msg);
        }
    }
}
