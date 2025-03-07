﻿namespace UnityEngine.UI
{
    [RequireComponent(typeof(RectTransform), typeof(Graphic))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Effects/Flippable")]
    public class UIFlippable : BaseMeshEffect
    {
        [SerializeField] private bool _horizontal;
        [SerializeField] private bool _veritical;

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="UnityEngine.UI.UIFlippable" /> should be flipped
        ///     horizontally.
        /// </summary>
        /// <value><c>true</c> if horizontal; otherwise, <c>false</c>.</value>
        public bool horizontal {
            get => _horizontal;
            set => _horizontal = value;
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="UnityEngine.UI.UIFlippable" /> should be flipped
        ///     vertically.
        /// </summary>
        /// <value><c>true</c> if vertical; otherwise, <c>false</c>.</value>
        public bool vertical {
            get => _veritical;
            set => _veritical = value;
        }

#if UNITY_EDITOR
        protected override void Awake() => OnValidate();
#endif

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            var components = gameObject.GetComponents(typeof(BaseMeshEffect));
            foreach (var comp in components)
            {
                if (comp.GetType() != typeof(UIFlippable))
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(this);
                }
                else
                {
                    break;
                }
            }

            GetComponent<Graphic>().SetVerticesDirty();
            base.OnValidate();
        }
#endif

        public override void ModifyMesh(VertexHelper verts)
        {
            var rt = transform as RectTransform;

            for (var i = 0; i < verts.currentVertCount; ++i)
            {
                var uiVertex = new UIVertex();
                verts.PopulateUIVertex(ref uiVertex, i);

                // Modify positions
                uiVertex.position = new Vector3(
                    _horizontal
                        ? uiVertex.position.x + (rt.rect.center.x - uiVertex.position.x) * 2
                        : uiVertex.position.x,
                    _veritical
                        ? uiVertex.position.y + (rt.rect.center.y - uiVertex.position.y) * 2
                        : uiVertex.position.y,
                    uiVertex.position.z
                );

                // Apply
                verts.SetUIVertex(uiVertex, i);
            }
        }
    }
}
