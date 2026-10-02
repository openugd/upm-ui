using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace OpenUGD.UI
{
    /// <summary>
    /// Maps a vertex position to the coordinate <see cref="GradientMeshEffect"/> passes to
    /// <see cref="Gradient.Evaluate"/>, for one mesh. The engine-free half of the effect's colouring.
    /// </summary>
    /// <remarks>
    /// <para>All four shapes are laid out on the mesh's vertex bounds, so the result does not depend on the
    /// <c>RectTransform</c> pivot.</para>
    /// <para>
    /// <b>Horizontal</b> and <b>Vertical</b>: 0 at the left (bottom) edge and 1 at the right (top) edge when the
    /// zoom is 1 and the offset 0. A zoom above 1 magnifies the gradient about the middle of the bounds; the
    /// offset slides it along the axis.
    /// </para>
    /// <para>
    /// <b>Radial</b>: the elliptical distance from the centre of the bounds, 0 at the centre and 1 on the
    /// ellipse inscribed in the bounds (the corners are at <c>sqrt(2)</c>). <b>Diamond</b>: the Manhattan
    /// distance on the same scale, 1 on the diamond whose corners touch the middle of each edge (the corners of
    /// the bounds are at 2). For both, the coordinate is divided by the zoom and the offset is subtracted.
    /// </para>
    /// </remarks>
    internal readonly struct GradientFrame
    {
        /// <summary>The shape of the gradient.</summary>
        internal readonly GradientMeshEffect.Type Type;

        /// <summary>The centre of the vertex bounds: the origin of the Radial and Diamond shapes.</summary>
        internal readonly Vector2 Center;

        /// <summary>The lower-left corner of the vertex bounds.</summary>
        internal readonly Vector2 Min;

        /// <summary>Horizontal and Vertical: the coordinate change per unit along the axis.</summary>
        internal readonly float InverseLength;

        /// <summary>Radial and Diamond: the normalised-distance change per unit along x and y.</summary>
        internal readonly Vector2 InverseRadius;

        /// <summary>Added to the shape's distance to give the coordinate.</summary>
        internal readonly float Bias;

        /// <summary>The largest distance (coordinate minus <see cref="Bias"/>) anywhere in the bounds.</summary>
        internal readonly float MaxDistance;

        /// <summary>The on-the-line tolerance, in mesh units, for cuts through this mesh.</summary>
        internal readonly float Epsilon;

        private GradientFrame(GradientMeshEffect.Type type, Vector2 center, Vector2 min, float inverseLength,
            Vector2 inverseRadius, float bias, float maxDistance, float epsilon)
        {
            Type = type;
            Center = center;
            Min = min;
            InverseLength = inverseLength;
            InverseRadius = inverseRadius;
            Bias = bias;
            MaxDistance = maxDistance;
            Epsilon = epsilon;
        }

        /// <summary>
        /// Whether the shape has no area to lay a Radial or Diamond gradient on (zero width or height).
        /// </summary>
        internal bool IsDegenerate => InverseRadius.x <= 0f || InverseRadius.y <= 0f;

        /// <summary>
        /// Lays out a gradient on a mesh.
        /// </summary>
        /// <param name="type">The gradient shape.</param>
        /// <param name="bounds">The vertex bounds of the mesh, in its local space.</param>
        /// <param name="offset">The gradient offset, -1 to 1.</param>
        /// <param name="zoom">The gradient zoom, greater than zero.</param>
        /// <returns>The frame.</returns>
        internal static GradientFrame Create(GradientMeshEffect.Type type, Rect bounds, float offset, float zoom)
        {
            var extent = Mathf.Max(Mathf.Max(Mathf.Abs(bounds.xMin), Mathf.Abs(bounds.xMax)),
                Mathf.Max(Mathf.Abs(bounds.yMin), Mathf.Abs(bounds.yMax)));
            var epsilon = 1e-5f + 1e-6f * extent;

            switch (type)
            {
                case GradientMeshEffect.Type.Horizontal:
                case GradientMeshEffect.Type.Vertical:
                {
                    var length = type == GradientMeshEffect.Type.Horizontal ? bounds.width : bounds.height;
                    var inverseLength = length > 0f ? 1f / (length * zoom) : 0f;
                    var zoomOffset = (1f - 1f / zoom) * 0.5f;
                    var bias = zoomOffset - offset * (1f - zoomOffset);
                    return new GradientFrame(type, bounds.center, bounds.min, inverseLength, Vector2.zero, bias,
                        length * inverseLength, epsilon);
                }
                default:
                {
                    var inverseRadius = new Vector2(
                        bounds.width > 0f ? 2f / (bounds.width * zoom) : 0f,
                        bounds.height > 0f ? 2f / (bounds.height * zoom) : 0f);
                    var corner = new Vector2(bounds.width * 0.5f * inverseRadius.x,
                        bounds.height * 0.5f * inverseRadius.y);
                    var maxDistance = type == GradientMeshEffect.Type.Diamond
                        ? corner.x + corner.y
                        : Mathf.Sqrt(corner.x * corner.x + corner.y * corner.y);
                    return new GradientFrame(type, bounds.center, bounds.min, 0f, inverseRadius, -offset,
                        maxDistance, epsilon);
                }
            }
        }

        /// <summary>
        /// The gradient coordinate of a position: the value to pass to <see cref="Gradient.Evaluate"/>, which
        /// clamps it to 0..1.
        /// </summary>
        /// <param name="position">A vertex position in the mesh's local space.</param>
        /// <returns>The coordinate.</returns>
        internal float Evaluate(Vector3 position) => Distance(position.x, position.y) + Bias;

        /// <summary>
        /// The shape's distance at a point: <see cref="Evaluate"/> without the <see cref="Bias"/>.
        /// </summary>
        /// <param name="x">The x coordinate, in the mesh's local space.</param>
        /// <param name="y">The y coordinate, in the mesh's local space.</param>
        /// <returns>The distance, never negative.</returns>
        internal float Distance(float x, float y)
        {
            switch (Type)
            {
                case GradientMeshEffect.Type.Horizontal:
                    return (x - Min.x) * InverseLength;
                case GradientMeshEffect.Type.Vertical:
                    return (y - Min.y) * InverseLength;
                case GradientMeshEffect.Type.Diamond:
                    return Mathf.Abs((x - Center.x) * InverseRadius.x) + Mathf.Abs((y - Center.y) * InverseRadius.y);
                default:
                {
                    var u = (x - Center.x) * InverseRadius.x;
                    var v = (y - Center.y) * InverseRadius.y;
                    return Mathf.Sqrt(u * u + v * v);
                }
            }
        }
    }

    /// <summary>
    /// Adds vertices to a mesh so that per-vertex colours, interpolated across each triangle, follow a gradient:
    /// the engine-free half of <see cref="GradientMeshEffect.ModifyVertices"/>.
    /// </summary>
    /// <remarks>
    /// <para>The mesh keeps its outline, and every attribute of a new vertex (UVs included) is interpolated from
    /// the triangle it was cut from, so sliced, tiled and atlas sprites keep their texturing.</para>
    /// <para>
    /// <b>Horizontal</b>/<b>Vertical</b>: the mesh is cut along every line where the coordinate equals a key
    /// time of the gradient, so each colour and alpha key lands on a row of vertices. Between two keys the
    /// coordinate is linear, so the result is exact.
    /// </para>
    /// <para>
    /// <b>Diamond</b>: the distance is linear inside each quadrant around the centre, so the mesh is cut along
    /// the two axes through the centre, then inside each quadrant along the diamond edge of every key. Exact.
    /// </para>
    /// <para>
    /// <b>Radial</b>: the mesh is cut into <see cref="RadialSectors"/> wedges around the centre, then inside each
    /// wedge along the chord of every key's ellipse. The colour is exact at the centre, at every vertex and on
    /// the wedge edges, and elsewhere within 0.5% of the distance (1 / cos(pi/32) - 1).
    /// </para>
    /// </remarks>
    internal static class GradientTessellator
    {
        /// <summary>The number of wedges a Radial gradient cuts the mesh into.</summary>
        internal const int RadialSectors = 32;

        private const int RadialLines = RadialSectors / 2;
        private const float SectorAngle = 2f * Mathf.PI / RadialSectors;
        private const float DistanceTolerance = 1e-4f;

        // cos(SectorAngle / 2): how far the chord between two points of a circle, one wedge apart, sits from the
        // centre, relative to the radius.
        private static readonly float ChordDistance = Mathf.Cos(SectorAngle * 0.5f);

        /// <summary>
        /// Whether <see cref="Tessellate"/> would change the mesh: always for a Radial or Diamond gradient on a
        /// mesh with area, and for Horizontal or Vertical only when a key falls strictly inside the mesh.
        /// </summary>
        /// <param name="frame">The gradient laid out on the mesh.</param>
        /// <param name="keyTimes">The gradient's colour and alpha key times, sorted, without duplicates.</param>
        /// <returns><c>true</c> if the mesh needs extra vertices.</returns>
        internal static bool NeedsTessellation(in GradientFrame frame, List<float> keyTimes)
        {
            switch (frame.Type)
            {
                case GradientMeshEffect.Type.Horizontal:
                case GradientMeshEffect.Type.Vertical:
                    for (var i = 0; i < keyTimes.Count; i++)
                    {
                        if (IsInside(frame, keyTimes[i] - frame.Bias))
                        {
                            return true;
                        }
                    }

                    return false;
                default:
                    return !frame.IsDegenerate;
            }
        }

        /// <summary>
        /// Replaces the triangles of <paramref name="mesh"/> with a finer triangulation of the same outline, with
        /// vertices where the gradient needs them.
        /// </summary>
        /// <param name="mesh">A triangle list (three vertices per triangle); rewritten in place.</param>
        /// <param name="frame">The gradient laid out on the mesh.</param>
        /// <param name="keyTimes">The gradient's colour and alpha key times, sorted, without duplicates.</param>
        internal static void Tessellate(List<UIVertex> mesh, in GradientFrame frame, List<float> keyTimes)
        {
            var levels = ListPool<float>.Get();
            var spareMesh = ListPool<UIVertex>.Get();
            var piece = ListPool<UIVertex>.Get();
            var pieceBack = ListPool<UIVertex>.Get();
            try
            {
                for (var i = 0; i < keyTimes.Count; i++)
                {
                    var level = keyTimes[i] - frame.Bias;
                    if (IsInside(frame, level))
                    {
                        levels.Add(level);
                    }
                }

                // The cuts ping-pong between the caller's list and a pooled one.
                var current = mesh;
                var spare = spareMesh;
                switch (frame.Type)
                {
                    case GradientMeshEffect.Type.Horizontal:
                    case GradientMeshEffect.Type.Vertical:
                    {
                        var horizontal = frame.Type == GradientMeshEffect.Type.Horizontal;
                        var normal = horizontal ? Vector2.right : Vector2.up;
                        var min = horizontal ? frame.Min.x : frame.Min.y;
                        for (var i = 0; i < levels.Count; i++)
                        {
                            CutAll(ref current, ref spare, normal, min + levels[i] / frame.InverseLength,
                                frame.Epsilon);
                        }

                        break;
                    }
                    case GradientMeshEffect.Type.Diamond:
                    {
                        if (frame.IsDegenerate)
                        {
                            break;
                        }

                        CutAll(ref current, ref spare, Vector2.right, frame.Center.x, frame.Epsilon);
                        CutAll(ref current, ref spare, Vector2.up, frame.Center.y, frame.Epsilon);
                        CutRegions(ref current, ref spare, piece, pieceBack, frame, levels);
                        break;
                    }
                    default:
                    {
                        if (frame.IsDegenerate)
                        {
                            break;
                        }

                        for (var k = 0; k < RadialLines; k++)
                        {
                            // A line through the centre at angle k * SectorAngle, in the normalised space where
                            // the ellipse is a circle.
                            var angle = k * SectorAngle;
                            var normal = new Vector2(-Mathf.Sin(angle) * frame.InverseRadius.x,
                                Mathf.Cos(angle) * frame.InverseRadius.y);
                            CutAllNormalised(ref current, ref spare, normal, Vector2.Dot(normal, frame.Center),
                                frame.Epsilon);
                        }

                        CutRegions(ref current, ref spare, piece, pieceBack, frame, levels);
                        break;
                    }
                }

                if (!ReferenceEquals(current, mesh))
                {
                    mesh.Clear();
                    Append(current, mesh);
                }
            }
            finally
            {
                ListPool<UIVertex>.Release(pieceBack);
                ListPool<UIVertex>.Release(piece);
                ListPool<UIVertex>.Release(spareMesh);
                ListPool<float>.Release(levels);
            }
        }

        // List<T>.AddRange copies through a temporary array on Mono's class library; a loop does not allocate.
        private static void Append(List<UIVertex> source, List<UIVertex> target)
        {
            for (var i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }
        }

        private static bool IsInside(in GradientFrame frame, float level) =>
            level > DistanceTolerance && level < frame.MaxDistance - DistanceTolerance;

        private static void CutAll(ref List<UIVertex> current, ref List<UIVertex> spare, Vector2 normal,
            float distance, float epsilon)
        {
            spare.Clear();
            TriangleCutter.Cut(current, spare, normal, distance, epsilon);
            var swap = current;
            current = spare;
            spare = swap;
        }

        // Normalises the line first, so that the epsilon stays in mesh units.
        private static void CutAllNormalised(ref List<UIVertex> current, ref List<UIVertex> spare, Vector2 normal,
            float distance, float epsilon)
        {
            var length = normal.magnitude;
            if (length <= 0f)
            {
                return;
            }

            CutAll(ref current, ref spare, normal / length, distance / length, epsilon);
        }

        // After the cuts through the centre, every triangle lies in one quadrant (Diamond) or one wedge (Radial),
        // where the iso-line of each level is one straight segment. Each triangle is cut by its own region's lines.
        private static void CutRegions(ref List<UIVertex> current, ref List<UIVertex> spare, List<UIVertex> piece,
            List<UIVertex> pieceBack, in GradientFrame frame, List<float> levels)
        {
            if (levels.Count == 0)
            {
                return;
            }

            spare.Clear();
            var count = current.Count - current.Count % 3;
            for (var i = 0; i < count; i += 3)
            {
                var a = current[i];
                var b = current[i + 1];
                var c = current[i + 2];
                var u = ((a.position.x + b.position.x + c.position.x) / 3f - frame.Center.x) * frame.InverseRadius.x;
                var v = ((a.position.y + b.position.y + c.position.y) / 3f - frame.Center.y) * frame.InverseRadius.y;

                // The region's iso-line direction, in normalised space: normal . (u, v) == level * scale.
                Vector2 regionNormal;
                float scale;
                if (frame.Type == GradientMeshEffect.Type.Diamond)
                {
                    regionNormal = new Vector2(u >= 0f ? 1f : -1f, v >= 0f ? 1f : -1f);
                    scale = 1f;
                }
                else
                {
                    var angle = Mathf.Atan2(v, u);
                    if (angle < 0f)
                    {
                        angle += 2f * Mathf.PI;
                    }

                    var sector = Mathf.Clamp((int)(angle / SectorAngle), 0, RadialSectors - 1);
                    var bisector = (sector + 0.5f) * SectorAngle;
                    regionNormal = new Vector2(Mathf.Cos(bisector), Mathf.Sin(bisector));
                    scale = ChordDistance;
                }

                // Back to mesh space: normal . (p - centre) == level * scale.
                var normal = new Vector2(regionNormal.x * frame.InverseRadius.x, regionNormal.y * frame.InverseRadius.y);
                var length = normal.magnitude;
                normal /= length;
                var centreDistance = Vector2.Dot(normal, frame.Center);

                piece.Clear();
                piece.Add(a);
                piece.Add(b);
                piece.Add(c);
                var pieces = piece;
                var piecesSpare = pieceBack;
                for (var l = 0; l < levels.Count; l++)
                {
                    piecesSpare.Clear();
                    TriangleCutter.Cut(pieces, piecesSpare, normal, centreDistance + levels[l] * scale / length,
                        frame.Epsilon);
                    var swap = pieces;
                    pieces = piecesSpare;
                    piecesSpare = swap;
                }

                Append(pieces, spare);
            }

            var swapRegions = current;
            current = spare;
            spare = swapRegions;
        }
    }
}
