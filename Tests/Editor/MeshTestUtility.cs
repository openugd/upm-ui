using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.UI.Tests
{
    /// <summary>
    /// Builds test meshes the way uGUI builds them, and checks geometry. Engine-free: it touches only
    /// <see cref="VertexHelper"/> members that are plain C# (never <c>GetUIVertexStream</c>, which calls into the
    /// engine), so the tests that use it run without Unity.
    /// </summary>
    internal static class MeshTestUtility
    {
        public const float Tolerance = 1e-3f;

        public static UIVertex Vertex(float x, float y, Vector2 uv, Color32 color)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(x, y, 0f);
            vertex.uv0 = uv;
            vertex.color = color;
            return vertex;
        }

        public static UIVertex Vertex(float x, float y) => Vertex(x, y, Vector2.zero, Color.white);

        /// <summary>
        /// The quad Graphic.OnPopulateMesh emits for <paramref name="rect"/>: corners bottom-left, top-left,
        /// top-right, bottom-right, triangles (0, 1, 2) and (2, 3, 0), UVs spanning <paramref name="uv"/>.
        /// </summary>
        public static UIVertex[] QuadCorners(Rect rect, Rect uv, Color32 color)
        {
            return new[]
            {
                Vertex(rect.xMin, rect.yMin, new Vector2(uv.xMin, uv.yMin), color),
                Vertex(rect.xMin, rect.yMax, new Vector2(uv.xMin, uv.yMax), color),
                Vertex(rect.xMax, rect.yMax, new Vector2(uv.xMax, uv.yMax), color),
                Vertex(rect.xMax, rect.yMin, new Vector2(uv.xMax, uv.yMin), color)
            };
        }

        /// <summary>Refills <paramref name="vertexHelper"/> with the quad. Allocates nothing once its lists have grown.</summary>
        public static void FillQuad(VertexHelper vertexHelper, Rect rect, Rect uv, Color32 color)
        {
            vertexHelper.Clear();
            vertexHelper.AddVert(Vertex(rect.xMin, rect.yMin, new Vector2(uv.xMin, uv.yMin), color));
            vertexHelper.AddVert(Vertex(rect.xMin, rect.yMax, new Vector2(uv.xMin, uv.yMax), color));
            vertexHelper.AddVert(Vertex(rect.xMax, rect.yMax, new Vector2(uv.xMax, uv.yMax), color));
            vertexHelper.AddVert(Vertex(rect.xMax, rect.yMin, new Vector2(uv.xMax, uv.yMin), color));
            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }

        public static VertexHelper Quad(Rect rect, Rect uv, Color32 color)
        {
            var vertexHelper = new VertexHelper();
            FillQuad(vertexHelper, rect, uv, color);
            return vertexHelper;
        }

        /// <summary>The same quad as a triangle list, as VertexHelper.GetUIVertexStream returns it.</summary>
        public static List<UIVertex> QuadTriangles(Rect rect, Rect uv, Color32 color)
        {
            var corners = QuadCorners(rect, uv, color);
            return new List<UIVertex> { corners[0], corners[1], corners[2], corners[2], corners[3], corners[0] };
        }

        /// <summary>
        /// A nine-slice layout: a 3x3 grid of quads with uneven cell sizes, each mapping its own part of the
        /// texture, as a sliced Image emits it. Each cell's UV rect is returned alongside its position rect.
        /// </summary>
        public static List<UIVertex> SlicedTriangles(Rect rect, out List<(Rect cell, Rect uv)> cells)
        {
            float[] xs = { rect.xMin, rect.xMin + 10f, rect.xMax - 20f, rect.xMax };
            float[] ys = { rect.yMin, rect.yMin + 15f, rect.yMax - 5f, rect.yMax };
            float[] us = { 0.5f, 0.6f, 0.7f, 0.8f };
            float[] vs = { 0.1f, 0.2f, 0.3f, 0.4f };
            var triangles = new List<UIVertex>();
            cells = new List<(Rect, Rect)>();
            for (var j = 0; j < 3; j++)
            {
                for (var i = 0; i < 3; i++)
                {
                    var cell = Rect.MinMaxRect(xs[i], ys[j], xs[i + 1], ys[j + 1]);
                    var uv = Rect.MinMaxRect(us[i], vs[j], us[i + 1], vs[j + 1]);
                    cells.Add((cell, uv));
                    triangles.AddRange(QuadTriangles(cell, uv, Color.white));
                }
            }

            return triangles;
        }

        public static List<UIVertex> ReadVertices(VertexHelper vertexHelper)
        {
            var result = new List<UIVertex>();
            var vertex = new UIVertex();
            for (var i = 0; i < vertexHelper.currentVertCount; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                result.Add(vertex);
            }

            return result;
        }

        /// <summary>Twice the signed area of a triangle: positive when counter-clockwise.</summary>
        public static float SignedArea(in UIVertex a, in UIVertex b, in UIVertex c)
        {
            var ab = b.position - a.position;
            var ac = c.position - a.position;
            return (ab.x * ac.y - ab.y * ac.x) * 0.5f;
        }

        public static float TotalArea(List<UIVertex> triangles)
        {
            var total = 0f;
            for (var i = 0; i + 2 < triangles.Count; i += 3)
            {
                total += Mathf.Abs(SignedArea(triangles[i], triangles[i + 1], triangles[i + 2]));
            }

            return total;
        }

        public static Vector2 Centroid(List<UIVertex> triangles, int first)
        {
            return (triangles[first].position + triangles[first + 1].position + triangles[first + 2].position) / 3f;
        }

        /// <summary>Asserts that every triangle keeps the winding of <paramref name="expectedSign"/>.</summary>
        public static void AssertWinding(List<UIVertex> triangles, float expectedSign)
        {
            for (var i = 0; i + 2 < triangles.Count; i += 3)
            {
                var area = SignedArea(triangles[i], triangles[i + 1], triangles[i + 2]);
                Assert.That(area * expectedSign, Is.GreaterThanOrEqualTo(-Tolerance),
                    "triangle " + i / 3 + " flipped its winding");
            }
        }

        public static bool HasVertexAt(List<UIVertex> vertices, Vector2 position, float tolerance = Tolerance)
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                if (Vector2.Distance(vertices[i].position, position) <= tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Maps a position inside <paramref name="cell"/> to the matching point of <paramref name="uv"/>.</summary>
        public static Vector2 ExpectedUv(Vector2 position, Rect cell, Rect uv)
        {
            return new Vector2(
                Mathf.Lerp(uv.xMin, uv.xMax, Mathf.InverseLerp(cell.xMin, cell.xMax, position.x)),
                Mathf.Lerp(uv.yMin, uv.yMax, Mathf.InverseLerp(cell.yMin, cell.yMax, position.y)));
        }

        public static List<float> Times(params float[] times) => new List<float>(times);
    }
}
