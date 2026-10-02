using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OpenUGD.UI.Tests
{
    // Engine-free: where GradientMeshEffect samples its gradient, and the vertices it adds. Gradient itself is an
    // engine object, so these tests stop at the coordinate passed to Gradient.Evaluate; GradientMeshEffectTests
    // covers the colours in Unity.
    public class GradientFrameTests
    {
        // The rect a RectTransform of 100 x 50 with pivot (0, 0) produces: the case the 0.1.x Radial and Diamond
        // shapes got wrong, because they were centred on the pivot.
        private static readonly Rect PivotAtCorner = new Rect(0f, 0f, 100f, 50f);
        private static readonly Rect PivotAtCentre = new Rect(-50f, -25f, 100f, 50f);

        private static float Coordinate(GradientMeshEffect.Type type, Rect bounds, float x, float y,
            float offset = 0f, float zoom = 1f)
        {
            return GradientFrame.Create(type, bounds, offset, zoom).Evaluate(new Vector3(x, y, 0f));
        }

        [Test]
        public void Horizontal_RunsFromZeroAtTheLeftEdgeToOneAtTheRight()
        {
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 0f, 10f), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 25f, 10f), Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 100f, 40f), Is.EqualTo(1f).Within(1e-6f));
        }

        [Test]
        public void Vertical_RunsFromZeroAtTheBottomEdgeToOneAtTheTop()
        {
            Assert.That(Coordinate(GradientMeshEffect.Type.Vertical, PivotAtCentre, 30f, -25f), Is.EqualTo(0f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Vertical, PivotAtCentre, 30f, 0f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Vertical, PivotAtCentre, -50f, 25f), Is.EqualTo(1f).Within(1e-6f));
        }

        [Test]
        public void LinearZoom_MagnifiesAboutTheMiddle()
        {
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 0f, 0f, zoom: 2f), Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 50f, 0f, zoom: 2f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 100f, 0f, zoom: 2f), Is.EqualTo(0.75f).Within(1e-6f));
        }

        [Test]
        public void LinearOffset_SlidesTheGradient()
        {
            // 0.1.x formula, kept: t = (x - min) / (width * zoom) + z - offset * (1 - z), z = (1 - 1 / zoom) / 2.
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 0f, 0f, offset: 0.5f), Is.EqualTo(-0.5f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 100f, 0f, offset: 0.5f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Horizontal, PivotAtCorner, 0f, 0f, offset: 0.5f, zoom: 2f), Is.EqualTo(-0.125f).Within(1e-6f));
        }

        [Test]
        public void Radial_IsCentredOnTheBoundsForAPivotAtTheCorner()
        {
            const GradientMeshEffect.Type radial = GradientMeshEffect.Type.Radial;
            Assert.That(Coordinate(radial, PivotAtCorner, 50f, 25f), Is.EqualTo(0f).Within(1e-6f), "centre");
            Assert.That(Coordinate(radial, PivotAtCorner, 100f, 25f), Is.EqualTo(1f).Within(1e-6f), "right edge");
            Assert.That(Coordinate(radial, PivotAtCorner, 0f, 25f), Is.EqualTo(1f).Within(1e-6f), "left edge");
            Assert.That(Coordinate(radial, PivotAtCorner, 50f, 50f), Is.EqualTo(1f).Within(1e-6f), "top edge");
            Assert.That(Coordinate(radial, PivotAtCorner, 0f, 0f), Is.EqualTo(Mathf.Sqrt(2f)).Within(1e-6f), "corner");
        }

        [Test]
        public void Radial_DoesNotDependOnThePivot()
        {
            const GradientMeshEffect.Type radial = GradientMeshEffect.Type.Radial;
            Assert.That(Coordinate(radial, PivotAtCentre, 0f, 0f), Is.EqualTo(Coordinate(radial, PivotAtCorner, 50f, 25f)).Within(1e-6f));
            Assert.That(Coordinate(radial, PivotAtCentre, 20f, -10f), Is.EqualTo(Coordinate(radial, PivotAtCorner, 70f, 15f)).Within(1e-6f));
        }

        [Test]
        public void Diamond_IsTheManhattanDistanceFromTheCentreOfTheBounds()
        {
            const GradientMeshEffect.Type diamond = GradientMeshEffect.Type.Diamond;
            Assert.That(Coordinate(diamond, PivotAtCorner, 50f, 25f), Is.EqualTo(0f).Within(1e-6f), "centre");
            Assert.That(Coordinate(diamond, PivotAtCorner, 100f, 25f), Is.EqualTo(1f).Within(1e-6f), "right edge");
            Assert.That(Coordinate(diamond, PivotAtCorner, 50f, 0f), Is.EqualTo(1f).Within(1e-6f), "bottom edge");
            Assert.That(Coordinate(diamond, PivotAtCorner, 100f, 50f), Is.EqualTo(2f).Within(1e-6f), "corner");

            // Halfway to the right edge and halfway to the top edge: 0.5 + 0.5 on the Manhattan scale, where a
            // Euclidean distance would give 0.707.
            Assert.That(Coordinate(diamond, PivotAtCorner, 75f, 37.5f), Is.EqualTo(1f).Within(1e-6f), "diagonal");
            Assert.That(Coordinate(diamond, PivotAtCorner, 25f, 12.5f), Is.EqualTo(1f).Within(1e-6f), "opposite diagonal");
        }

        [Test]
        public void RadialAndDiamond_ApplyZoomThenOffset()
        {
            Assert.That(Coordinate(GradientMeshEffect.Type.Radial, PivotAtCorner, 100f, 25f, zoom: 2f), Is.EqualTo(0.5f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Radial, PivotAtCorner, 50f, 25f, offset: 0.25f), Is.EqualTo(-0.25f).Within(1e-6f));
            Assert.That(Coordinate(GradientMeshEffect.Type.Diamond, PivotAtCorner, 100f, 50f, offset: 0.5f, zoom: 2f), Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void ZeroHeightBounds_AreDegenerateForRadialAndFinite()
        {
            var frame = GradientFrame.Create(GradientMeshEffect.Type.Radial, new Rect(0f, 10f, 100f, 0f), 0f, 1f);

            Assert.That(frame.IsDegenerate, Is.True);
            Assert.That(float.IsNaN(frame.Evaluate(new Vector3(30f, 10f, 0f))), Is.False);
            Assert.That(GradientTessellator.NeedsTessellation(frame, MeshTestUtility.Times(0f, 1f)), Is.False);
        }

        [Test]
        public void BlendModes_CombineAsDocumented()
        {
            var vertex = new Color(0.5f, 0.25f, 1f, 0.5f);
            var gradient = new Color(0.5f, 1f, 0.25f, 1f);

            Assert.That(GradientMeshEffect.BlendColors(vertex, gradient, GradientMeshEffect.Blend.Override), Is.EqualTo(gradient));
            Assert.That(GradientMeshEffect.BlendColors(vertex, gradient, GradientMeshEffect.Blend.Multiply),
                Is.EqualTo(new Color(0.25f, 0.25f, 0.25f, 0.5f)));
            Assert.That(GradientMeshEffect.BlendColors(vertex, gradient, GradientMeshEffect.Blend.Add),
                Is.EqualTo(new Color(1f, 1.25f, 1.25f, 1.5f)));
            Assert.That((Color32)GradientMeshEffect.BlendColors(vertex, gradient, GradientMeshEffect.Blend.Add),
                Is.EqualTo(new Color32(255, 255, 255, 255)), "a vertex colour saturates");
        }
    }

    public class GradientTessellatorTests
    {
        private static readonly Rect Square = new Rect(0f, 0f, 100f, 100f);
        private static readonly Rect FullUv = new Rect(0f, 0f, 1f, 1f);

        // A sprite packed into an atlas: the quad maps only this part of the texture.
        private static readonly Rect AtlasUv = new Rect(0.25f, 0.5f, 0.5f, 0.5f);

        private static List<UIVertex> Tessellate(GradientMeshEffect.Type type, Rect rect, Rect uv, List<float> keys,
            float offset = 0f, float zoom = 1f)
        {
            var mesh = MeshTestUtility.QuadTriangles(rect, uv, Color.white);
            var frame = GradientFrame.Create(type, rect, offset, zoom);
            Assert.That(GradientTessellator.NeedsTessellation(frame, keys), Is.True, "needs tessellation");
            GradientTessellator.Tessellate(mesh, frame, keys);
            return mesh;
        }

        private static void AssertKeepsQuad(List<UIVertex> mesh, Rect rect, Rect uv)
        {
            Assert.That(mesh.Count % 3, Is.Zero, "a triangle list");
            Assert.That(MeshTestUtility.TotalArea(mesh), Is.EqualTo(rect.width * rect.height).Within(0.01f), "area");
            MeshTestUtility.AssertWinding(mesh, -1f);
            foreach (var vertex in mesh)
            {
                var p = (Vector2)vertex.position;
                Assert.That(p.x, Is.InRange(rect.xMin - 1e-3f, rect.xMax + 1e-3f), "x inside the outline");
                Assert.That(p.y, Is.InRange(rect.yMin - 1e-3f, rect.yMax + 1e-3f), "y inside the outline");
                var expected = MeshTestUtility.ExpectedUv(p, rect, uv);
                Assert.That(Vector2.Distance(vertex.uv0, expected), Is.LessThan(1e-4f),
                    "uv0 at " + p + " is " + (Vector2)vertex.uv0 + ", expected " + expected);
            }
        }

        [Test]
        public void Linear_WithKeysOnlyAtTheEnds_NeedsNoExtraVertices()
        {
            var frame = GradientFrame.Create(GradientMeshEffect.Type.Horizontal, Square, 0f, 1f);

            Assert.That(GradientTessellator.NeedsTessellation(frame, MeshTestUtility.Times(0f, 1f)), Is.False);
        }

        [Test]
        public void Horizontal_CutsTheMeshAtEveryInnerKey()
        {
            var mesh = Tessellate(GradientMeshEffect.Type.Horizontal, Square, AtlasUv, MeshTestUtility.Times(0f, 0.25f, 0.6f, 1f));

            AssertKeepsQuad(mesh, Square, AtlasUv);
            foreach (var x in new[] { 25f, 60f })
            {
                Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(x, 0f)), Is.True, "bottom at " + x);
                Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(x, 100f)), Is.True, "top at " + x);
                for (var i = 0; i < mesh.Count; i += 3)
                {
                    var min = Mathf.Min(mesh[i].position.x, Mathf.Min(mesh[i + 1].position.x, mesh[i + 2].position.x));
                    var max = Mathf.Max(mesh[i].position.x, Mathf.Max(mesh[i + 1].position.x, mesh[i + 2].position.x));
                    Assert.That(max <= x + 1e-3f || min >= x - 1e-3f, Is.True, "a triangle straddles x = " + x);
                }
            }
        }

        [Test]
        public void Vertical_PlacesKeysWhereZoomAndOffsetPutThem()
        {
            // zoom 2, offset 0: t = y / 200 + 0.25, so key 0.3 lands at y = 10 and key 0.5 at y = 50.
            var mesh = Tessellate(GradientMeshEffect.Type.Vertical, Square, FullUv, MeshTestUtility.Times(0.3f, 0.5f), zoom: 2f);

            AssertKeepsQuad(mesh, Square, FullUv);
            Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(0f, 10f)), Is.True);
            Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(100f, 50f)), Is.True);
        }

        [Test]
        public void Radial_PivotAtTheCorner_PutsAVertexAtTheCentreOfTheBounds()
        {
            var rect = new Rect(0f, 0f, 120f, 80f);
            var mesh = Tessellate(GradientMeshEffect.Type.Radial, rect, AtlasUv, MeshTestUtility.Times(0f, 1f));

            AssertKeepsQuad(mesh, rect, AtlasUv);
            Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(60f, 40f)), Is.True, "centre");
            Assert.That(MeshTestUtility.HasVertexAt(mesh, Vector2.zero), Is.True, "the corners stay");
            Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(120f, 80f)), Is.True, "the corners stay");
            Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(120f, 40f)), Is.True, "key 1 on the right edge");
            Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(60f, 80f)), Is.True, "key 1 on the top edge");
        }

        [Test]
        public void Radial_EveryTriangleLiesInOneWedge()
        {
            var rect = new Rect(0f, 0f, 120f, 80f);
            var mesh = Tessellate(GradientMeshEffect.Type.Radial, rect, FullUv, MeshTestUtility.Times(0f, 0.5f, 1f));
            var centre = rect.center;
            var inverse = new Vector2(2f / rect.width, 2f / rect.height);
            var wedge = 2f * Mathf.PI / GradientTessellator.RadialSectors;

            for (var i = 0; i < mesh.Count; i += 3)
            {
                var g = (MeshTestUtility.Centroid(mesh, i) - centre) * inverse;
                var sector = Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(g.y, g.x), 2f * Mathf.PI) / wedge);
                for (var k = 0; k < 3; k++)
                {
                    var p = ((Vector2)mesh[i + k].position - centre) * inverse;
                    if (p.magnitude < 1e-4f)
                    {
                        continue;
                    }

                    var angle = Mathf.Repeat(Mathf.Atan2(p.y, p.x) - sector * wedge + 1e-3f, 2f * Mathf.PI);
                    Assert.That(angle, Is.LessThanOrEqualTo(wedge + 2e-3f), "triangle " + i / 3 + " leaves wedge " + sector);
                }
            }
        }

        [Test]
        public void Radial_KeysBecomeRingsThroughTheWedgeEdges()
        {
            var mesh = Tessellate(GradientMeshEffect.Type.Radial, Square, FullUv, MeshTestUtility.Times(0f, 0.5f, 1f));
            var frame = GradientFrame.Create(GradientMeshEffect.Type.Radial, Square, 0f, 1f);
            var wedge = 2f * Mathf.PI / GradientTessellator.RadialSectors;

            for (var s = 0; s < GradientTessellator.RadialSectors; s++)
            {
                var direction = new Vector2(Mathf.Cos(s * wedge), Mathf.Sin(s * wedge));
                var onRing = Square.center + direction * 25f;
                Assert.That(MeshTestUtility.HasVertexAt(mesh, onRing), Is.True, "key 0.5 on wedge edge " + s);
                Assert.That(frame.Evaluate(onRing), Is.EqualTo(0.5f).Within(1e-4f));
            }
        }

        [Test]
        public void Diamond_IsExactlyLinearInsideEveryTriangle()
        {
            var rect = new Rect(0f, 0f, 120f, 80f);
            var mesh = Tessellate(GradientMeshEffect.Type.Diamond, rect, AtlasUv, MeshTestUtility.Times(0f, 0.3f, 0.7f, 1f));
            var frame = GradientFrame.Create(GradientMeshEffect.Type.Diamond, rect, 0f, 1f);

            AssertKeepsQuad(mesh, rect, AtlasUv);
            for (var i = 0; i < mesh.Count; i += 3)
            {
                // A function is linear on a triangle when its value at the centroid is the mean of its corners.
                var g = MeshTestUtility.Centroid(mesh, i);
                var mean = (frame.Evaluate(mesh[i].position) + frame.Evaluate(mesh[i + 1].position) +
                            frame.Evaluate(mesh[i + 2].position)) / 3f;
                Assert.That(frame.Evaluate(g), Is.EqualTo(mean).Within(1e-4f), "triangle " + i / 3);
            }
        }

        [Test]
        public void Diamond_PivotAtTheCorner_PutsVerticesAtTheCentreAndOnTheKeyDiamonds()
        {
            var rect = new Rect(0f, 0f, 120f, 80f);
            var mesh = Tessellate(GradientMeshEffect.Type.Diamond, rect, FullUv, MeshTestUtility.Times(0f, 0.5f, 1f));

            Assert.That(MeshTestUtility.HasVertexAt(mesh, new Vector2(60f, 40f)), Is.True, "centre");
            foreach (var corner in new[] { new Vector2(120f, 40f), new Vector2(60f, 80f), new Vector2(0f, 40f), new Vector2(60f, 0f) })
            {
                Assert.That(MeshTestUtility.HasVertexAt(mesh, corner), Is.True, "key 1 at " + corner);
            }

            foreach (var corner in new[] { new Vector2(90f, 40f), new Vector2(60f, 60f), new Vector2(30f, 40f), new Vector2(60f, 20f) })
            {
                Assert.That(MeshTestUtility.HasVertexAt(mesh, corner), Is.True, "key 0.5 at " + corner);
            }
        }

        [Test]
        public void SlicedMesh_KeepsEachCellsTexturing()
        {
            var rect = new Rect(-40f, -30f, 90f, 70f);
            var mesh = MeshTestUtility.SlicedTriangles(rect, out var cells);
            var frame = GradientFrame.Create(GradientMeshEffect.Type.Radial, rect, 0f, 1f);

            GradientTessellator.Tessellate(mesh, frame, MeshTestUtility.Times(0f, 0.4f, 1f));

            Assert.That(MeshTestUtility.TotalArea(mesh), Is.EqualTo(rect.width * rect.height).Within(0.01f));
            for (var i = 0; i < mesh.Count; i += 3)
            {
                var g = MeshTestUtility.Centroid(mesh, i);
                var cell = cells.Find(c => c.cell.Contains(g));
                Assert.That(cell.cell.width, Is.GreaterThan(0f), "triangle " + i / 3 + " is in a cell");
                for (var k = 0; k < 3; k++)
                {
                    var expected = MeshTestUtility.ExpectedUv(mesh[i + k].position, cell.cell, cell.uv);
                    Assert.That(Vector2.Distance(mesh[i + k].uv0, expected), Is.LessThan(1e-4f));
                }
            }
        }

        [Test]
        public void Tessellate_LeavesAnOnlyTouchingKeyAlone()
        {
            // Offset 1 moves key 0 to distance 1 and key 1 out of the bounds; a key at distance 0 or beyond the
            // corners adds nothing.
            var frame = GradientFrame.Create(GradientMeshEffect.Type.Horizontal, Square, 1f, 1f);
            Assert.That(GradientTessellator.NeedsTessellation(frame, MeshTestUtility.Times(0f, 1f)), Is.False);
        }
    }
}
