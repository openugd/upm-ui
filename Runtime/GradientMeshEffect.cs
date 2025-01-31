using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(RectTransform), typeof(Graphic))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Effects/Gradient")]
    public class GradientMeshEffect : BaseMeshEffect
    {
        #region Public Method

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper.currentVertCount == 0)
            {
                return;
            }

            var vertices = new List<UIVertex>();

            vertexHelper.GetUIVertexStream(vertices);

            var vCount = vertices.Count;
            switch (GradientType)
            {
                case Type.Horizontal:
                case Type.Vertical:
                {
                    var bounds = GetBounds(vertices);
                    var min = bounds.xMin;
                    var w = bounds.width;
                    Func<UIVertex, float> getPosition = v => v.position.x;

                    if (GradientType == Type.Vertical)
                    {
                        min = bounds.yMin;
                        w = bounds.height;
                        getPosition = v => v.position.y;
                    }

                    var width = w == 0.0f ? 0.0f : 1.0f / w / GradientZoom;
                    var zoomOffset = (1.0f - 1.0f / GradientZoom) * 0.5f;
                    var offset = GradientOffset * (1.0f - zoomOffset) - zoomOffset;

                    if (ModifyVertices)
                    {
                        SplitTrianglesAtGradientStops(vertices, bounds, zoomOffset, vertexHelper);
                    }

                    var vertex = new UIVertex();
                    for (var i = 0; i < vertexHelper.currentVertCount; i++)
                    {
                        vertexHelper.PopulateUIVertex(ref vertex, i);
                        if (modifyTangents)
                        {
                            vertex.tangent = BlendColor(vertex.color,
                                GradientColor.Evaluate((getPosition(vertex) - min) * width - offset));
                        }
                        else
                        {
                            vertex.color = BlendColor(vertex.color,
                                GradientColor.Evaluate((getPosition(vertex) - min) * width - offset));
                        }

                        vertexHelper.SetUIVertex(vertex, i);
                    }
                }
                    break;

                case Type.Diamond:
                {
                    var bounds = GetBounds(vertices);

                    var height = bounds.height == 0.0f ? 0.0f : 1.0f / bounds.height / GradientZoom;
                    var radius = bounds.center.y / 2.0f;
                    var center = (Vector3.right + Vector3.up) * radius + Vector3.forward * vertices[0].position.z;

                    if (ModifyVertices)
                    {
                        vertexHelper.Clear();
                        for (var i = 0; i < vCount; i++)
                        {
                            vertexHelper.AddVert(vertices[i]);
                        }

                        var centralVertex = new UIVertex();
                        centralVertex.position = center;
                        centralVertex.normal = vertices[0].normal;
                        centralVertex.uv0 = new Vector2(0.5f, 0.5f);
                        centralVertex.color = Color.white;
                        vertexHelper.AddVert(centralVertex);

                        for (var i = 1; i < vCount; i++)
                        {
                            vertexHelper.AddTriangle(i - 1, i, vCount);
                        }

                        vertexHelper.AddTriangle(0, vCount - 1, vCount);
                    }

                    var vertex = new UIVertex();

                    for (var i = 0; i < vertexHelper.currentVertCount; i++)
                    {
                        vertexHelper.PopulateUIVertex(ref vertex, i);

                        vertex.color = BlendColor(vertex.color,
                            GradientColor.Evaluate(Vector3.Distance(vertex.position, center) * height -
                                                   GradientOffset));
                        vertexHelper.SetUIVertex(vertex, i);
                    }
                }
                    break;

                case Type.Radial:
                {
                    var bounds = GetBounds(vertices);

                    var width = bounds.width == 0.0f ? 0.0f : 1.0f / bounds.width / GradientZoom;
                    var height = bounds.height == 0.0f ? 0.0f : 1.0f / bounds.height / GradientZoom;

                    if (ModifyVertices)
                    {
                        vertexHelper.Clear();

                        var radiusX = bounds.width / 2.0f;
                        var radiusY = bounds.height / 2.0f;
                        var centralVertex = new UIVertex();
                        centralVertex.position = Vector3.right * bounds.center.x + Vector3.up * bounds.center.y +
                                                 Vector3.forward * vertices[0].position.z;
                        centralVertex.normal = vertices[0].normal;
                        centralVertex.uv0 = new Vector2(0.5f, 0.5f);
                        centralVertex.color = Color.white;

                        var steps = 64;
                        for (var i = 0; i < steps; i++)
                        {
                            var curVertex = new UIVertex();
                            var angle = i * 360.0f / steps;
                            var cosX = Mathf.Cos(Mathf.Deg2Rad * angle);
                            var cosY = Mathf.Sin(Mathf.Deg2Rad * angle);

                            curVertex.position = Vector3.right * cosX * radiusX + Vector3.up * cosY * radiusY +
                                                 Vector3.forward * vertices[0].position.z;
                            curVertex.normal = vertices[0].normal;
                            curVertex.uv0 = new Vector2((cosX + 1) * 0.5f, (cosY + 1) * 0.5f);
                            curVertex.color = Color.white;
                            vertexHelper.AddVert(curVertex);
                        }

                        vertexHelper.AddVert(centralVertex);

                        for (var i = 1; i < steps; i++)
                        {
                            vertexHelper.AddTriangle(i - 1, i, steps);
                        }

                        vertexHelper.AddTriangle(0, steps - 1, steps);
                    }

                    var vertex = new UIVertex();

                    for (var i = 0; i < vertexHelper.currentVertCount; i++)
                    {
                        vertexHelper.PopulateUIVertex(ref vertex, i);

                        vertex.color = BlendColor(vertex.color, GradientColor.Evaluate(Mathf.Sqrt(
                            Mathf.Pow(Mathf.Abs(vertex.position.x - bounds.center.x) * width, 2.0f) +
                            Mathf.Pow(Mathf.Abs(vertex.position.y - bounds.center.y) * height, 2.0f)
                        ) * 2.0f - GradientOffset));

                        vertexHelper.SetUIVertex(vertex, i);
                    }
                }
                    break;
            }
        }

        #endregion

        #region Serialize Fields

        [FormerlySerializedAs("gradient_type")] [SerializeField]
        private Type gradientType = Type.Horizontal;

        [FormerlySerializedAs("blend_mode")] [SerializeField]
        private Blend blendMode = Blend.Multiply;

        [FormerlySerializedAs("modify_vertices")]
        [Tooltip(
            "Add vertices to display complex gradients. Turn off if your shape is already very complex, like text.")]
        [SerializeField]
        private bool modifyVertices = true;

        [FormerlySerializedAs("modify_tangents")] [SerializeField]
        private bool modifyTangents;

        [FormerlySerializedAs("gradient_offset")] [SerializeField] [Range(-1.0f, 1.0f)]
        private float gradientOffset;

        [FormerlySerializedAs("gradient_zoom")] [SerializeField] [Range(0.1f, 10.0f)]
        private float gradientZoom = 1.0f;

        [FormerlySerializedAs("gradient_color")] [SerializeField]
        private Gradient gradientColor = new() {
            colorKeys = new[]
                { new GradientColorKey(Color.black, 0.0f), new GradientColorKey(Color.white, 1.0f) }
        };

        #endregion

        #region Public Fields

        public Blend BlendMode {
            get => blendMode;
            set {
                blendMode = value;
                graphic.SetVerticesDirty();
            }
        }

        public Gradient GradientColor {
            get => gradientColor;
            set {
                gradientColor = value;
                graphic.SetVerticesDirty();
            }
        }

        public Type GradientType {
            get => gradientType;
            set {
                gradientType = value;
                graphic.SetVerticesDirty();
            }
        }

        public bool ModifyVertices {
            get => modifyVertices;
            set {
                modifyVertices = value;
                graphic.SetVerticesDirty();
            }
        }

        public float GradientOffset {
            get => gradientOffset;
            set {
                gradientOffset = Mathf.Clamp(value, -1.0f, 1.0f);
                graphic.SetVerticesDirty();
            }
        }

        public float GradientZoom {
            get => gradientZoom;
            set {
                gradientZoom = Mathf.Clamp(value, 0.1f, 10.0f);
                graphic.SetVerticesDirty();
            }
        }

        #endregion

        #region Private Method

        private static Rect GetBounds(List<UIVertex> vertices)
        {
            var left = vertices[0].position.x;
            var right = left;
            var bottom = vertices[0].position.y;
            var top = bottom;

            for (var i = vertices.Count - 1; i >= 1; --i)
            {
                var x = vertices[i].position.x;
                var y = vertices[i].position.y;

                if (x > right)
                {
                    right = x;
                }
                else if (x < left)
                {
                    left = x;
                }

                if (y > top)
                {
                    top = y;
                }
                else if (y < bottom)
                {
                    bottom = y;
                }
            }

            return new Rect(left, bottom, right - left, top - bottom);
        }

        private void SplitTrianglesAtGradientStops(List<UIVertex> vertexList, Rect bounds, float zoomOffset,
            VertexHelper helper)
        {
            var stops = FindStops(zoomOffset, bounds);
            if (stops.Count <= 0)
            {
                return;
            }

            helper.Clear();

            var vCount = vertexList.Count;
            for (var i = 0; i < vCount; i += 3)
            {
                var positions = GetPositions(vertexList, i);
                var originIndices = new List<int>(3);
                var starts = new List<UIVertex>(3);
                var ends = new List<UIVertex>(2);

                for (var s = 0; s < stops.Count; s++)
                {
                    var initialCount = helper.currentVertCount;
                    var hadEnds = ends.Count > 0;
                    var earlyStart = false;

                    // find any start vertices for this stop
                    for (var p = 0; p < 3; p++)
                    {
                        if (!originIndices.Contains(p) && positions[p] < stops[s])
                        {
                            // make sure the first index crosses the stop
                            var p1 = (p + 1) % 3;
                            var start = vertexList[p + i];
                            if (positions[p1] > stops[s])
                            {
                                originIndices.Insert(0, p);
                                starts.Insert(0, start);
                                earlyStart = true;
                            }
                            else
                            {
                                originIndices.Add(p);
                                starts.Add(start);
                            }
                        }
                    }

                    // bail if all before or after the stop
                    if (originIndices.Count == 0)
                    {
                        continue;
                    }

                    if (originIndices.Count == 3)
                    {
                        break;
                    }

                    // report any start vertices
                    foreach (var start in starts)
                    {
                        helper.AddVert(start);
                    }

                    // make two ends, splitting at the stop
                    ends.Clear();
                    foreach (var index in originIndices)
                    {
                        var oppositeIndex = (index + 1) % 3;
                        if (positions[oppositeIndex] < stops[s])
                        {
                            oppositeIndex = (oppositeIndex + 1) % 3;
                        }

                        ends.Add(CreateSplitVertex(vertexList[index + i], vertexList[oppositeIndex + i], stops[s]));
                    }

                    if (ends.Count == 1)
                    {
                        var oppositeIndex = (originIndices[0] + 2) % 3;
                        ends.Add(CreateSplitVertex(vertexList[originIndices[0] + i], vertexList[oppositeIndex + i],
                            stops[s]));
                    }

                    // report end vertices
                    foreach (var end in ends)
                    {
                        helper.AddVert(end);
                    }

                    // make triangles
                    if (hadEnds)
                    {
                        helper.AddTriangle(initialCount - 2, initialCount, initialCount + 1);
                        helper.AddTriangle(initialCount - 2, initialCount + 1, initialCount - 1);
                        if (starts.Count > 0)
                        {
                            if (earlyStart)
                            {
                                helper.AddTriangle(initialCount - 2, initialCount + 3, initialCount);
                            }
                            else
                            {
                                helper.AddTriangle(initialCount + 1, initialCount + 3, initialCount - 1);
                            }
                        }
                    }
                    else
                    {
                        var vertexCount = helper.currentVertCount;
                        helper.AddTriangle(initialCount, vertexCount - 2, vertexCount - 1);

                        if (starts.Count > 1)
                        {
                            helper.AddTriangle(initialCount, vertexCount - 1, initialCount + 1);
                        }
                    }

                    starts.Clear();
                }

                // clean up after looping through gradient stops
                if (ends.Count > 0)
                {
                    // find any final vertices after the gradient stops
                    if (starts.Count == 0)
                    {
                        for (var p = 0; p < 3; p++)
                        {
                            if (!originIndices.Contains(p) && positions[p] > stops[stops.Count - 1])
                            {
                                var p1 = (p + 1) % 3;
                                var end = vertexList[p + i];
                                if (positions[p1] > stops[stops.Count - 1])
                                {
                                    starts.Insert(0, end);
                                }
                                else
                                {
                                    starts.Add(end);
                                }
                            }
                        }
                    }

                    // report final vertices
                    foreach (var start in starts)
                    {
                        helper.AddVert(start);
                    }

                    // make final triangle(s)
                    var vertexCount = helper.currentVertCount;
                    if (starts.Count > 1)
                    {
                        helper.AddTriangle(vertexCount - 4, vertexCount - 2, vertexCount - 1);
                        helper.AddTriangle(vertexCount - 4, vertexCount - 1, vertexCount - 3);
                    }
                    else if (starts.Count > 0)
                    {
                        helper.AddTriangle(vertexCount - 3, vertexCount - 1, vertexCount - 2);
                    }
                }
                else
                {
                    // if the triangle wasn't split, add it as-is
                    helper.AddVert(vertexList[i]);
                    helper.AddVert(vertexList[i + 1]);
                    helper.AddVert(vertexList[i + 2]);
                    var vertexCount = helper.currentVertCount;
                    helper.AddTriangle(vertexCount - 3, vertexCount - 2, vertexCount - 1);
                }
            }
        }

        private float[] GetPositions(List<UIVertex> vertexList, int index)
        {
            var positions = new float[3];
            if (GradientType == Type.Horizontal)
            {
                positions[0] = vertexList[index].position.x;
                positions[1] = vertexList[index + 1].position.x;
                positions[2] = vertexList[index + 2].position.x;
            }
            else
            {
                positions[0] = vertexList[index].position.y;
                positions[1] = vertexList[index + 1].position.y;
                positions[2] = vertexList[index + 2].position.y;
            }

            return positions;
        }

        private List<float> FindStops(float zoomOffset, Rect bounds)
        {
            var stops = new List<float>();
            var offset = GradientOffset * (1.0f - zoomOffset);
            var startBoundary = zoomOffset - offset;
            var endBoundary = 1.0f - zoomOffset - offset;

            foreach (var color in GradientColor.colorKeys)
            {
                if (color.time >= endBoundary)
                {
                    break;
                }

                if (color.time > startBoundary)
                {
                    stops.Add((color.time - startBoundary) * GradientZoom);
                }
            }

            foreach (var alpha in GradientColor.alphaKeys)
            {
                if (alpha.time >= endBoundary)
                {
                    break;
                }

                if (alpha.time > startBoundary)
                {
                    stops.Add((alpha.time - startBoundary) * GradientZoom);
                }
            }

            var min = bounds.xMin;
            var size = bounds.width;
            if (GradientType == Type.Vertical)
            {
                min = bounds.yMin;
                size = bounds.height;
            }

            stops.Sort();
            for (var i = 0; i < stops.Count; i++)
            {
                stops[i] = stops[i] * size + min;

                if (i > 0 && Math.Abs(stops[i] - stops[i - 1]) < 2)
                {
                    stops.RemoveAt(i);
                    --i;
                }
            }

            return stops;
        }

        private UIVertex CreateSplitVertex(UIVertex vertex1, UIVertex vertex2, float stop)
        {
            if (GradientType == Type.Horizontal)
            {
                var sx = vertex1.position.x - stop;
                var dx = vertex1.position.x - vertex2.position.x;
                var dy = vertex1.position.y - vertex2.position.y;
                var uvx = vertex1.uv0.x - vertex2.uv0.x;
                var uvy = vertex1.uv0.y - vertex2.uv0.y;
                var ratio = sx / dx;
                var splitY = vertex1.position.y - dy * ratio;

                var splitVertex = new UIVertex();
                splitVertex.position = new Vector3(stop, splitY, vertex1.position.z);
                splitVertex.normal = vertex1.normal;
                splitVertex.uv0 = new Vector2(vertex1.uv0.x - uvx * ratio, vertex1.uv0.y - uvy * ratio);
                splitVertex.color = Color.white;
                return splitVertex;
            }
            else
            {
                var sy = vertex1.position.y - stop;
                var dy = vertex1.position.y - vertex2.position.y;
                var dx = vertex1.position.x - vertex2.position.x;
                var uvx = vertex1.uv0.x - vertex2.uv0.x;
                var uvy = vertex1.uv0.y - vertex2.uv0.y;
                var ratio = sy / dy;
                var splitX = vertex1.position.x - dx * ratio;

                var splitVertex = new UIVertex();
                splitVertex.position = new Vector3(splitX, stop, vertex1.position.z);
                splitVertex.normal = vertex1.normal;
                splitVertex.uv0 = new Vector2(vertex1.uv0.x - uvx * ratio, vertex1.uv0.y - uvy * ratio);
                splitVertex.color = Color.white;
                return splitVertex;
            }
        }

        private Color BlendColor(Color colorA, Color colorB)
        {
            switch (BlendMode)
            {
                case Blend.Add: return colorA + colorB;
                case Blend.Multiply: return colorA * colorB;

                default: return colorB;
            }
        }

        #endregion

        #region Public Enum

        public enum Type : byte
        {
            Horizontal = 0,
            Vertical = 1,
            Radial = 2,
            Diamond = 3
        }

        public enum Blend : byte
        {
            Override = 0,
            Add = 1,
            Multiply = 2
        }

        #endregion
    }
}
