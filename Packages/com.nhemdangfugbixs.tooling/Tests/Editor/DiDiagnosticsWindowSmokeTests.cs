using System;
using System.Linq;
using NUnit.Framework;
using NhemDangFugBixs.Editor;
using UnityEditor;
using UnityEngine;

namespace NhemDangFugBixs.Tooling.Editor.Tests {
    public sealed class DiDiagnosticsWindowSmokeTests {
        [Test]
        public void OpenWindow_CreatesDiagnosticsWindowInstance() {
            var before = Resources.FindObjectsOfTypeAll<DiDiagnosticsWindow>();

            DiDiagnosticsWindow.OpenWindow();

            var after = Resources.FindObjectsOfTypeAll<DiDiagnosticsWindow>();
            Assert.That(after.Length, Is.GreaterThanOrEqualTo(before.Length + 1));
            var window = after.LastOrDefault();
            Assert.That(window, Is.Not.Null);
            Assert.That(window!.titleContent.text, Is.EqualTo("DI Diagnostics"));

            window.Close();
        }

        [Test]
        public void Window_CanRepaintAndRenderWithoutExceptions() {
            DiDiagnosticsWindow.OpenWindow();
            var window = Resources.FindObjectsOfTypeAll<DiDiagnosticsWindow>().LastOrDefault();
            Assert.That(window, Is.Not.Null);

            Assert.DoesNotThrow(() => {
                window!.Repaint();
                window.SendEvent(new Event { type = EventType.Repaint });
            });

            window!.Close();
        }
    }
}
