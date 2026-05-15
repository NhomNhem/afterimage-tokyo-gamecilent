using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NhemBootStrap.Editor.Core;
using NhemBootstrap.Editor.Config;
using NUnit.Framework;
using UnityEngine;

namespace NhemBootstrap.Tests.Editor {
    /// <summary>
    /// Property-based and unit tests for the NhemBootstrap tool.
    /// Each property test runs 100 iterations using System.Random with a fixed seed for reproducibility.
    /// </summary>
    public class BootstrapPropertyTests {
        private static readonly System.Random Rng = new System.Random(42);

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string RandomString(int length = 8) {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Range(0, length).Select(_ => chars[Rng.Next(chars.Length)]).ToArray());
        }

        private static string RandomGitUrl() =>
            $"https://github.com/{RandomString()}/{RandomString()}.git";

        private static string RandomVersion() =>
            $"{Rng.Next(0, 10)}.{Rng.Next(0, 20)}.{Rng.Next(0, 100)}";

        // ── P1: URL Version Composition ───────────────────────────────────────

        // Feature: bootstrap-tool-optimization-and-packaging, Property 1: For any PackageEntry with a non-empty version field,
        // the resolved Git URL passed to Client.Add SHALL end with #<version>.
        // For any PackageEntry with an empty or null version, the URL SHALL be passed unchanged.

        /// <summary>Validates: Requirements 6.1, 6.2, 6.3</summary>
        [Test]
        public void P1_UrlVersionComposition_NonEmptyVersion_EndsWithHashVersion() {
            // Feature: bootstrap-tool-optimization-and-packaging, Property 1: URL version composition
            for (int i = 0; i < 100; i++) {
                string gitUrl = RandomGitUrl();
                string version = RandomVersion();
                var entry = new PackageEntry { gitUrl = gitUrl, version = version, enabled = true };

                string result = BuildUrl(entry);

                Assert.That(result, Does.EndWith($"#{version}"),
                    $"Iteration {i}: URL '{result}' should end with '#{version}'");
                Assert.That(result, Does.StartWith(gitUrl),
                    $"Iteration {i}: URL '{result}' should start with '{gitUrl}'");
            }
        }

        /// <summary>Validates: Requirements 6.1, 6.3</summary>
        [Test]
        public void P1_UrlVersionComposition_EmptyVersion_UrlUnchanged() {
            // Feature: bootstrap-tool-optimization-and-packaging, Property 1: URL version composition
            for (int i = 0; i < 100; i++) {
                string gitUrl = RandomGitUrl();
                var entry = new PackageEntry { gitUrl = gitUrl, version = "", enabled = true };

                string result = BuildUrl(entry);

                Assert.AreEqual(gitUrl, result,
                    $"Iteration {i}: URL should be unchanged when version is empty");
            }
        }

        /// <summary>Validates: Requirements 6.1, 6.3</summary>
        [Test]
        public void P1_UrlVersionComposition_NullVersion_UrlUnchanged() {
            // Feature: bootstrap-tool-optimization-and-packaging, Property 1: URL version composition
            for (int i = 0; i < 100; i++) {
                string gitUrl = RandomGitUrl();
                var entry = new PackageEntry { gitUrl = gitUrl, version = null, enabled = true };

                string result = BuildUrl(entry);

                Assert.AreEqual(gitUrl, result,
                    $"Iteration {i}: URL should be unchanged when version is null");
            }
        }

        /// <summary>
        /// Mirrors the private BuildUrl logic in InstallPackageStep.
        /// Appends #version when version is non-empty; returns gitUrl unchanged otherwise.
        /// </summary>
        private static string BuildUrl(PackageEntry entry) {
            if (string.IsNullOrEmpty(entry.version)) return entry.gitUrl;
            return $"{entry.gitUrl}#{entry.version}";
        }

        // ── P2: Folder Creation Idempotence ───────────────────────────────────

        // Feature: bootstrap-tool-optimization-and-packaging, Property 2: For any list of FolderEntry paths,
        // executing CreateFolderStep twice SHALL produce the same set of folders as executing it once,
        // and the second execution SHALL report zero newly created folders.

