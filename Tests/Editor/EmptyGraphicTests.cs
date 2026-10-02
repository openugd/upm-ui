using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OpenUGD.UI.Tests
{
    [TestFixture]
    [Category("RequiresUnity")]
    public class EmptyGraphicTests
    {
        private GameObject _gameObject;
        private EmptyGraphic _graphic;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("EmptyGraphic", typeof(RectTransform));
            ((RectTransform)_gameObject.transform).sizeDelta = new Vector2(100f, 50f);
            _graphic = _gameObject.AddComponent<EmptyGraphic>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void PopulateMesh_LeavesNoVertices()
        {
            using var vertexHelper = MeshTestUtility.Quad(new Rect(0f, 0f, 100f, 50f), new Rect(0f, 0f, 1f, 1f), Color.white);
            var populate = typeof(EmptyGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null);
            Assert.That(populate, Is.Not.Null);

            populate.Invoke(_graphic, new object[] { vertexHelper });

            Assert.That(vertexHelper.currentVertCount, Is.Zero);
            Assert.That(vertexHelper.currentIndexCount, Is.Zero);
        }

        [Test]
        public void RaycastFilter_AcceptsEveryLocation()
        {
            Assert.That(_graphic.IsRaycastLocationValid(Vector2.zero, null), Is.True);
            Assert.That(_graphic.IsRaycastLocationValid(new Vector2(-1e6f, 1e6f), null), Is.True);
        }

        [Test]
        public void Raycast_HitsWhileActive()
        {
            Assert.That(_graphic.raycastTarget, Is.True, "a raycast target by default");
            Assert.That(_graphic.Raycast(new Vector2(10f, 10f), null), Is.True);
        }

        [Test]
        public void RequiresACanvasRenderer()
        {
            Assert.That(_gameObject.GetComponent<CanvasRenderer>(), Is.Not.Null);
        }

        [Test]
        public void IsInTheOpenUGDNamespaceAndTheUIMenu()
        {
            Assert.That(typeof(EmptyGraphic).Namespace, Is.EqualTo("OpenUGD.UI"));
            var menu = (AddComponentMenu)Attribute.GetCustomAttribute(typeof(EmptyGraphic), typeof(AddComponentMenu));
            Assert.That(menu.componentMenu, Is.EqualTo("UI/Empty Graphic"));
        }
    }
}
