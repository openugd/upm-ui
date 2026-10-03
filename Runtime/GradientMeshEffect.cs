using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenUGD.UI
{
    /// <summary>
    /// Colours the mesh of the <see cref="Graphic"/> on the same GameObject with a <see cref="Gradient"/>, by
    /// writing the gradient into the vertex colours. Needs no shader, material or texture of its own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gradient is laid out on the bounds of the mesh's vertices, so it does not depend on the
    /// <c>RectTransform</c> pivot. <see cref="GradientType"/> picks the shape, <see cref="GradientZoom"/> and
    /// <see cref="GradientOffset"/> scale and slide it, and <see cref="BlendMode"/> decides how it combines with
    /// the colour the graphic already gave each vertex (its <c>color</c> property, a previous effect).
    /// </para>
    /// <para>
    /// A GPU interpolates vertex colours linearly across each triangle, so a quad's four corners cannot show a
    /// gradient with a key in the middle, a radial shape or a diamond. With <see cref="ModifyVertices"/> on, the
    /// effect cuts the triangles where the gradient needs vertices (at every colour and alpha key, and around the
    /// centre for Radial and Diamond). The outline is kept and every new vertex is interpolated from the triangle
    /// it came from, UVs included, so sliced, tiled and atlas sprites keep their texturing.
    /// </para>
    /// <para>
    /// Mesh effects run in component order. Put this component below a <see cref="UIFlippable"/> to keep the
    /// gradient's direction when the graphic is flipped, or above it to flip the gradient with the graphic.
    /// </para>
    /// <para>
    /// Once its pooled work lists have grown to the size of the mesh, a rebuild allocates no managed memory: the
    /// work lists come from <see cref="ListPool{T}"/>, and the gradient's key times are re-read only when the
    /// gradient changed.
    /// </para>
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "UnityEngine.UI")]
    [AddComponentMenu("UI/Effects/Gradient", 83)]
    [RequireComponent(typeof(RectTransform), typeof(Graphic))]
    [DisallowMultipleComponent]
    public class GradientMeshEffect : BaseMeshEffect
    {
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 10f;

        // VertexHelper.FillMesh throws at this many vertices or more.
        private const int MaxVertices = 65000;

        [FormerlySerializedAs("gradient_type")] [SerializeField]
        private Type gradientType = Type.Horizontal;

        [FormerlySerializedAs("blend_mode")] [SerializeField]
        private Blend blendMode = Blend.Multiply;

        [FormerlySerializedAs("modify_vertices")]
        [Tooltip("Add vertices so the gradient shows its keys, and Radial and Diamond shapes, on simple meshes. " +
                 "Turn it off for meshes that are already dense, such as text.")]
        [SerializeField]
        private bool modifyVertices = true;

        [FormerlySerializedAs("modify_tangents")]
        [Tooltip("Write the blended colour into the vertex tangent instead of the vertex colour, for a custom " +
                 "shader that reads it from there. The vertex colour is then left unchanged.")]
        [SerializeField]
        private bool modifyTangents;

        [FormerlySerializedAs("gradient_offset")] [SerializeField] [Range(-1.0f, 1.0f)]
        private float gradientOffset;

        [FormerlySerializedAs("gradient_zoom")] [SerializeField] [Range(MinZoom, MaxZoom)]
        private float gradientZoom = 1.0f;

        [FormerlySerializedAs("gradient_color")] [SerializeField]
        private Gradient gradientColor = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(Color.black, 0.0f), new GradientColorKey(Color.white, 1.0f) }
        };

        // The key times of the gradient they were read from, and a copy of that gradient to detect changes.
        // Gradient.colorKeys and alphaKeys allocate arrays, and so does Gradient.Equals in the Unity runtime, so the key
        // times are recomputed only when the gradient is known to have changed: through the GradientColor setter, an
        // inspector edit (OnValidate) or re-enabling. Comparing gradients on every rebuild allocated once per mesh.
        [NonSerialized] private readonly List<float> _keyTimes = new List<float>(16);
        [NonSerialized] private bool _keyTimesValid;

        /// <summary>
        /// The shape of the gradient.
        /// </summary>
        public enum Type : byte
        {
            /// <summary>Left to right across the vertex bounds.</summary>
            Horizontal = 0,

            /// <summary>Bottom to top across the vertex bounds.</summary>
            Vertical = 1,

            /// <summary>
            /// Outwards from the centre of the vertex bounds by elliptical distance: the gradient's end is on the
            /// ellipse inscribed in the bounds.
            /// </summary>
            Radial = 2,

            /// <summary>
            /// Outwards from the centre of the vertex bounds by Manhattan distance: the gradient's end is on the
            /// diamond whose corners touch the middle of each edge of the bounds.
            /// </summary>
            Diamond = 3
        }

        /// <summary>
        /// How the gradient colour combines with the colour a vertex already has.
        /// </summary>
        public enum Blend : byte
        {
            /// <summary>The gradient colour replaces the vertex colour.</summary>
            Override = 0,

            /// <summary>
            /// The colours are added per channel, alpha included; written to the vertex colour, the result
            /// saturates at 1.
            /// </summary>
            Add = 1,

            /// <summary>The colours are multiplied per channel, alpha included.</summary>
            Multiply = 2
        }

        /// <summary>
        /// How the gradient combines with the colour a vertex already has. Default <see cref="Blend.Multiply"/>.
        /// </summary>
        public Blend BlendMode
        {
            get => blendMode;
            set
            {
                if (blendMode == value)
                {
                    return;
                }

                blendMode = value;
                SetDirty();
            }
        }

        /// <summary>
        /// The gradient: black to white by default.
        /// </summary>
        /// <remarks>
        /// The getter returns the component's own instance. After changing it in place (for example with
        /// <see cref="Gradient.SetKeys"/>), assign it back to this property: the setter is what tells the component the
        /// keys changed and rebuilds the mesh. Calling <see cref="Graphic.SetVerticesDirty"/> alone rebuilds with the key
        /// positions cached from the previous assignment.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
        public Gradient GradientColor
        {
            get => gradientColor;
            set
            {
                gradientColor = value ?? throw new ArgumentNullException(nameof(value));
                _keyTimesValid = false;
                SetDirty();
            }
        }

        /// <summary>
        /// The shape of the gradient. Default <see cref="Type.Horizontal"/>.
        /// </summary>
        public Type GradientType
        {
            get => gradientType;
            set
            {
                if (gradientType == value)
                {
                    return;
                }

                gradientType = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Whether to add vertices where the gradient needs them. Default <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Without extra vertices a gradient can only be as detailed as the mesh: on a simple quad a Radial or
        /// Diamond gradient gives all four corners the same colour, and a key between the ends does not show.
        /// Turn it off for meshes that are already dense, such as text, where the extra vertices cost more than
        /// they add.
        /// </remarks>
        public bool ModifyVertices
        {
            get => modifyVertices;
            set
            {
                if (modifyVertices == value)
                {
                    return;
                }

                modifyVertices = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Slides the gradient along its shape, from -1 to 1; values outside are clamped. Default 0.
        /// </summary>
        /// <remarks>
        /// For Radial and Diamond the offset is subtracted from the gradient coordinate, so a positive offset
        /// pushes the colours outwards. For Horizontal and Vertical it is scaled by the zoom as well.
        /// </remarks>
        public float GradientOffset
        {
            get => gradientOffset;
            set
            {
                value = Mathf.Clamp(value, -1.0f, 1.0f);
                if (gradientOffset == value)
                {
                    return;
                }

                gradientOffset = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Magnifies the gradient, from 0.1 to 10; values outside are clamped. Default 1, which fits the gradient
        /// to the vertex bounds.
        /// </summary>
        /// <remarks>
        /// Above 1 the gradient is stretched so only part of it shows: Horizontal and Vertical stretch about the
        /// middle of the bounds and show the middle of the gradient, Radial and Diamond stretch from the centre
        /// and show its start. Below 1 the gradient ends inside the bounds and its end colours fill the rest.
        /// </remarks>
        public float GradientZoom
        {
            get => gradientZoom;
            set
            {
                value = Mathf.Clamp(value, MinZoom, MaxZoom);
                if (gradientZoom == value)
                {
                    return;
                }

                gradientZoom = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Writes the gradient into the vertices of <paramref name="vertexHelper"/>. Called by the
        /// <see cref="Graphic"/> while it rebuilds its mesh; does nothing while this component is disabled or
        /// inactive.
        /// </summary>
        /// <param name="vertexHelper">The graphic's mesh, modified in place.</param>
        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper == null || vertexHelper.currentVertCount == 0 || gradientColor == null)
            {
                return;
            }

            var frame = GradientFrame.Create(gradientType, GetBounds(vertexHelper),
                Mathf.Clamp(gradientOffset, -1f, 1f), Mathf.Clamp(gradientZoom, MinZoom, MaxZoom));

            if (modifyVertices)
            {
                var keyTimes = GetKeyTimes();
                if (GradientTessellator.NeedsTessellation(frame, keyTimes))
                {
                    var mesh = ListPool<UIVertex>.Get();
                    try
                    {
                        vertexHelper.GetUIVertexStream(mesh);
                        GradientTessellator.Tessellate(mesh, frame, keyTimes);
                        if (!FitsInOneMesh(mesh.Count))
                        {
                            // Too dense to subdivide (long text, say): colour the existing vertices instead.
                            PaintInPlace(vertexHelper, frame);
                            return;
                        }

                        vertexHelper.Clear();
                        for (var i = 0; i < mesh.Count; i++)
                        {
                            var vertex = mesh[i];
                            Paint(ref vertex, frame);
                            vertexHelper.AddVert(vertex);
                        }

                        for (var i = 0; i + 2 < mesh.Count; i += 3)
                        {
                            vertexHelper.AddTriangle(i, i + 1, i + 2);
                        }
                    }
                    finally
                    {
                        ListPool<UIVertex>.Release(mesh);
                    }

                    return;
                }
            }

            PaintInPlace(vertexHelper, frame);
        }

        /// <summary>
        /// Whether a mesh of <paramref name="vertexCount"/> vertices stays under uGUI's limit:
        /// <c>VertexHelper.FillMesh</c> throws at 65,000 vertices or more.
        /// </summary>
        /// <param name="vertexCount">The number of vertices.</param>
        /// <returns><c>true</c> if the mesh can be filled.</returns>
        internal static bool FitsInOneMesh(int vertexCount) => vertexCount < MaxVertices;

        /// <summary>
        /// Combines a vertex colour with a gradient colour.
        /// </summary>
        /// <param name="vertexColor">The colour the vertex already has.</param>
        /// <param name="gradientColor">The gradient's colour at the vertex.</param>
        /// <param name="mode">How to combine them.</param>
        /// <returns>The combined colour, not clamped (a <see cref="Color32"/> vertex colour saturates it).</returns>
        internal static Color BlendColors(Color vertexColor, Color gradientColor, Blend mode)
        {
            switch (mode)
            {
                case Blend.Add:
                    return vertexColor + gradientColor;
                case Blend.Multiply:
                    return vertexColor * gradientColor;
                default:
                    return gradientColor;
            }
        }

        private void PaintInPlace(VertexHelper vertexHelper, in GradientFrame frame)
        {
            var vertex = new UIVertex();
            var count = vertexHelper.currentVertCount;
            for (var i = 0; i < count; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                Paint(ref vertex, frame);
                vertexHelper.SetUIVertex(vertex, i);
            }
        }

        private void Paint(ref UIVertex vertex, in GradientFrame frame)
        {
            var color = BlendColors(vertex.color, gradientColor.Evaluate(frame.Evaluate(vertex.position)), blendMode);
            if (modifyTangents)
            {
                vertex.tangent = color;
            }
            else
            {
                vertex.color = color;
            }
        }

        private List<float> GetKeyTimes()
        {
            if (_keyTimesValid)
            {
                return _keyTimes;
            }

            var colorKeys = gradientColor.colorKeys;
            var alphaKeys = gradientColor.alphaKeys;
            _keyTimes.Clear();
            for (var i = 0; i < colorKeys.Length; i++)
            {
                AddKeyTime(colorKeys[i].time);
            }

            for (var i = 0; i < alphaKeys.Length; i++)
            {
                AddKeyTime(alphaKeys[i].time);
            }

            _keyTimesValid = true;
            return _keyTimes;
        }

        // Keeps _keyTimes sorted and free of duplicates (a colour key and an alpha key often share a time).
        private void AddKeyTime(float time)
        {
            var index = 0;
            while (index < _keyTimes.Count && _keyTimes[index] < time)
            {
                index++;
            }

            if (index < _keyTimes.Count && Mathf.Abs(_keyTimes[index] - time) < 1e-5f)
            {
                return;
            }

            if (index > 0 && Mathf.Abs(_keyTimes[index - 1] - time) < 1e-5f)
            {
                return;
            }

            _keyTimes.Insert(index, time);
        }

        private static Rect GetBounds(VertexHelper vertexHelper)
        {
            var vertex = new UIVertex();
            vertexHelper.PopulateUIVertex(ref vertex, 0);
            var min = (Vector2)vertex.position;
            var max = min;
            var count = vertexHelper.currentVertCount;
            for (var i = 1; i < count; i++)
            {
                vertexHelper.PopulateUIVertex(ref vertex, i);
                var position = vertex.position;
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            _keyTimesValid = false;
            base.OnEnable();
        }

#if UNITY_EDITOR
        /// <inheritdoc/>
        protected override void OnValidate()
        {
            _keyTimesValid = false;
            base.OnValidate();
        }
#endif

        // The setters run outside a rebuild, often from code that has just added the component; Unity's == treats a
        // destroyed or missing Graphic as null.
        private void SetDirty()
        {
            var target = graphic;
            if (target != null)
            {
                target.SetVerticesDirty();
            }
        }
    }
}
