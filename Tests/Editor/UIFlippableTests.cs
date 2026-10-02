using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.UI.Tests
{
    [TestFixture]
    [Category("RequiresUnity")]
    public class UIFlippableTests
    {
        private GameObject _gameObject;
        private RectTransform _rectTransform;
        private Image _image;
        private UIFlippable _flippable;
        private int _dirtyCount;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Flippable", typeof(RectTransform));
            _rectTransform = (RectTransform)_gameObject.transform;
            _rectTransform.sizeDelta = new Vector2(100f, 50f);
            _rectTransform.pivot = Vector2.zero;
            _image = _gameObject.AddComponent<Image>();
            _flippable = _gameObject.AddComponent<UIFlippable>();
            _image.RegisterDirtyVerticesCallback(OnDirty);
            _dirtyCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        private void OnDirty() => _dirtyCount++;

        // An asymmetric triangle: mirroring moves every vertex.
        private static VertexHelper Triangle()
        {
            var vertexHelper = new VertexHelper();
            vertexHelper.AddVert(MeshTestUtility.Vertex(10f, 5f));
            vertexHelper.AddVert(MeshTestUtility.Vertex(30f, 5f));
            vertexHelper.AddVert(MeshTestUtility.Vertex(10f, 45f));
            vertexHelper.AddTriangle(0, 1, 2);
            return vertexHelper;
        }

        [Test]
        public void Horizontal_MirrorsAboutTheRectCentre()
        {
            using var vertexHelper = Triangle();
            _flippable.horizontal = true;

            _flippable.ModifyMesh(vertexHelper);

            // The rect is (0, 0, 100, 50): x mirrors about 50.
            var vertices = MeshTestUtility.ReadVertices(vertexHelper);
            Assert.That(vertices[0].position, Is.EqualTo(new Vector3(90f, 5f, 0f)));
            Assert.That(vertices[1].position, Is.EqualTo(new Vector3(70f, 5f, 0f)));
            Assert.That(vertices[2].position, Is.EqualTo(new Vector3(90f, 45f, 0f)));
        }

        [Test]
        public void Vertical_MirrorsAboutTheRectCentre()
        {
            using var vertexHelper = Triangle();
            _flippable.vertical = true;

            _flippable.ModifyMesh(vertexHelper);

            // y mirrors about 25.
            var vertices = MeshTestUtility.ReadVertices(vertexHelper);
            Assert.That(vertices[0].position, Is.EqualTo(new Vector3(10f, 45f, 0f)));
            Assert.That(vertices[1].position, Is.EqualTo(new Vector3(30f, 45f, 0f)));
            Assert.That(vertices[2].position, Is.EqualTo(new Vector3(10f, 5f, 0f)));
        }

        [Test]
        public void CentredPivot_MirrorsAboutTheOrigin()
        {
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            using var vertexHelper = Triangle();
            _flippable.horizontal = true;
            _flippable.vertical = true;

            _flippable.ModifyMesh(vertexHelper);

            var vertices = MeshTestUtility.ReadVertices(vertexHelper);
            Assert.That(vertices[0].position, Is.EqualTo(new Vector3(-10f, -5f, 0f)));
            Assert.That(vertices[2].position, Is.EqualTo(new Vector3(-10f, -45f, 0f)));
        }

        [Test]
        public void Disabled_ChangesNothing()
        {
            using var vertexHelper = Triangle();
            var before = MeshTestUtility.ReadVertices(vertexHelper);
            _flippable.horizontal = true;
            _flippable.vertical = true;
            _flippable.enabled = false;

            _flippable.ModifyMesh(vertexHelper);

            Assert.That(MeshTestUtility.ReadVertices(vertexHelper), Is.EqualTo(before));
        }

        [Test]
        public void InactiveGameObject_ChangesNothing()
        {
            using var vertexHelper = Triangle();
            var before = MeshTestUtility.ReadVertices(vertexHelper);
            _flippable.horizontal = true;
            _gameObject.SetActive(false);

            _flippable.ModifyMesh(vertexHelper);

            Assert.That(MeshTestUtility.ReadVertices(vertexHelper), Is.EqualTo(before));
        }

        [Test]
        public void Setters_DirtyTheGraphicsVertices()
        {
            _flippable.horizontal = true;
            Assert.That(_dirtyCount, Is.EqualTo(1), "horizontal");

            _flippable.vertical = true;
            Assert.That(_dirtyCount, Is.EqualTo(2), "vertical");

            _flippable.horizontal = false;
            Assert.That(_dirtyCount, Is.EqualTo(3), "horizontal back");
        }

        [Test]
        public void Setters_WithAnUnchangedValue_DoNotDirty()
        {
            _flippable.horizontal = false;
            _flippable.vertical = false;

            Assert.That(_dirtyCount, Is.Zero);
        }

        [Test]
        public void Setters_StoreTheValue()
        {
            _flippable.horizontal = true;
            _flippable.vertical = true;

            Assert.That(_flippable.horizontal, Is.True);
            Assert.That(_flippable.vertical, Is.True);
        }

        [Test]
        public void IsInTheOpenUGDNamespaceAndTheEffectsMenu()
        {
            Assert.That(typeof(UIFlippable).Namespace, Is.EqualTo("OpenUGD.UI"));
            var menu = (AddComponentMenu)System.Attribute.GetCustomAttribute(typeof(UIFlippable), typeof(AddComponentMenu));
            Assert.That(menu.componentMenu, Is.EqualTo("UI/Effects/Flippable"));
        }
    }
}
