using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenUGD.UI.Samples
{
    /// <summary>
    /// Builds a screen-space canvas that shows the three components of <c>com.openugd.ui</c>: four flipped
    /// texts, the four gradient shapes (one of them on a graphic with its pivot in the corner), and an invisible
    /// button made from an <see cref="EmptyGraphic"/>. Put it on an empty GameObject and enter Play mode.
    /// </summary>
    /// <remarks>
    /// Everything is created in code so the sample has no scene or prefab to import. Two things move while it
    /// runs, and neither calls <c>SetVerticesDirty</c>: the setters of both effects rebuild the mesh themselves.
    /// </remarks>
    [AddComponentMenu("OpenUGD/Samples/UI Components Demo")]
    public sealed class UIComponentsDemo : MonoBehaviour
    {
        private const float Cell = 150f;
        private const float FirstColumn = -220f;
        private const float TitleColumn = -480f;

        [SerializeField] [Tooltip("Seconds between two flips of the animated text.")]
        private float _flipInterval = 1f;

        private Font _font;
        private UIFlippable _animatedFlip;
        private GradientMeshEffect _animatedGradient;
        private Text _clickLabel;
        private int _clicks;
        private float _nextFlip;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = CreateCanvas();

            BuildFlippableRow(canvas, 220f);
            BuildGradientRow(canvas, 0f);
            BuildEmptyGraphicRow(canvas, -220f);

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                Debug.Log("UIComponentsDemo: add an Event System (GameObject > UI > Event System) to click the " +
                          "invisible button. Unity picks the input module that matches your Active Input Handling.",
                    this);
            }
        }

        private void Update()
        {
            // Both setters mark the graphic's vertices dirty, so the mesh follows without any other call.
            _animatedGradient.GradientOffset = Mathf.Sin(Time.time) * 0.5f;
            if (Time.time >= _nextFlip)
            {
                _animatedFlip.horizontal = !_animatedFlip.horizontal;
                _nextFlip = Time.time + _flipInterval;
            }
        }

        private RectTransform CreateCanvas()
        {
            var canvasObject = new GameObject("UI Components Demo", typeof(RectTransform));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasObject.AddComponent<GraphicRaycaster>();
            return (RectTransform)canvasObject.transform;
        }

        // UIFlippable: the same text, mirrored four ways, plus one that the Update loop flips.
        private void BuildFlippableRow(RectTransform canvas, float y)
        {
            AddLabel(canvas, "UIFlippable", new Vector2(TitleColumn, y), 24);
            var cases = new[] { (false, false, "none"), (true, false, "horizontal"), (false, true, "vertical"), (true, true, "both") };
            for (var i = 0; i < cases.Length; i++)
            {
                var (horizontal, vertical, caption) = cases[i];
                var position = new Vector2(FirstColumn + i * Cell, y);
                var text = AddLabel(canvas, "Flip", position, 48);
                var flippable = text.gameObject.AddComponent<UIFlippable>();
                flippable.horizontal = horizontal;
                flippable.vertical = vertical;
                AddLabel(canvas, caption, position + new Vector2(0f, -55f), 18);
            }

            var animatedPosition = new Vector2(FirstColumn + 4 * Cell, y);
            _animatedFlip = AddLabel(canvas, "Flip", animatedPosition, 48).gameObject.AddComponent<UIFlippable>();
            AddLabel(canvas, "animated", animatedPosition + new Vector2(0f, -55f), 18);
        }

        // GradientMeshEffect: the four shapes on plain white images, and Radial again with the pivot in the
        // bottom-left corner, which lands on the same picture because the shapes follow the vertex bounds.
        private void BuildGradientRow(RectTransform canvas, float y)
        {
            AddLabel(canvas, "GradientMeshEffect", new Vector2(TitleColumn, y), 24);
            var shapes = new[]
            {
                GradientMeshEffect.Type.Horizontal, GradientMeshEffect.Type.Vertical, GradientMeshEffect.Type.Radial,
                GradientMeshEffect.Type.Diamond
            };
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.55f, 0f), 0f),
                    new GradientColorKey(new Color(0.85f, 0.1f, 0.6f), 0.5f),
                    new GradientColorKey(new Color(0.1f, 0.8f, 0.9f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            for (var i = 0; i < shapes.Length; i++)
            {
                var position = new Vector2(FirstColumn + i * Cell, y);
                var effect = AddGradientImage(canvas, position, new Vector2(0.5f, 0.5f), shapes[i], gradient);
                AddLabel(canvas, shapes[i].ToString(), position + new Vector2(0f, -75f), 18);
                if (i == 0)
                {
                    _animatedGradient = effect;
                }
            }

            var cornerPosition = new Vector2(FirstColumn + 4 * Cell, y);
            AddGradientImage(canvas, cornerPosition - new Vector2(60f, 60f), Vector2.zero, GradientMeshEffect.Type.Radial,
                gradient);
            AddLabel(canvas, "Radial, pivot (0, 0)", cornerPosition + new Vector2(0f, -75f), 18);
        }

        // EmptyGraphic: an invisible button. The thin frame only shows where the hit area is; the area itself draws
        // nothing.
        private void BuildEmptyGraphicRow(RectTransform canvas, float y)
        {
            AddLabel(canvas, "EmptyGraphic", new Vector2(TitleColumn, y), 24);
            var size = new Vector2(300f, 100f);
            var centre = new Vector2(FirstColumn + 1.5f * Cell, y);
            AddFrame(canvas, centre, size);

            var hitArea = new GameObject("Invisible Button", typeof(RectTransform));
            var rect = (RectTransform)hitArea.transform;
            rect.SetParent(canvas, false);
            rect.anchoredPosition = centre;
            rect.sizeDelta = size;
            var graphic = hitArea.AddComponent<EmptyGraphic>();
            var button = hitArea.AddComponent<Button>();
            button.targetGraphic = graphic;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnInvisibleButtonClicked);

            AddLabel(canvas, "click inside the frame", centre, 20);
            _clickLabel = AddLabel(canvas, "0 clicks", centre + new Vector2(2.5f * Cell, 0f), 24);
        }

        private void OnInvisibleButtonClicked()
        {
            _clicks++;
            _clickLabel.text = _clicks == 1 ? "1 click" : _clicks + " clicks";
        }

        private GradientMeshEffect AddGradientImage(RectTransform canvas, Vector2 position, Vector2 pivot,
            GradientMeshEffect.Type shape, Gradient gradient)
        {
            var imageObject = new GameObject(shape + " gradient", typeof(RectTransform));
            var rect = (RectTransform)imageObject.transform;
            rect.SetParent(canvas, false);
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(120f, 120f);
            imageObject.AddComponent<Image>();
            var effect = imageObject.AddComponent<GradientMeshEffect>();
            effect.GradientType = shape;
            effect.BlendMode = GradientMeshEffect.Blend.Override;
            effect.GradientColor = gradient;
            return effect;
        }

        private void AddFrame(RectTransform canvas, Vector2 centre, Vector2 size)
        {
            const float thickness = 2f;
            var colour = new Color(1f, 1f, 1f, 0.6f);
            AddBar(canvas, centre + new Vector2(0f, size.y * 0.5f), new Vector2(size.x, thickness), colour);
            AddBar(canvas, centre - new Vector2(0f, size.y * 0.5f), new Vector2(size.x, thickness), colour);
            AddBar(canvas, centre + new Vector2(size.x * 0.5f, 0f), new Vector2(thickness, size.y), colour);
            AddBar(canvas, centre - new Vector2(size.x * 0.5f, 0f), new Vector2(thickness, size.y), colour);
        }

        private static void AddBar(RectTransform canvas, Vector2 position, Vector2 size, Color colour)
        {
            var bar = new GameObject("Frame", typeof(RectTransform));
            var rect = (RectTransform)bar.transform;
            rect.SetParent(canvas, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = bar.AddComponent<Image>();
            image.color = colour;
            image.raycastTarget = false;
        }

        private Text AddLabel(RectTransform canvas, string content, Vector2 position, int fontSize)
        {
            var labelObject = new GameObject(content, typeof(RectTransform));
            var rect = (RectTransform)labelObject.transform;
            rect.SetParent(canvas, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(Cell + 100f, fontSize * 1.5f);
            var text = labelObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }
    }
}
