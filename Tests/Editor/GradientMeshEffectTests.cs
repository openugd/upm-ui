using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace OpenUGD.UI.Tests
{
    [TestFixture]
    [Category("RequiresUnity")]
    public class GradientMeshEffectTests
    {
        private static readonly Rect FullUv = new Rect(0f, 0f, 1f, 1f);
        private static readonly Rect AtlasUv = new Rect(0.25f, 0.5f, 0.5f, 0.5f);

        private GameObject _gameObject;
        private RectTransform _rectTransform;
        private Image _image;
        private GradientMeshEffect _effect;
        private int _dirtyCount;

        [SetUp]
        public void SetUp()
        {
            // Pivot (0, 0): the rect is (0, 0, 100, 100), off-centre relative to the local origin.
            _gameObject = new GameObject("Gradient", typeof(RectTransform));
            _rectTransform = (RectTransform)_gameObject.transform;
            _rectTransform.sizeDelta = new Vector2(100f, 100f);
            _rectTransform.pivot = Vector2.zero;
            _image = _gameObject.AddComponent<Image>();
            _effect = _gameObject.AddComponent<GradientMeshEffect>();
            _effect.BlendMode = GradientMeshEffect.Blend.Override;
            _image.RegisterDirtyVerticesCallback(OnDirty);
            _dirtyCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        private void OnDirty() => _dirtyCount++;

        private static Gradient Keys(params (float time, Color color)[] keys)
        {
            var colorKeys = new GradientColorKey[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                colorKeys[i] = new GradientColorKey(keys[i].color, keys[i].time);
            }

            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private List<UIVertex> Apply(Rect uv)
        {
            using var vertexHelper = MeshTestUtility.Quad(_rectTransform.rect, uv, Color.white);
            _effect.ModifyMesh(vertexHelper);
            var stream = new List<UIVertex>();
            vertexHelper.GetUIVertexStream(stream);
            return stream;
        }

        private static void AssertColor(Color32 actual, Color expected, string message)
        {
            Color32 expected32 = expected;
            Assert.That(Mathf.Abs(actual.r - expected32.r), Is.LessThanOrEqualTo(2), message + " red " + actual);
            Assert.That(Mathf.Abs(actual.g - expected32.g), Is.LessThanOrEqualTo(2), message + " green " + actual);
            Assert.That(Mathf.Abs(actual.b - expected32.b), Is.LessThanOrEqualTo(2), message + " blue " + actual);
            Assert.That(Mathf.Abs(actual.a - expected32.a), Is.LessThanOrEqualTo(2), message + " alpha " + actual);
        }

        [Test]
        public void Horizontal_PaintsTheEndColoursOnTheEdges()
        {
            _effect.ModifyVertices = false;

            foreach (var vertex in Apply(FullUv))
            {
                AssertColor(vertex.color, vertex.position.x < 50f ? Color.black : Color.white, "x = " + vertex.position.x);
            }
        }

        [Test]
        public void Vertical_PaintsTheEndColoursOnTheEdges()
        {
            _effect.ModifyVertices = false;
            _effect.GradientType = GradientMeshEffect.Type.Vertical;
            _effect.GradientColor = Keys((0f, Color.red), (1f, Color.blue));

            foreach (var vertex in Apply(FullUv))
            {
                AssertColor(vertex.color, vertex.position.y < 50f ? Color.red : Color.blue, "y = " + vertex.position.y);
            }
        }

        [Test]
        public void InnerKey_GetsARowOfVerticesInItsColour()
        {
            _effect.GradientColor = Keys((0f, Color.black), (0.3f, Color.red), (1f, Color.white));

            var stream = Apply(AtlasUv);

            var atKey = 0;
            foreach (var vertex in stream)
            {
                if (Mathf.Abs(vertex.position.x - 30f) < 1e-3f)
                {
                    atKey++;
                    AssertColor(vertex.color, Color.red, "the key's row");
                }
            }

            Assert.That(atKey, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void BlendModes_CombineWithTheVertexColour()
        {
            _effect.ModifyVertices = false;
            _effect.GradientColor = Keys((0f, new Color(0.5f, 0.5f, 0.5f, 1f)), (1f, new Color(0.5f, 0.5f, 0.5f, 1f)));
            var vertexColor = new Color32(128, 64, 32, 255);

            using var vertexHelper = MeshTestUtility.Quad(_rectTransform.rect, FullUv, vertexColor);
            _effect.BlendMode = GradientMeshEffect.Blend.Multiply;
            _effect.ModifyMesh(vertexHelper);
            AssertColor(MeshTestUtility.ReadVertices(vertexHelper)[0].color, new Color(64f / 255f, 32f / 255f, 16f / 255f, 1f), "multiply");

            MeshTestUtility.FillQuad(vertexHelper, _rectTransform.rect, FullUv, vertexColor);
            _effect.BlendMode = GradientMeshEffect.Blend.Add;
            _effect.ModifyMesh(vertexHelper);
            AssertColor(MeshTestUtility.ReadVertices(vertexHelper)[0].color, new Color(1f, 192f / 255f, 160f / 255f, 1f), "add");

            MeshTestUtility.FillQuad(vertexHelper, _rectTransform.rect, FullUv, vertexColor);
            _effect.BlendMode = GradientMeshEffect.Blend.Override;
            _effect.ModifyMesh(vertexHelper);
            AssertColor(MeshTestUtility.ReadVertices(vertexHelper)[0].color, new Color(0.5f, 0.5f, 0.5f, 1f), "override");
        }

        [Test]
        public void Radial_PivotAtTheCorner_IsCentredOnTheRect()
        {
            _effect.GradientType = GradientMeshEffect.Type.Radial;

            var stream = Apply(AtlasUv);

            Assert.That(MeshTestUtility.HasVertexAt(stream, new Vector2(50f, 50f)), Is.True, "a vertex at the centre");
            foreach (var vertex in stream)
            {
                var p = (Vector2)vertex.position;
                var expected = Mathf.Clamp01(Vector2.Distance(p, new Vector2(50f, 50f)) / 50f);
                AssertColor(vertex.color, Color.Lerp(Color.black, Color.white, expected), "at " + p);
            }
        }

        [Test]
        public void Diamond_PivotAtTheCorner_IsTheManhattanDistanceFromTheRectCentre()
        {
            _effect.GradientType = GradientMeshEffect.Type.Diamond;

            var stream = Apply(FullUv);

            Assert.That(MeshTestUtility.HasVertexAt(stream, new Vector2(50f, 50f)), Is.True, "a vertex at the centre");
            foreach (var vertex in stream)
            {
                var p = (Vector2)vertex.position;
                var expected = Mathf.Clamp01((Mathf.Abs(p.x - 50f) + Mathf.Abs(p.y - 50f)) / 50f);
                AssertColor(vertex.color, Color.Lerp(Color.black, Color.white, expected), "at " + p);
            }
        }

        [Test]
        public void ModifyVertices_InterpolatesUvsWithinTheOriginalUvRect()
        {
            _effect.GradientType = GradientMeshEffect.Type.Radial;

            foreach (var vertex in Apply(AtlasUv))
            {
                var expected = MeshTestUtility.ExpectedUv(vertex.position, _rectTransform.rect, AtlasUv);
                Assert.That(Vector2.Distance(vertex.uv0, expected), Is.LessThan(1e-4f), "uv0 at " + vertex.position);
            }
        }

        [Test]
        public void ModifyTangents_WritesTheTangentAndKeepsTheColour()
        {
            _effect.ModifyVertices = false;
            SetModifyTangents(_effect, true);

            foreach (var vertex in Apply(FullUv))
            {
                Assert.That(vertex.color, Is.EqualTo(new Color32(255, 255, 255, 255)), "colour untouched");
                var expected = vertex.position.x < 50f ? Color.black : Color.white;
                Assert.That(Vector4.Distance(vertex.tangent, expected), Is.LessThan(1e-3f), "tangent at " + vertex.position);
            }
        }

        [Test]
        public void Disabled_ChangesNothing()
        {
            using var vertexHelper = MeshTestUtility.Quad(_rectTransform.rect, FullUv, Color.white);
            var before = MeshTestUtility.ReadVertices(vertexHelper);
            _effect.GradientType = GradientMeshEffect.Type.Radial;
            _effect.enabled = false;

            _effect.ModifyMesh(vertexHelper);

            Assert.That(MeshTestUtility.ReadVertices(vertexHelper), Is.EqualTo(before));
            Assert.That(vertexHelper.currentIndexCount, Is.EqualTo(6));
        }

        [Test]
        public void Setters_DirtyTheGraphicsVertices()
        {
            _effect.GradientType = GradientMeshEffect.Type.Diamond;
            _effect.ModifyVertices = false;
            _effect.GradientOffset = 0.5f;
            _effect.GradientZoom = 2f;
            _effect.BlendMode = GradientMeshEffect.Blend.Add;
            _effect.GradientColor = new Gradient();

            Assert.That(_dirtyCount, Is.EqualTo(6));
        }

        [Test]
        public void Setters_ClampOffsetAndZoom()
        {
            _effect.GradientOffset = 3f;
            _effect.GradientZoom = 0f;

            Assert.That(_effect.GradientOffset, Is.EqualTo(1f));
            Assert.That(_effect.GradientZoom, Is.EqualTo(0.1f).Within(1e-6f));
        }

        [Test]
        public void GradientColor_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => _effect.GradientColor = null);
        }

        [Test]
        public void ChangingTheGradientInPlace_IsPickedUpOnceAssignedBack()
        {
            _effect.GradientColor = Keys((0f, Color.black), (0.3f, Color.red), (1f, Color.white));
            Apply(FullUv);

            var gradient = _effect.GradientColor;
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.green, 0.7f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            _effect.GradientColor = gradient;
            var stream = Apply(FullUv);

            Assert.That(MeshTestUtility.HasVertexAt(stream, new Vector2(70f, 0f)), Is.True, "the new key's row");
            Assert.That(MeshTestUtility.HasVertexAt(stream, new Vector2(30f, 0f)), Is.False, "the old key's row is gone");
        }

        [Test]
        public void ChangingTheGradientInPlace_WithoutAssigningBack_KeepsTheCachedKeyRows()
        {
            // The documented contract: the setter is what invalidates the cached key positions. Detecting an
            // in-place change on every rebuild would need Gradient.Equals, which allocates in the Unity runtime.
            _effect.GradientColor = Keys((0f, Color.black), (0.3f, Color.red), (1f, Color.white));
            Apply(FullUv);

            _effect.GradientColor.SetKeys(
                new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.green, 0.7f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            var stream = Apply(FullUv);

            Assert.That(MeshTestUtility.HasVertexAt(stream, new Vector2(30f, 0f)), Is.True, "the cached row stays until reassigned");
        }

        [Test]
        public void Rebuild_AllocatesNothingOnceWarm()
        {
            _effect.GradientType = GradientMeshEffect.Type.Radial;
            _effect.GradientColor = Keys((0f, Color.black), (0.5f, Color.red), (1f, Color.white));
            var vertexHelper = MeshTestUtility.Quad(_rectTransform.rect, FullUv, Color.white);
            var rect = _rectTransform.rect;
            var white = new Color32(255, 255, 255, 255);

            void Rebuild()
            {
                MeshTestUtility.FillQuad(vertexHelper, rect, FullUv, white);
                _effect.ModifyMesh(vertexHelper);
            }

            Rebuild();
            Rebuild();

            Assert.That(new TestDelegate(Rebuild), Is.Not.AllocatingGCMemory());
            vertexHelper.Dispose();
        }

        [Test]
        public void IsInTheOpenUGDNamespaceAndTheEffectsMenu()
        {
            Assert.That(typeof(GradientMeshEffect).Namespace, Is.EqualTo("OpenUGD.UI"));
            var menu = (AddComponentMenu)Attribute.GetCustomAttribute(typeof(GradientMeshEffect), typeof(AddComponentMenu));
            Assert.That(menu.componentMenu, Is.EqualTo("UI/Effects/Gradient"));
        }

        private static void SetModifyTangents(GradientMeshEffect effect, bool value)
        {
            var field = typeof(GradientMeshEffect).GetField("modifyTangents",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(effect, value);
        }
    }
}