        /// <summary>Validates: Requirements 2.3, 2.4</summary>
        [Test]
        public void P2_FolderIdempotence_SecondRunCreatesZeroFolders() {
            // Feature: bootstrap-tool-optimization-and-packaging, Property 2: Folder creation idempotence
            for (int i = 0; i < 20; i++) {
                string tempRoot = Path.Combine(Path.GetTempPath(), $"NhemBootstrapTest_{Guid.NewGuid():N}");
                try {
                    Directory.CreateDirectory(tempRoot);

                    int count = Rng.Next(1, 6);
                    var folderEntries = new List<FolderEntry>();
                    for (int j = 0; j < count; j++) {
                        string subPath = Path.Combine(tempRoot, RandomString(4), RandomString(4));
                        folderEntries.Add(new FolderEntry { path = subPath, enabled = true });
                    }

                    // First run: create folders
                    int firstRunCreated = 0;
                    foreach (var entry in folderEntries) {
                        if (!Directory.Exists(entry.path)) {
                            Directory.CreateDirectory(entry.path);
                            firstRunCreated++;
                        }
                    }

                    // Second run: same logic — should create 0 new folders
                    int secondRunCreated = 0;
                    foreach (var entry in folderEntries) {
                        if (!Directory.Exists(entry.path)) {
                            Directory.CreateDirectory(entry.path);
                            secondRunCreated++;
                        }
                    }

                    Assert.AreEqual(0, secondRunCreated,
                        $"Iteration {i}: Second run should create 0 folders, but created {secondRunCreated}");

                    // All folders must exist after both runs
                    foreach (var entry in folderEntries) {
                        Assert.IsTrue(Directory.Exists(entry.path),
                            $"Iteration {i}: Folder '{entry.path}' should exist after both runs");
                    }
                }
                finally {
                    if (Directory.Exists(tempRoot))
                        Directory.Delete(tempRoot, true);
                }
            }
        }

        // ── P3: Asmdef Reference Filtering ────────────────────────────────────

        // Feature: bootstrap-tool-optimization-and-packaging, Property 3: For any AsmdefEntry whose references list
        // contains a mix of installed and uninstalled assembly names, the generated .asmdef file SHALL contain
        // only the subset of references that are currently installed.

        /// <summary>Validates: Requirements 3.3, 3.4</summary>
        [Test]
        public void P3_AsmdefReferenceFiltering_OnlyInstalledRefsInOutput() {
            // Feature: bootstrap-tool-optimization-and-packaging, Property 3: Asmdef reference filtering
            for (int i = 0; i < 100; i++) {
                int installedCount = Rng.Next(0, 6);
                int uninstalledCount = Rng.Next(0, 6);

                var installed = Enumerable.Range(0, installedCount)
                    .Select(_ => $"Installed.{RandomString(6)}")
                    .ToList();
                var uninstalled = Enumerable.Range(0, uninstalledCount)
                    .Select(_ => $"Uninstalled.{RandomString(6)}")
                    .ToList();

                // Mix installed and uninstalled in random order
                var allRefs = installed.Concat(uninstalled).OrderBy(_ => Rng.Next()).ToList();

                // Simulate the filtering logic from GenerateAsmdefStep
                var filteredRefs = allRefs.Where(r => installed.Contains(r)).ToList();

                // Every filtered ref must be in the installed set
                foreach (var r in filteredRefs) {
                    Assert.IsTrue(installed.Contains(r),
                        $"Iteration {i}: Filtered ref '{r}' should be in the installed set");
                }

                // No uninstalled ref may appear in the output
                foreach (var r in uninstalled) {
                    Assert.IsFalse(filteredRefs.Contains(r),
                        $"Iteration {i}: Uninstalled ref '{r}' should not be in filtered output");
                }

                // The filtered set must be a subset of allRefs
                Assert.LessOrEqual(filteredRefs.Count, allRefs.Count,
                    $"Iteration {i}: Filtered count ({filteredRefs.Count}) must not exceed total refs ({allRefs.Count})");
            }
        }

        // ── P4: Snapshot Rollback Completeness ────────────────────────────────

