using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NhemBootStrap.Editor.Core;
using NhemBootstrap.Editor.Config;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NhemBootstrap.Editor.Steps {
    /// <summary>Bootstrap step that installs Unity packages via UPM asynchronously.</summary>
    public class InstallPackageStep : IAsyncBootstrapStep {
        private const double TimeoutSeconds = 30.0;
        private const double ListTimeoutSeconds = 10.0;
        private const double SyncListTimeoutSeconds = 5.0;

        /// <inheritdoc/>
        public string Name => "Install Packages";

        /// <summary>The list of packages managed by this step, with UI state.</summary>
        public List<PackageInfoViewModel> Packages { get; } = new();

        /// <summary>Creates a step from a list of <see cref="PackageEntry"/> config objects.</summary>
        public InstallPackageStep(List<PackageEntry> entries) {
            foreach (var entry in entries) {
                Packages.Add(new PackageInfoViewModel {
                    Entry = entry,
                    Name = string.IsNullOrEmpty(entry.displayName) ? entry.gitUrl : entry.displayName,
                    Selected = entry.enabled
                });
            }
        }

        /// <summary>Legacy constructor accepting raw package name/URL strings.</summary>
        public InstallPackageStep(List<string> packageNames) {
            foreach (var name in packageNames) {
                Packages.Add(new PackageInfoViewModel {
                    Entry = new PackageEntry { gitUrl = name, displayName = name, enabled = true },
                    Name = name,
                    Selected = true
                });
            }
        }

        /// <inheritdoc/>
        public bool CheckCompleted() {
            var installed = GetInstalledPackagesSync();
            if (installed == null) return false;

            bool allSelectedCompleted = true;
            foreach (var p in Packages) {
                p.IsInstalled = installed.ContainsKey(p.Entry.gitUrl) || installed.ContainsKey(p.Name);
                if (p.Selected && !p.IsInstalled) {
                    allSelectedCompleted = false;
                }
            }
            return allSelectedCompleted;
        }

        /// <summary>Synchronous fallback — schedules the async implementation for backward compatibility.</summary>
        public void Execute(BootstrapContext context) {
            // Fire-and-forget via Awaitable (no UniTask dependency)
            _ = ExecuteAsync(context, CancellationToken.None);
        }

        /// <inheritdoc/>
        public async Awaitable ExecuteAsync(BootstrapContext context, CancellationToken ct) {
            var installed = await GetInstalledPackagesAsync(ct);
            int total = Packages.Count(p => p.Selected);
            int done = 0;

            for (int i = 0; i < Packages.Count; i++) {
                ct.ThrowIfCancellationRequested();

                var p = Packages[i];
                if (!p.Selected) continue;

                string url = BuildUrl(p.Entry);
                string packageId = ExtractPackageId(p.Entry.gitUrl);

                // Version mismatch detection — skip install if already present
                if (installed != null && installed.TryGetValue(packageId, out string installedVersion)) {
                    p.IsInstalled = true;
                    p.InstalledVersion = installedVersion;

                    if (!string.IsNullOrEmpty(p.Entry.version) && installedVersion != p.Entry.version) {
                        context.Log($"⚠️ Version mismatch for {p.Name}: installed={installedVersion}, requested={p.Entry.version}");
                    }

                    done++;
                    context.Progress?.Report(new BootstrapProgress(i, Packages.Count, p.Name, (float)done / total));
                    continue;
                }

                context.Log($"📦 Installing: {p.Name} ({url})");
                context.Progress?.Report(new BootstrapProgress(i, Packages.Count, p.Name, (float)done / total));

                var addRequest = Client.Add(url);
                var sw = Stopwatch.StartNew();
                bool timedOut = false;

                // Poll until complete or timeout — yield each frame via Awaitable
                while (!addRequest.IsCompleted) {
                    if (sw.Elapsed.TotalSeconds > TimeoutSeconds) {
                        timedOut = true;
                        break;
                    }
                    ct.ThrowIfCancellationRequested();
                    await Awaitable.NextFrameAsync(ct);
                }

                if (timedOut) {
                    context.Log($"⏱️ Timeout installing {p.Name} after {TimeoutSeconds}s — skipping.");
                    done++;
                    context.Progress?.Report(new BootstrapProgress(i, Packages.Count, p.Name, (float)done / total));
                    continue;
                }

                if (addRequest.Status == StatusCode.Success) {
                    p.IsInstalled = true;
                    p.InstalledVersion = addRequest.Result?.version;
                    context.Log($"✅ Installed: {p.Name}");
                }
                else {
                    context.Log($"❌ Failed to install {p.Name}: [{addRequest.Error?.errorCode}] {addRequest.Error?.message}");
                }

                done++;
                context.Progress?.Report(new BootstrapProgress(i, Packages.Count, p.Name, (float)done / total));
            }

            AssetDatabase.Refresh();
            context.Log($"Package installation complete. {done}/{total} processed.");
        }

        /// <summary>Returns installed packages as a dictionary of packageId → version using async polling.</summary>
        public async Awaitable<Dictionary<string, string>> GetInstalledPackagesAsync(CancellationToken ct) {
            var listRequest = Client.List(true);
            var sw = Stopwatch.StartNew();

            while (!listRequest.IsCompleted) {
                if (sw.Elapsed.TotalSeconds > ListTimeoutSeconds) break;
                ct.ThrowIfCancellationRequested();
                await Awaitable.NextFrameAsync(ct);
            }

            if (!listRequest.IsCompleted || listRequest.Status != StatusCode.Success) return null;
            return listRequest.Result.ToDictionary(p => p.name, p => p.version);
        }

        /// <summary>Synchronous package list for <see cref="CheckCompleted"/> (short busy-wait, 5-second guard).</summary>
        public Dictionary<string, string> GetInstalledPackagesSync() {
            var listRequest = Client.List(true);
            var sw = Stopwatch.StartNew();
            while (!listRequest.IsCompleted) {
                if (sw.Elapsed.TotalSeconds > SyncListTimeoutSeconds) return null;
                System.Threading.Thread.Sleep(10);
            }
            if (listRequest.Status != StatusCode.Success) return null;
            return listRequest.Result.ToDictionary(p => p.name, p => p.version);
        }

        /// <summary>Legacy method kept for backward compatibility — returns only package names.</summary>
        public HashSet<string> GetInstalledPackages() {
            var dict = GetInstalledPackagesSync();
            return dict == null ? null : new HashSet<string>(dict.Keys);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Appends <c>#version</c> to the Git URL when <see cref="PackageEntry.version"/> is non-empty.</summary>
        private static string BuildUrl(PackageEntry entry) {
            if (string.IsNullOrEmpty(entry.version)) return entry.gitUrl;
            return $"{entry.gitUrl}#{entry.version}";
        }

        /// <summary>Strips query string and fragment from a Git URL to get the bare package identifier.</summary>
        private static string ExtractPackageId(string gitUrl) {
            if (string.IsNullOrEmpty(gitUrl)) return gitUrl;
            int q = gitUrl.IndexOf('?');
            int h = gitUrl.IndexOf('#');
            string clean = gitUrl;
            if (q >= 0) clean = clean.Substring(0, q);
            if (h >= 0) clean = clean.Substring(0, h);
            return clean;
        }
    }

    /// <summary>UI view model for a package entry in the Bootstrap Window.</summary>
    public class PackageInfoViewModel {
        /// <summary>The underlying config entry.</summary>
        public PackageEntry Entry;
        /// <summary>Display name shown in the UI.</summary>
        public string Name;
        /// <summary>Whether the user has selected this package for installation.</summary>
        public bool Selected;
        /// <summary>Whether this package is currently installed in the project.</summary>
        public bool IsInstalled;
        /// <summary>Installed version string (populated after a package list query).</summary>
        public string InstalledVersion;
    }
}
