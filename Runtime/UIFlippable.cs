using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace OpenUGD.UI
{
    /// <summary>
    /// Mirrors the mesh of the <see cref="Graphic"/> on the same GameObject about the centre of its
    /// <see cref="RectTransform"/>, horizontally, vertically or both, without a negative scale on the transform.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only vertex positions change: the texture is mirrored with the geometry, and the
    /// <see cref="RectTransform"/>, layout and raycast area stay as they are. Mirroring on one axis reverses the
    /// triangle winding, exactly as a negative scale does; uGUI's default shader draws both faces.
    /// </para>
    /// <para>
    /// Mesh effects run in component order, and this component no longer reorders itself. Put it above another
    /// effect (a <see cref="GradientMeshEffect"/>, a <see cref="Shadow"/>) to leave that effect's result
    /// unmirrored, or below it to mirror the result too.
    /// </para>
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "UnityEngine.UI")]
    [AddComponentMenu("UI/Effects/Flippable", 84)]
    [RequireComponent(typeof(RectTransform), typeof(Graphic))]
    [DisallowMultipleComponent]
    public class UIFlippable : BaseMeshEffect
    {
        [SerializeField] private bool _horizontal;

        [FormerlySerializedAs("_veritical")] [SerializeField]
        private bool _vertical;

        /// <summary>
        /// Whether the graphic is mirrored left to right. Setting a new value rebuilds the graphic's mesh.
        /// </summary>
        public bool horizontal
        {
            get => _horizontal;
            set
            {
                if (_horizontal == value)
                {
                    return;
                }

                _horizontal = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Whether the graphic is mirrored top to bottom. Setting a new value rebuilds the graphic's mesh.
        /// </summary>
        public bool vertical
        {
            get => _vertical;
            set
            {
                if (_vertical == value)
                {
                    return;
                }

                _vertical = value;
                SetDirty();
            }
        }

        /// <summary>
        /// Mirrors the vertices of <paramref name="vertexHelper"/> about the centre of this GameObject's
        /// <see cref="RectTransform"/> rect. Called by the <see cref="Graphic"/> while it rebuilds its mesh; does
        /// nothing while this component is disabled or inactive.
        /// </summary>
        /// <param name="vertexHelper">The graphic's mesh, modified in place.</param>
        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || vertexHelper == null || (!_horizontal && !_vertical))
            {
                return;
            }

            var rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return;
            }

            MeshMirror.Mirror(vertexHelper, rectTransform.rect.center, _horizontal, _vertical);
        }

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