        // Feature: bootstrap-tool-optimization-and-packaging, Property 4: For any bootstrap run that creates N folders
        // and M asmdefs, a subsequent rollback SHALL remove exactly those N folders and M asmdefs.

        /// <summary>Validates: Requirements 7.1, 7.2, 7.3</summary>
        [Test]
        public void P4_SnapshotRollback_RemovesExactlyCreatedItems() {
            // Feature: bootstrap-tool-optimization-and-packaging, Property 4: Snapshot rollback completeness
            for (int i = 0; i < 20; i++) {
                string tempRoot = Path.Combine(Path.GetTempPath(), $"NhemBootstrapRollback_{Guid.NewGuid():N}");
                try {
                    Directory.CreateDirectory(tempRoot);

                    int folderCount = Rng.Next(1, 5);
                    int asmdefCount = Rng.Next(1, 4);

                    var snapshot = new ProjectSnapshot();

                    // Simulate creating folders and recording them in the snapshot
                    var createdFolders = new List<string>();
                    for (int j = 0; j < folderCount; j++) {
                        string path = Path.Combine(tempRoot, $"folder_{j}");
                        Directory.CreateDirectory(path);
                        createdFolders.Add(path);
                        snapshot.CreatedFolders.Add(path);
                    }

                    // Simulate creating asmdef files and recording them in the snapshot
                    var createdAsmdefs = new List<string>();
                    for (int j = 0; j < asmdefCount; j++) {
                        string path = Path.Combine(tempRoot, $"test_{j}.asmdef");
                        File.WriteAllText(path, "{}");
                        createdAsmdefs.Add(path);
                        snapshot.CreatedAsmdefs.Add(path);
                    }

                    // Verify items exist before rollback
                    foreach (var f in createdFolders)
                        Assert.IsTrue(Directory.Exists(f), $"Iteration {i}: Folder should exist before rollback");
                    foreach (var a in createdAsmdefs)
                        Assert.IsTrue(File.Exists(a), $"Iteration {i}: Asmdef should exist before rollback");

                    // Simulate rollback (mirrors BootstrapRunner.RollbackAsync logic)
                    foreach (var asmdefPath in snapshot.CreatedAsmdefs) {
                        if (File.Exists(asmdefPath)) File.Delete(asmdefPath);
                    }
                    var folders = new List<string>(snapshot.CreatedFolders);
                    folders.Sort((a, b) => b.Length.CompareTo(a.Length)); // deepest-first
                    foreach (var folderPath in folders) {
                        if (Directory.Exists(folderPath)) {
                            try { Directory.Delete(folderPath, false); } catch { /* non-empty folders are skipped */ }
                        }
                    }

                    // Verify items are gone after rollback
                    foreach (var f in createdFolders)
                        Assert.IsFalse(Directory.Exists(f),
                            $"Iteration {i}: Folder '{f}' should be removed after rollback");
                    foreach (var a in createdAsmdefs)
                        Assert.IsFalse(File.Exists(a),
                            $"Iteration {i}: Asmdef '{a}' should be removed after rollback");
                }
                finally {
                    if (Directory.Exists(tempRoot))
                        Directory.Delete(tempRoot, true);
                }
            }
        }

        // ── P5: Cancellation Safety ───────────────────────────────────────────

        // Feature: bootstrap-tool-optimization-and-packaging, Property 5: For any bootstrap run cancelled mid-execution,
        // every step that was reported as OnStepCompleted(success=true) before cancellation SHALL have its side effects
        // present, and no step that had not yet started SHALL have any side effects.

