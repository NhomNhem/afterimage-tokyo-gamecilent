using System.IO;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0InputLegacyReferenceTests {
        private static readonly string[] ForbiddenLegacyInputPatterns = {
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
        public void InputFoundationAndRouterFilesDoNotReferenceLegacyInputManager() {
            string[] files = {
                "Assets/_Project/Code/Input/M0InputFoundation.cs",
                "Assets/_Project/Code/Input/M0InputRouter.cs",
                "Assets/_Project/Content/Data/Input/M0InputActions.inputactions"
            };

            foreach (var file in files) {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);

                var contents = File.ReadAllText(file);

                foreach (var pattern in ForbiddenLegacyInputPatterns)
                    Assert.That(contents.Contains(pattern), Is.False,
                        file + " contains forbidden legacy input pattern: " + pattern);
            }
        }
    }
}