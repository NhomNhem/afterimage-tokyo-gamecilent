using GlassRefrain.Camera;
using NUnit.Framework;
using UnityEngine;

namespace GlassRefrain.Tests.EditMode {
    public class CameraMovementBasisProviderTests {
        private GameObject _cameraGameObject = null!;
        private GameObject _providerGameObject = null!;

        [TearDown]
        public void TearDown() {
            if (_providerGameObject != null) Object.DestroyImmediate(_providerGameObject);
            if (_cameraGameObject != null) Object.DestroyImmediate(_cameraGameObject);
        }

        [Test]
        public void GetMovementBasis_ProjectsForwardOnGroundPlane() {
            _cameraGameObject = new GameObject("TestCamera");
            var camera = _cameraGameObject.AddComponent<UnityEngine.Camera>();
            _cameraGameObject.transform.rotation = Quaternion.Euler(35f, 45f, 0f);

            _providerGameObject = new GameObject("BasisProvider");
            var provider = _providerGameObject.AddComponent<CameraMovementBasisProvider>();
            SetPrivateTargetCamera(provider, camera);

            var basis = provider.GetMovementBasis();

            Assert.IsTrue(basis.IsValid);
            var expectedForward = new Vector3(_cameraGameObject.transform.forward.x, 0f, _cameraGameObject.transform.forward.z).normalized;
            Assert.That(basis.Forward.X, Is.EqualTo(expectedForward.x).Within(0.0001f));
            Assert.That(basis.Forward.Y, Is.EqualTo(expectedForward.z).Within(0.0001f));
        }

        [Test]
        public void GetMovementBasis_BuildsOrthogonalRightAxisFromForward() {
            _cameraGameObject = new GameObject("RolledCamera");
            var camera = _cameraGameObject.AddComponent<UnityEngine.Camera>();
            _cameraGameObject.transform.rotation = Quaternion.Euler(20f, 30f, 35f);

            _providerGameObject = new GameObject("BasisProvider");
            var provider = _providerGameObject.AddComponent<CameraMovementBasisProvider>();
            SetPrivateTargetCamera(provider, camera);

            var basis = provider.GetMovementBasis();

            Assert.IsTrue(basis.IsValid);
            var forward = new Vector3(basis.Forward.X, 0f, basis.Forward.Y).normalized;
            var right = new Vector3(basis.Right.X, 0f, basis.Right.Y).normalized;
            var dot = Vector3.Dot(forward, right);

            Assert.That(dot, Is.EqualTo(0f).Within(0.0001f), "Ground-plane forward and right should remain orthogonal.");
        }

        private static void SetPrivateTargetCamera(CameraMovementBasisProvider provider, UnityEngine.Camera camera) {
            var field = typeof(CameraMovementBasisProvider).GetField("targetCamera",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(provider, camera);
        }
    }
}
