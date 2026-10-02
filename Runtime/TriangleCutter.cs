using System.Collections.Generic;
using UnityEngine;

namespace OpenUGD.UI
{
    /// <summary>
    /// Cuts triangles along straight lines in the XY plane, interpolating every vertex attribute, so a mesh can
    /// get extra vertices where a vertex-colour effect needs them without changing its shape or its texturing.
    /// </summary>
    /// <remarks>
    /// Meshes are triangle lists: three consecutive <see cref="UIVertex"/> values per triangle, the layout
    /// <see cref="UnityEngine.UI.VertexHelper.GetUIVertexStream"/> produces. A cut keeps each piece's winding.
    /// The class makes no engine calls and allocates nothing beyond the growth of the target list.
    /// </remarks>
    internal static class TriangleCutter
    {
        /// <summary>
        /// Cuts every triangle of <paramref name="source"/> by the line
        /// <c>dot(normal, position.xy) == distance</c> and appends the pieces to <paramref name="target"/>.
        /// </summary>
        /// <param name="source">The triangles to cut. Not modified.</param>
        /// <param name="target">Receives the pieces, three vertices per triangle.</param>
        /// <param name="normal">The line's normal. Its length scales <paramref name="epsilon"/>.</param>
        /// <param name="distance">The line's offset along <paramref name="normal"/>.</param>
        /// <param name="epsilon">
        /// A vertex whose signed distance is within this tolerance counts as lying on the line, so a triangle
        /// that only touches the line is copied unchanged instead of producing slivers.
        /// </param>
        internal static void Cut(List<UIVertex> source, List<UIVertex> target, Vector2 normal, float distance,
            float epsilon)
        {
            var count = source.Count - source.Count % 3;
            for (var i = 0; i < count; i += 3)
            {
                CutTriangle(source[i], source[i + 1], source[i + 2], normal, distance, epsilon, target);
            }
        }

        /// <summary>
        /// Cuts one triangle by the line <c>dot(normal, position.xy) == distance</c> and appends the one, two or
        /// three triangles that result to <paramref name="target"/>.
        /// </summary>
        /// <param name="a">The triangle's first vertex.</param>
        /// <param name="b">The triangle's second vertex.</param>
        /// <param name="c">The triangle's third vertex.</param>
        /// <param name="normal">The line's normal.</param>
        /// <param name="distance">The line's offset along <paramref name="normal"/>.</param>
        /// <param name="epsilon">The on-the-line tolerance; see <see cref="Cut"/>.</param>
        /// <param name="target">Receives the pieces.</param>
        internal static void CutTriangle(in UIVertex a, in UIVertex b, in UIVertex c, Vector2 normal, float distance,
            float epsilon, List<UIVertex> target)
        {
            var da = normal.x * a.position.x + normal.y * a.position.y - distance;
            var db = normal.x * b.position.x + normal.y * b.position.y - distance;
            var dc = normal.x * c.position.x + normal.y * c.position.y - distance;

            var anyAbove = da > epsilon || db > epsilon || dc > epsilon;
            var anyBelow = da < -epsilon || db < -epsilon || dc < -epsilon;
            if (!anyAbove || !anyBelow)
            {
                target.Add(a);
                target.Add(b);
                target.Add(c);
                return;
            }

            ClipToSide(a, b, c, da, db, dc, 1f, epsilon, target);
            ClipToSide(a, b, c, da, db, dc, -1f, epsilon, target);
        }

        /// <summary>
        /// Interpolates every attribute of two vertices: position, normal, tangent, colour and the four UV
        /// channels.
        /// </summary>
        /// <param name="from">The vertex at <paramref name="t"/> = 0.</param>
        /// <param name="to">The vertex at <paramref name="t"/> = 1.</param>
        /// <param name="t">The interpolation factor; not clamped.</param>
        /// <returns>The interpolated vertex.</returns>
        internal static UIVertex Lerp(in UIVertex from, in UIVertex to, float t)
        {
            var result = new UIVertex();
            result.position = Vector3.LerpUnclamped(from.position, to.position, t);
            result.normal = Vector3.LerpUnclamped(from.normal, to.normal, t);
            result.tangent = Vector4.LerpUnclamped(from.tangent, to.tangent, t);
            result.color = Color32.LerpUnclamped(from.color, to.color, t);
            result.uv0 = Vector4.LerpUnclamped(from.uv0, to.uv0, t);
            result.uv1 = Vector4.LerpUnclamped(from.uv1, to.uv1, t);
            result.uv2 = Vector4.LerpUnclamped(from.uv2, to.uv2, t);
            result.uv3 = Vector4.LerpUnclamped(from.uv3, to.uv3, t);
            return result;
        }

        // Sutherland-Hodgman against one half-plane. A triangle clipped by a half-plane is a convex polygon of at
        // most four vertices, emitted as a fan, which keeps the original winding.
        private static void ClipToSide(in UIVertex a, in UIVertex b, in UIVertex c, float da, float db, float dc,
            float side, float epsilon, List<UIVertex> target)
        {
            var polygon = new Polygon();
            ClipEdge(ref polygon, a, b, da, db, side, epsilon);
            ClipEdge(ref polygon, b, c, db, dc, side, epsilon);
            ClipEdge(ref polygon, c, a, dc, da, side, epsilon);
            polygon.EmitFan(target);
        }

        private static void ClipEdge(ref Polygon polygon, in UIVertex start, in UIVertex end, float dStart, float dEnd,
            float side, float epsilon)
        {
            var sStart = dStart * side;
            var sEnd = dEnd * side;
            if (sStart >= -epsilon)
            {
                polygon.Add(start);
            }

            if ((sStart > epsilon && sEnd < -epsilon) || (sStart < -epsilon && sEnd > epsilon))
            {
                polygon.Add(Intersect(start, end, dStart, dEnd));
            }
        }

        // The crossing point of an edge, computed from a canonical endpoint order so that the two triangles sharing
        // an edge (which walk it in opposite directions) get bit-identical vertices and the cut leaves no crack.
        private static UIVertex Intersect(in UIVertex start, in UIVertex end, float dStart, float dEnd)
        {
            if (Precedes(end.position, start.position))
            {
                return Lerp(end, start, dEnd / (dEnd - dStart));
            }

            return Lerp(start, end, dStart / (dStart - dEnd));
        }

        private static bool Precedes(Vector3 p, Vector3 q)
        {
            if (p.x != q.x)
            {
                return p.x < q.x;
            }

            if (p.y != q.y)
            {
                return p.y < q.y;
            }

            return p.z < q.z;
        }

        // A fixed-capacity polygon held on the stack, so clipping allocates nothing.
        private struct Polygon
        {
            private UIVertex _v0;
            private UIVertex _v1;
            private UIVertex _v2;
            private UIVertex _v3;
            private int _count;

            public void Add(in UIVertex vertex)
            {
                switch (_count)
                {
                    case 0:
                        _v0 = vertex;
                        break;
                    case 1:
                        _v1 = vertex;
                        break;
                    case 2:
                        _v2 = vertex;
                        break;
                    case 3:
                        _v3 = vertex;
                        break;
                    default:
                        // Unreachable: a triangle clipped by one half-plane has at most four vertices.
                        return;
                }

                _count++;
            }

            public void EmitFan(List<UIVertex> target)
            {
                if (_count < 3)
                {
                    return;
                }

                target.Add(_v0);
                target.Add(_v1);
                target.Add(_v2);
                if (_count == 4)
                {
                    target.Add(_v0);
                    target.Add(_v2);
                    target.Add(_v3);
                }
            }
        }
    }
}