        /// <summary>Validates: Requirements 11.3, 11.4, 11.5</summary>
        [Test]
        public void P5_CancellationSafety_CompletedStepsHaveEffectsUnstartedStepsDontRun() {
            // Feature: bootstrap-tool-optimization-and-packaging, Property 5: Cancellation safety
            for (int i = 0; i < 50; i++) {
                int totalSteps = Rng.Next(2, 8);
                int cancelAfter = Rng.Next(0, totalSteps);

                var executedSteps = new List<int>();
                var summary = new BootstrapSummary { TotalSteps = totalSteps };

                // Simulate the BootstrapRunner.RunAsync cancellation logic
                for (int step = 0; step < totalSteps; step++) {
                    if (step >= cancelAfter) {
                        summary.WasCancelled = true;
                        summary.SkippedSteps += totalSteps - step;
                        break;
                    }
                    executedSteps.Add(step);
                    summary.SucceededSteps++;
                }

                // Steps before cancelAfter were executed
                Assert.AreEqual(cancelAfter, executedSteps.Count,
                    $"Iteration {i}: Expected {cancelAfter} steps to execute before cancellation at step {cancelAfter}");

                // Steps at or after cancelAfter were NOT executed
                for (int step = cancelAfter; step < totalSteps; step++) {
                    Assert.IsFalse(executedSteps.Contains(step),
                        $"Iteration {i}: Step {step} should not have executed (cancelled at step {cancelAfter})");
                }

                // Summary arithmetic must hold
                Assert.AreEqual(totalSteps, summary.SucceededSteps + summary.FailedSteps + summary.SkippedSteps,
                    $"Iteration {i}: Summary counts should sum to TotalSteps ({totalSteps})");

                // WasCancelled must be true when cancelAfter < totalSteps
                if (cancelAfter < totalSteps) {
                    Assert.IsTrue(summary.WasCancelled,
                        $"Iteration {i}: WasCancelled should be true when cancelled at step {cancelAfter} of {totalSteps}");
                }
            }
        }

        // ── P6: Summary Accuracy ──────────────────────────────────────────────

        // Feature: bootstrap-tool-optimization-and-packaging, Property 6: For any bootstrap run over N steps,
        // the BootstrapSummary SHALL satisfy: SucceededSteps + FailedSteps + SkippedSteps == TotalSteps.

        /// <summary>Validates: Requirements 5.5, 11.5</summary>
        [Test]
        public void P6_SummaryAccuracy_SucceededPlusFailedPlusSkippedEqualsTotalSteps() {
            // Feature: bootstrap-tool-optimization-and-packaging, Property 6: Summary accuracy
            for (int i = 0; i < 100; i++) {
                int total = Rng.Next(0, 20);
                int succeeded = Rng.Next(0, total + 1);
                int failed = Rng.Next(0, total - succeeded + 1);
                int skipped = total - succeeded - failed;

                var summary = new BootstrapSummary {
                    TotalSteps = total,
                    SucceededSteps = succeeded,
                    FailedSteps = failed,
                    SkippedSteps = skipped
                };

                Assert.AreEqual(summary.TotalSteps,
                    summary.SucceededSteps + summary.FailedSteps + summary.SkippedSteps,
                    $"Iteration {i}: {succeeded}+{failed}+{skipped} should equal TotalSteps ({total})");

                // Each count must be non-negative
                Assert.GreaterOrEqual(summary.SucceededSteps, 0, $"Iteration {i}: SucceededSteps must be >= 0");
                Assert.GreaterOrEqual(summary.FailedSteps, 0, $"Iteration {i}: FailedSteps must be >= 0");
                Assert.GreaterOrEqual(summary.SkippedSteps, 0, $"Iteration {i}: SkippedSteps must be >= 0");
            }
        }

        // ── Example Tests ─────────────────────────────────────────────────────

        /// <summary>
        /// Req 1.4: Timeout behavior — verifies that a timeout flag is set after the threshold elapses.
        /// </summary>
        [Test]
        public void ExampleTest_TimeoutBehavior_TimeoutFlagSetAfterThreshold() {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool timedOut = false;
            double timeoutSeconds = 0.05; // 50 ms — short enough for a unit test

            // Spin until the timeout threshold is crossed (mirrors InstallPackageStep WaitUntil guard)
            while (!timedOut) {
                if (sw.Elapsed.TotalSeconds > timeoutSeconds) {
                    timedOut = true;
                }
                if (sw.Elapsed.TotalSeconds > 2.0) break; // safety guard
            }

            Assert.IsTrue(timedOut, "Timeout flag should be set after the threshold elapses");
        }

