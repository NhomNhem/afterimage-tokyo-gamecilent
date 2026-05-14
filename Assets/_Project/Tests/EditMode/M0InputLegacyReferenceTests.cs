using System.IO;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode
{
    public class M0InputLegacyReferenceTests
    {
        [Test]
        public void InputFoundationAndRouterFilesDoNotReferenceLegacyInputManager()
        {
            string[] files =
            {
                "Assets/_Project/Code/Input/M0InputFoundation.cs",
                "Assets/_Project/Code/Input/M0InputRouter.cs",
                "Assets/_Project/Content/Data/Input/M0InputActions.inputactions"
            };

            foreach (string file in files)
            {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);

                string contents = File.ReadAllText(file);
                Assert.That(contents.Contains("InputManager"), Is.False, file);
                Assert.That(contents.Contains("UnityEngine.Input"), Is.False, file);
            }
        }
    }
}
