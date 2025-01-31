namespace UnityEngine.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/EmptyGraphic")]
    public class EmptyGraphic : Graphic, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera) => true;

        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
    }
}
