using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace NhemBootStrap.Editor.Core {
    /// <summary>Orchestrates the execution of bootstrap steps, supporting both sync and async execution.</summary>
    public class BootstrapRunner {
        private readonly List<IBootstrapStep> _steps;

        /// <summary>Raised when a step begins execution. Parameters: stepName, stepIndex (0-based), totalSteps.</summary>
        public event Action<string, int, int> OnStepStarted;

        /// <summary>Raised when a step finishes. Parameters: stepName, success, errorMessage (null if success).</summary>
        public event Action<string, bool, string> OnStepCompleted;

        /// <summary>Raised when all steps have been processed.</summary>
        public event Action<BootstrapSummary> OnAllCompleted;

        /// <summary>Creates a new runner with the given list of steps.</summary>
        public BootstrapRunner(List<IBootstrapStep> steps) {
            _steps = steps;
        }

        /// <summary>
        /// Runs all steps asynchronously. Steps that implement <see cref="IAsyncBootstrapStep"/> are awaited;
        /// others are executed synchronously on the calling thread.
        /// </summary>
        /// <param name="steps">The ordered list of steps to execute.</param>
        /// <param name="context">Shared execution context passed to each step.</param>
        /// <param name="ct">Token used to cancel the run mid-execution.</param>
        /// <returns>A <see cref="BootstrapSummary"/> describing the outcome of the run.</returns>
        public async Awaitable<BootstrapSummary> RunAsync(
            IReadOnlyList<IBootstrapStep> steps,
            BootstrapContext context,
            CancellationToken ct) {

            var summary = new BootstrapSummary { TotalSteps = steps.Count };
            context.CancellationToken = ct;

            for (int i = 0; i < steps.Count; i++) {
                if (ct.IsCancellationRequested) {
                    summary.SkippedSteps += steps.Count - i;
                    summary.WasCancelled = true;
                    break;
                }

                var step = steps[i];
                OnStepStarted?.Invoke(step.Name, i, steps.Count);
                context.Progress?.Report(new BootstrapProgress(i, steps.Count, step.Name));

                try {
                    if (step is IAsyncBootstrapStep asyncStep) {
                        await asyncStep.ExecuteAsync(context, ct);
                    }
                    else {
                        step.Execute(context);
                    }

                    summary.SucceededSteps++;
                    OnStepCompleted?.Invoke(step.Name, true, null);
                }
                catch (OperationCanceledException) {
                    summary.SkippedSteps += steps.Count - i;
                    summary.WasCancelled = true;
                    OnStepCompleted?.Invoke(step.Name, false, "Cancelled");
                    break;
                }
                catch (Exception ex) {
                    summary.FailedSteps++;
                    summary.Failures.Add((step.Name, ex.Message));
                    context.Log($"❌ Failed: {step.Name} - {ex.Message}");
                    OnStepCompleted?.Invoke(step.Name, false, ex.Message);
                }
            }

            OnAllCompleted?.Invoke(summary);
            return summary;
        }

        /// <summary>
        /// Rolls back the changes made during a run by removing created folders and asmdefs.
        /// Folders are removed deepest-first so that nested paths are cleaned up before their parents.
        /// </summary>
        /// <param name="snapshot">The snapshot captured at the start of the run.</param>
        /// <param name="context">Shared execution context used for logging.</param>
        public async Awaitable RollbackAsync(ProjectSnapshot snapshot, BootstrapContext context) {
            context.Log("⏪ Starting rollback...");

            // Remove created asmdefs first
            foreach (var asmdefPath in snapshot.CreatedAsmdefs) {
                try {
                    if (File.Exists(asmdefPath)) {
                        File.Delete(asmdefPath);
                        context.Log($"Removed asmdef: {asmdefPath}");
                    }
                }
                catch (Exception ex) {
                    context.Log($"⚠️ Could not remove asmdef {asmdefPath}: {ex.Message}");
                }
            }

            // Remove created folders deepest-first to handle nested paths
            var folders = new List<string>(snapshot.CreatedFolders);
            folders.Sort((a, b) => b.Length.CompareTo(a.Length));

            foreach (var folderPath in folders) {
                try {
                    if (Directory.Exists(folderPath)) {
                        Directory.Delete(folderPath, false);
                        context.Log($"Removed folder: {folderPath}");
                    }
                }
                catch (Exception ex) {
                    context.Log($"⚠️ Could not remove folder {folderPath}: {ex.Message}");
                }
            }

            AssetDatabase.Refresh();
            context.Log("✅ Rollback complete.");
            await Awaitable.NextFrameAsync();
        }

        /// <summary>
        /// Synchronous run for backward compatibility. Executes all steps in order.
        /// A failed step is logged and skipped; remaining steps continue to run.
        /// </summary>
        /// <param name="context">Shared execution context passed to each step.</param>
        public void Run(BootstrapContext context) {
            bool allSucceeded = true;

            foreach (var step in _steps) {
                try {
                    context.Log($"▶ Running: {step.Name}");
                    step.Execute(context);
                }
                catch (Exception ex) {
                    context.Log($"❌ Failed: {step.Name} - {ex.Message}");
                    allSucceeded = false;
                }
            }

            if (allSucceeded) {
                context.Log("✅ Bootstrap Completed");
            }
            else {
                context.Log("⚠️ Bootstrap Completed with Errors");
            }
        }

        /// <summary>
        /// Creates a baseline <see cref="ProjectSnapshot"/> before the run begins.
        /// Individual steps are responsible for recording items they create into the snapshot.
        /// </summary>
        private static ProjectSnapshot CreateSnapshot() {
            return new ProjectSnapshot();
        }
    }
}
