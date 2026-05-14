using System.IO;
using GlassRefrain.Input;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode
{
    public class InputFoundationTests
    {
        private static readonly string[] ForbiddenLegacyInputPatterns =
        {
            "UnityEngine.Input;",
            "UnityEngine.Input ",
            "Input.GetAxis",
            "Input.GetButton",
            "Input.GetButtonDown",
            "Input.GetButtonUp",
            "Input.GetKey",
            "Input.GetKeyDown",
            "Input.GetKeyUp",
            "Input.GetMouseButton",
            "Input.GetMouseButtonDown",
            "Input.GetMouseButtonUp",
            "Input.mousePosition",
            "Input.touches",
            "Input.touchCount"
        };

        [Test]
        public void NewInputSystemAssetExists()
        {
            Assert.That(File.Exists(M0InputAssetPaths.GameplayActions), Is.True);
        }

        [Test]
        public void NewInputSystemAssetDoesNotReferenceLegacyInputManager()
        {
            var contents = File.ReadAllText(M0InputAssetPaths.GameplayActions);

            foreach (string pattern in ForbiddenLegacyInputPatterns)
            {
                Assert.That(contents.Contains(pattern), Is.False, "Found forbidden legacy input pattern: " + pattern);
            }
        }
    }
}
