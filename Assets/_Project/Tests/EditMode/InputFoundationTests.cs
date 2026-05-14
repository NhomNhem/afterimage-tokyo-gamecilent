using System.IO;
using GlassRefrain.Input;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode
{
    public class InputFoundationTests
    {
        [Test]
        public void NewInputSystemAssetExists()
        {
            Assert.That(File.Exists(M0InputAssetPaths.GameplayActions), Is.True);
        }

        [Test]
        public void NewInputSystemAssetDoesNotReferenceLegacyInputManager()
        {
            var contents = File.ReadAllText(M0InputAssetPaths.GameplayActions);

            Assert.That(contents.Contains("InputManager"), Is.False);
            Assert.That(contents.Contains("UnityEngine.Input"), Is.False);
        }
    }
}
