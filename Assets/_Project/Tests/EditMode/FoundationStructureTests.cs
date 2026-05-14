using System.IO;
using GlassRefrain.Core;
using GlassRefrain.Infrastructure;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode
{
    public class FoundationStructureTests
    {
        [Test]
        public void AuthoredProjectRootAndCoreFoldersExist()
        {
            Assert.That(Directory.Exists(ProjectFolders.Root), Is.True);
            Assert.That(Directory.Exists(ProjectFolders.CodeRoot), Is.True);
            Assert.That(Directory.Exists(ProjectFolders.ContentRoot), Is.True);
            Assert.That(Directory.Exists(ProjectFolders.DataRoot), Is.True);
            Assert.That(Directory.Exists(ProjectFolders.SceneRoot), Is.True);
            Assert.That(Directory.Exists(ProjectFolders.TestsRoot), Is.True);
        }

        [Test]
        public void CameraMovementBasisSnapshotIsReadOnly()
        {
            Assert.That(typeof(CameraMovementBasisSnapshot).IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false), Is.True);
        }

        [Test]
        public void DebugTransitionEventIsReadOnly()
        {
            Assert.That(typeof(DebugTransitionEvent).IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false), Is.True);
        }
    }
}
