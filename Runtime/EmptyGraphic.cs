using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace OpenUGD.UI
{
    /// <summary>
    /// A <see cref="Graphic"/> that draws nothing but still receives raycasts: an invisible hit area for a
    /// button, a drag handle or a click blocker. Use it instead of an <see cref="Image"/> with zero alpha, which
    /// still builds a mesh and is drawn unless its <see cref="CanvasRenderer"/> culls transparent meshes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The hit area is the <see cref="RectTransform"/> rect, as for any graphic: <see cref="GraphicRaycaster"/>
    /// tests the rect first and only then asks <see cref="ICanvasRaycastFilter"/> components.
    /// <see cref="Graphic.raycastTarget"/> turns the hit area off, as it does for any graphic.
    /// </para>
    /// <para>
    /// The colour, material and texture have no visible effect, because the mesh has no vertices.
    /// </para>
    /// </remarks>
    [MovedFrom(true, sourceNamespace: "UnityEngine.UI")]
    [AddComponentMenu("UI/Empty Graphic", 13)]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class EmptyGraphic : Graphic, ICanvasRaycastFilter
    {
        /// <summary>
        /// Accepts every location. The rect test has already passed when a raycaster asks, so the whole rect is
        /// the hit area.
        /// </summary>
        /// <param name="sp">The screen position of the raycast.</param>
        /// <param name="eventCamera">The camera of the raycast, <c>null</c> for a screen-space overlay canvas.</param>
        /// <returns>Always <c>true</c>.</returns>
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera) => true;

        /// <summary>
        /// Leaves the mesh empty: clears <paramref name="vh"/> and adds no vertices.
        /// </summary>
        /// <param name="vh">The mesh to fill.</param>
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
    }
}
