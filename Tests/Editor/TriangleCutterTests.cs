using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OpenUGD.UI.Tests
{
    // Engine-free: the triangle cutting behind GradientMeshEffect.ModifyVertices.
    public class TriangleCutterTests
    {
        private const float Epsilon = 1e-4f;

        // A counter-clockwise triangle whose attributes are affine functions of position, so any correct
        // interpolation reproduces them exactly at every new vertex.
        private static UIVertex Attributed(float x, float y)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(x, y, 0f);
            vertex.uv0 = new Vector4(x / 100f, y / 100f, 0f, 0f);
            vertex.uv1 = new Vector4(1f - x / 100f, 0.5f, y / 200f, 1f);
            vertex.uv2 = new Vector4(x, y, x + y, 0f);
            vertex.uv3 = new Vector4(0.25f, x / 50f, 0f, y / 50f);
            vertex.color = new Color32((byte)(x * 2f), (byte)(y * 2f), 0, 255);
            return vertex;
        }

        private static List<UIVertex> Cut(UIVertex a, UIVertex b, UIVertex c, Vector2 normal, float distance)
        {
            var target = new List<UIVertex>();
            TriangleCutter.CutTriangle(a, b, c, normal, distance, Epsilon, target);
            return target;
        }

        [Test]
        public void TriangleAcrossTheLine_BecomesThreeTrianglesWithTheSameArea()
        {
            var a = Attributed(0f, 0f);
            var b = Attributed(100f, 0f);
            var c = Attributed(0f, 100f);

            var pieces = Cut(a, b, c, Vector2.right, 40f);

            Assert.That(pieces.Count, Is.EqualTo(9));
            Assert.That(MeshTestUtility.TotalArea(pieces), Is.EqualTo(5000f).Within(MeshTestUtility.Tolerance));
            MeshTestUtility.AssertWinding(pieces, 1f);
        }

        [Test]
        public void EveryPiece_LiesOnOneSideOfTheLine()
        {
            var pieces = Cut(Attributed(0f, 0f), Attributed(100f, 0f), Attributed(30f, 100f), Vector2.right, 55f);

            for (var i = 0; i < pieces.Count; i += 3)
            {
                var min = Mathf.Min(pieces[i].position.x, Mathf.Min(pieces[i + 1].position.x, pieces[i + 2].position.x));
                var max = Mathf.Max(pieces[i].position.x, Mathf.Max(pieces[i + 1].position.x, pieces[i + 2].position.x));
                Assert.That(max <= 55f + Epsilon || min >= 55f - Epsilon, Is.True,
                    "piece " + i / 3 + " spans x " + min + ".." + max);
            }
        }

        [Test]
        public void NewVertices_InterpolateEveryAttribute()
        {
            var pieces = Cut(Attributed(0f, 0f), Attributed(100f, 0f), Attributed(0f, 100f),
                new Vector2(0.6f, 0.8f), 40f);

            foreach (var vertex in pieces)
            {
                var expected = Attributed(vertex.position.x, vertex.position.y);
                Assert.That(Vector4.Distance(vertex.uv0, expected.uv0), Is.LessThan(1e-4f), "uv0");
                Assert.That(Vector4.Distance(vertex.uv1, expected.uv1), Is.LessThan(1e-4f), "uv1");
                Assert.That(Vector4.Distance(vertex.uv2, expected.uv2), Is.LessThan(1e-3f), "uv2");
                Assert.That(Vector4.Distance(vertex.uv3, expected.uv3), Is.LessThan(1e-4f), "uv3");
                Assert.That(Mathf.Abs(vertex.color.r - expected.color.r), Is.LessThanOrEqualTo(1), "red");
                Assert.That(Mathf.Abs(vertex.color.g - expected.color.g), Is.LessThanOrEqualTo(1), "green");
                Assert.That(vertex.normal, Is.EqualTo(UIVertex.simpleVert.normal), "normal");
                Assert.That(vertex.tangent, Is.EqualTo(UIVertex.simpleVert.tangent), "tangent");
            }
        }

        [Test]
        public void TriangleOnOneSide_IsCopiedUnchanged()
        {
            var a = Attributed(0f, 0f);
            var b = Attributed(10f, 0f);
            var c = Attributed(0f, 10f);

            var pieces = Cut(a, b, c, Vector2.right, 50f);

            Assert.That(pieces, Is.EqualTo(new List<UIVertex> { a, b, c }));
        }

        [Test]
        public void TriangleTouchingTheLineAtAVertex_IsCopiedUnchanged()
        {
            var a = Attributed(0f, 0f);
            var b = Attributed(50f, 0f);
            var c = Attributed(0f, 10f);

            var pieces = Cut(a, b, c, Vector2.right, 50f);

            Assert.That(pieces, Is.EqualTo(new List<UIVertex> { a, b, c }));
        }

        [Test]
        public void LineThroughAVertex_SplitsIntoTwoTriangles()
        {
            var pieces = Cut(Attributed(0f, 0f), Attributed(100f, 0f), Attributed(50f, 100f), Vector2.right, 50f);

            Assert.That(pieces.Count, Is.EqualTo(6));
            Assert.That(MeshTestUtility.TotalArea(pieces), Is.EqualTo(5000f).Within(MeshTestUtility.Tolerance));
            Assert.That(MeshTestUtility.HasVertexAt(pieces, new Vector2(50f, 0f)), Is.True);
            MeshTestUtility.AssertWinding(pieces, 1f);
        }

        [Test]
        public void ClockwiseTriangle_KeepsItsWinding()
        {
            var pieces = Cut(Attributed(0f, 0f), Attributed(0f, 100f), Attributed(100f, 0f), Vector2.up, 30f);

            Assert.That(pieces.Count, Is.EqualTo(9));
            MeshTestUtility.AssertWinding(pieces, -1f);
        }

        [Test]
        public void TrianglesSharingAnEdge_GetIdenticalVerticesOnIt()
        {
            // The quad's diagonal runs (0,0)-(100,100); the two triangles walk it in opposite directions.
            var source = MeshTestUtility.QuadTriangles(new Rect(0f, 0f, 100f, 100f), new Rect(0f, 0f, 1f, 1f),
                Color.white);
            var target = new List<UIVertex>();

            TriangleCutter.Cut(source, target, Vector2.right, 37.3f, Epsilon);
            var crossings = new HashSet<Vector3>();
            foreach (var vertex in target)
            {
                var p = vertex.position;
                if (Mathf.Abs(p.x - p.y) < 1e-3f && p.x > 1f && p.x < 99f)
                {
                    crossings.Add(p);
                }
            }

            // Both triangles cut the shared diagonal at (37.3, 37.3): one distinct position there, not two that
            // differ in the last bit and leave a crack.
            Assert.That(crossings.Count, Is.EqualTo(1));
            Assert.That(MeshTestUtility.TotalArea(target), Is.EqualTo(10000f).Within(MeshTestUtility.Tolerance));
        }

        [Test]
        public void Cut_IgnoresATrailingPartialTriangle()
        {
            var source = new List<UIVertex> { Attributed(0f, 0f), Attributed(100f, 0f), Attributed(0f, 100f),
                Attributed(5f, 5f) };
            var target = new List<UIVertex>();

            TriangleCutter.Cut(source, target, Vector2.right, 200f, Epsilon);

            Assert.That(target.Count, Is.EqualTo(3));
        }

        [Test]
        public void Lerp_InterpolatesEndpoints()
        {
            var from = Attributed(0f, 0f);
            var to = Attributed(100f, 50f);

            Assert.That(TriangleCutter.Lerp(from, to, 0f), Is.EqualTo(from));
            Assert.That(TriangleCutter.Lerp(from, to, 1f).position, Is.EqualTo(to.position));
            Assert.That(TriangleCutter.Lerp(from, to, 0.5f).position, Is.EqualTo(new Vector3(50f, 25f, 0f)));
        }
    }
}
