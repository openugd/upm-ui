using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace OpenUGD.UI.Tests
{
    // Engine-free: the mirror UIFlippable applies, driven directly with a VertexHelper.
    public class MeshMirrorTests
    {
        private static readonly Vector2 Center = new Vector2(60f, 25f);

        private static VertexHelper Triangle()
        {
            var vertexHelper = new VertexHelper();
            vertexHelper.AddVert(MeshTestUtility.Vertex(10f, 5f, new Vector2(0f, 0f), new Color32(255, 0, 0, 255)));
            vertexHelper.AddVert(MeshTestUtility.Vertex(30f, 5f, new Vector2(1f, 0f), new Color32(0, 255, 0, 255)));
            vertexHelper.AddVert(MeshTestUtility.Vertex(10f, 45f, new Vector2(0f, 1f), new Color32(0, 0, 255, 255)));
            vertexHelper.AddTriangle(0, 1, 2);
            return vertexHelper;
        }

        [Test]
        public void Horizontal_MirrorsXAboutTheCentre()
        {
            using var vertexHelper = Triangle();

            MeshMirror.Mirror(vertexHelper, Center, true, false);

            var vertices = MeshTestUtility.ReadVertices(vertexHelper);
            Assert.That(vertices[0].position, Is.EqualTo(new Vector3(110f, 5f, 0f)));
            Assert.That(vertices[1].position, Is.EqualTo(new Vector3(90f, 5f, 0f)));
            Assert.That(vertices[2].position, Is.EqualTo(new Vector3(110f, 45f, 0f)));
        }

        [Test]
        public void Vertical_MirrorsYAboutTheCentre()
        {
            using var vertexHelper = Triangle();

            MeshMirror.Mirror(vertexHelper, Center, false, true);

            var vertices = MeshTestUtility.ReadVertices(vertexHelper);
            Assert.That(vertices[0].position, Is.EqualTo(new Vector3(10f, 45f, 0f)));
            Assert.That(vertices[1].position, Is.EqualTo(new Vector3(30f, 45f, 0f)));
            Assert.That(vertices[2].position, Is.EqualTo(new Vector3(10f, 5f, 0f)));
        }

        [Test]
        public void Both_MirrorsThroughTheCentre()
        {
            using var vertexHelper = Triangle();

            MeshMirror.Mirror(vertexHelper, Center, true, true);

            var vertices = MeshTestUtility.ReadVertices(vertexHelper);
            Assert.That(vertices[0].position, Is.EqualTo(new Vector3(110f, 45f, 0f)));
            Assert.That(vertices[1].position, Is.EqualTo(new Vector3(90f, 45f, 0f)));
            Assert.That(vertices[2].position, Is.EqualTo(new Vector3(110f, 5f, 0f)));
        }

        [Test]
        public void Neither_LeavesTheMeshUnchanged()
        {
            using var vertexHelper = Triangle();
            var before = MeshTestUtility.ReadVertices(vertexHelper);

            MeshMirror.Mirror(vertexHelper, Center, false, false);

            Assert.That(MeshTestUtility.ReadVertices(vertexHelper), Is.EqualTo(before));
        }

        [Test]
        public void KeepsUvsColoursAndTriangles()
        {
            using var vertexHelper = Triangle();
            var before = MeshTestUtility.ReadVertices(vertexHelper);

            MeshMirror.Mirror(vertexHelper, Center, true, true);

            var after = MeshTestUtility.ReadVertices(vertexHelper);
            for (var i = 0; i < before.Count; i++)
            {
                Assert.That(after[i].uv0, Is.EqualTo(before[i].uv0), "uv0 of vertex " + i);
                Assert.That(after[i].color, Is.EqualTo(before[i].color), "colour of vertex " + i);
                Assert.That(after[i].normal, Is.EqualTo(before[i].normal), "normal of vertex " + i);
            }

            Assert.That(vertexHelper.currentVertCount, Is.EqualTo(3));
            Assert.That(vertexHelper.currentIndexCount, Is.EqualTo(3));
        }

        [Test]
        public void MirroringTwice_RestoresTheMesh()
        {
            using var vertexHelper = Triangle();
            var before = MeshTestUtility.ReadVertices(vertexHelper);

            MeshMirror.Mirror(vertexHelper, Center, true, true);
            MeshMirror.Mirror(vertexHelper, Center, true, true);

            Assert.That(MeshTestUtility.ReadVertices(vertexHelper), Is.EqualTo(before));
        }
    }
}
