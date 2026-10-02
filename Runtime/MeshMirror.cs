using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.UI
{
    /// <summary>
    /// The engine-free half of <see cref="UIFlippable"/>: mirrors the vertex positions of a mesh about a centre
    /// point. Only <see cref="UIVertex.position"/> changes; colours, UVs, normals and tangents stay with their
    /// vertex, so the texture is mirrored together with the geometry.
    /// </summary>
    internal static class MeshMirror
    {
        /// <summary>
        /// Mirrors every vertex of <paramref name="vertexHelper"/> about <paramref name="center"/>.
        /// </summary>
        /// <param name="vertexHelper">The mesh to modify in place.</param>
        /// <param name="center">The point to mirror about, in the mesh's local space.</param>
        /// <param name="horizontal">Mirror left to right (negate x about <c>center.x</c>).</param>
        /// <param name="vertical">Mirror top to bottom (negate y about <c>center.y</c>).</param>
        internal static void Mirror(VertexHelper vertexHelper, Vector2 center, bool horizontal, bool vertical)
        {
            if (!horizontal && !vertical)
            {
                return;
            }

            var twiceX = 2f * center.x;
            var twiceY = 2f * center.y;
            var vertex = new UIVertex();
            var count = vertexHelper.currentVertCount;
            for (var i = 0; i < count; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                var position = vertex.position;
                if (horizontal)
                {
                    position.x = twiceX - position.x;
                }

                if (vertical)
                {
                    position.y = twiceY - position.y;
                }

                vertex.position = position;
                vertexHelper.SetUIVertex(vertex, i);
            }
        }
    }
}