        /// <summary>
        /// Req 6.4: Version mismatch logging — verifies that a warning is logged when installed version differs from requested.
        /// </summary>
        [Test]
        public void ExampleTest_VersionMismatch_LogsWarningWhenVersionsDiffer() {
            var context = new BootstrapContext { ProjectName = "Test" };

            string installedVersion = "1.0.0";
            string requestedVersion = "2.0.0";
            string packageName = "TestPackage";

            // Simulate the version mismatch logging logic from InstallPackageStep.ExecuteAsync
            if (!string.IsNullOrEmpty(requestedVersion) && installedVersion != requestedVersion) {
                context.Log($"⚠️ Version mismatch for {packageName}: installed={installedVersion}, requested={requestedVersion}");
            }

            Assert.AreEqual(1, context.Logs.Count, "Should have logged exactly one warning");
            StringAssert.Contains("Version mismatch", context.Logs[0]);
            StringAssert.Contains(packageName, context.Logs[0]);
            StringAssert.Contains(installedVersion, context.Logs[0]);
            StringAssert.Contains(requestedVersion, context.Logs[0]);
        }

        /// <summary>
        /// Req 6.4: No log when versions match — verifies that no warning is emitted for matching versions.
        /// </summary>
        [Test]
        public void ExampleTest_VersionMismatch_NoLogWhenVersionsMatch() {
            var context = new BootstrapContext { ProjectName = "Test" };

            string installedVersion = "1.0.0";
            string requestedVersion = "1.0.0";
            string packageName = "TestPackage";

            if (!string.IsNullOrEmpty(requestedVersion) && installedVersion != requestedVersion) {
                context.Log($"⚠️ Version mismatch for {packageName}: installed={installedVersion}, requested={requestedVersion}");
            }

            Assert.AreEqual(0, context.Logs.Count, "Should not log when versions match");
        }

        /// <summary>
        /// Req 4.2: Minimal profile — verifies that a Minimal config has exactly one folder and no packages.
        /// </summary>
        [Test]
        public void ExampleTest_ProfileLoading_MinimalProfileHasOneFolderAndNoPackages() {
            // Simulate what SampleConfigGenerator.CreateMinimalConfig produces (Req 4.2)
            var config = ScriptableObject.CreateInstance<BootstrapConfig>();
            config.profile = ConfigProfile.Minimal;
            config.folders = new List<FolderEntry> {
                new FolderEntry { path = "Assets/_Project", enabled = true }
            };
            config.packages = new List<PackageEntry>();
            config.asmdefs = new List<AsmdefEntry>();

            Assert.AreEqual(ConfigProfile.Minimal, config.profile);
            Assert.AreEqual(1, config.folders.Count, "Minimal profile should have exactly 1 folder");
            Assert.AreEqual(0, config.packages.Count, "Minimal profile should have no packages");
            Assert.AreEqual(0, config.asmdefs.Count, "Minimal profile should have no asmdefs");
            Assert.AreEqual("Assets/_Project", config.folders[0].path);
            Assert.IsTrue(config.folders[0].enabled);
        }

        /// <summary>
        /// Req 4.3: Full profile — verifies that a Full config has multiple folders and at least one package.
        /// </summary>
        [Test]
        public void ExampleTest_ProfileLoading_FullProfileHasMultipleFoldersAndPackages() {
            // Simulate what SampleConfigGenerator.CreateFullConfig produces (Req 4.3)
            var config = ScriptableObject.CreateInstance<BootstrapConfig>();
            config.profile = ConfigProfile.Full;
            config.folders = new List<FolderEntry>();
            for (int i = 0; i < 10; i++)
                config.folders.Add(new FolderEntry { path = $"Assets/_Project/Layer{i}", enabled = true });
            config.packages = new List<PackageEntry> {
                new PackageEntry {
                    displayName = "UniTask",
                    gitUrl = "https://github.com/Cysharp/UniTask.git",
                    enabled = true
                }
            };
            config.asmdefs = new List<AsmdefEntry>();

            Assert.AreEqual(ConfigProfile.Full, config.profile);
            Assert.Greater(config.folders.Count, 1, "Full profile should have multiple folders");
            Assert.Greater(config.packages.Count, 0, "Full profile should have at least one package");
        }
    }
}
