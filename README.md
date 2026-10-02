# OpenUGD uGUI Components (com.openugd.ui)

Three uGUI components that work on the mesh uGUI already builds, so they need no shader, material or
texture of their own:

- **`UIFlippable`** mirrors a graphic horizontally, vertically or both, about the centre of its
  `RectTransform`, without a negative scale on the transform.
- **`GradientMeshEffect`** writes a horizontal, vertical, radial or diamond gradient into the vertex colours.
- **`EmptyGraphic`** draws nothing and still receives raycasts: an invisible hit area or button.

All three live in the `OpenUGD.UI` namespace. The package depends only on `com.unity.ugui`, and on no other
OpenUGD package.

## Install

### openupm-cli

```sh
openupm add com.openugd.ui
```

### Scoped registry

Add the registry and the package to `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.openugd"
      ]
    }
  ],
  "dependencies": {
    "com.openugd.ui": "2.0.0"
  }
}
```

### Git URL

In `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.openugd.ui": "https://github.com/openugd/upm-ui.git#2.0.0"
  }
}
```

## Quick start

In the editor the components are under **Add Component > UI**: *Empty Graphic*, and *Effects > Flippable* and
*Effects > Gradient* next to Unity's Shadow and Outline. From code:

```csharp
using OpenUGD.UI;
using UnityEngine;
using UnityEngine.UI;

public class GradientButtonSetup : MonoBehaviour
{
    [SerializeField] private Image _target;

    private void Start()
    {
        // Mirror the image left to right; the RectTransform keeps its positive scale.
        var flippable = _target.gameObject.AddComponent<UIFlippable>();
        flippable.horizontal = true;

        // A vertical gradient multiplied into the image's colour. Effects run in component order, so this one
        // runs after the flip and keeps its direction.
        var gradient = _target.gameObject.AddComponent<GradientMeshEffect>();
        gradient.GradientType = GradientMeshEffect.Type.Vertical;
        gradient.BlendMode = GradientMeshEffect.Blend.Multiply;
        gradient.GradientColor = new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.gray, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        };

        // An invisible, clickable area over the image: no vertices, still a raycast target.
        var hitArea = new GameObject("HitArea", typeof(RectTransform), typeof(EmptyGraphic), typeof(Button));
        hitArea.transform.SetParent(_target.transform, false);
        var area = (RectTransform)hitArea.transform;
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.sizeDelta = Vector2.zero;
        hitArea.GetComponent<Button>().onClick.AddListener(() => Debug.Log("clicked"));
    }
}
```

Every setter rebuilds the graphic's mesh when the value changes, so animating `GradientOffset` or toggling
`horizontal` needs no `SetVerticesDirty` call.

## API overview

| Type | Base | Purpose |
| --- | --- | --- |
| `UIFlippable` | `BaseMeshEffect` | Mirrors the mesh about the centre of the `RectTransform` rect. Properties: `horizontal`, `vertical`. |
| `GradientMeshEffect` | `BaseMeshEffect` | Writes a `Gradient` into the vertex colours. Properties: `GradientType`, `BlendMode`, `GradientColor`, `GradientOffset` (-1..1), `GradientZoom` (0.1..10), `ModifyVertices`. |
| `GradientMeshEffect.Type` | `enum` | `Horizontal`, `Vertical`, `Radial` (elliptical distance from the centre), `Diamond` (Manhattan distance from the centre). |
| `GradientMeshEffect.Blend` | `enum` | `Override`, `Add`, `Multiply`: how the gradient combines with the colour the vertex already has. |
| `EmptyGraphic` | `Graphic`, `ICanvasRaycastFilter` | Draws nothing and receives raycasts over its whole rect. |

### How the effects behave

- **Disabled means off.** A disabled or inactive effect leaves the mesh alone, and enabling or disabling it
  rebuilds the mesh.
- **Component order is the order of effects.** uGUI runs mesh effects in the order of the components on the
  GameObject. `UIFlippable` above `GradientMeshEffect` keeps the gradient's direction; below, the gradient is
  mirrored with the graphic. The same holds for Unity's `Shadow` and `Outline`.
- **The gradient follows the mesh, not the pivot.** All four shapes are laid out on the bounds of the mesh's
  vertices. At zoom 1 and offset 0, Horizontal and Vertical run from one edge (0) to the other (1); Radial
  reaches 1 on the ellipse inscribed in the bounds, Diamond on the diamond whose corners touch the middle of
  each edge. `GradientZoom` magnifies the gradient about the middle, and `GradientOffset` slides it.
- **`ModifyVertices`** (on by default) adds vertices where the gradient needs them, because a GPU
  interpolates vertex colours linearly: on a plain quad a key between the ends would not show, and Radial or
  Diamond would give all four corners the same colour. The triangles are cut at every colour and alpha key,
  and around the centre for Radial and Diamond; the outline is kept and every new vertex is interpolated from
  the triangle it was cut from, UVs included, so sliced, tiled and atlas sprites keep their texturing.
  With the gradient's default *Blend* mode, Diamond and the linear shapes come out exact; Radial is split into
  32 wedges and stays within 0.5% of the true distance. Turn it off for meshes that are already dense, such as
  text. A mesh that would reach uGUI's 65,000-vertex limit is coloured without extra vertices instead.
- **Changing a gradient in place.** `GradientColor` returns the component's own `Gradient`. After changing its
  keys in place, assign it back or call `SetVerticesDirty()` on the graphic so the mesh is rebuilt.
- **Allocation.** Once warm, a rebuild allocates no managed memory: the work lists come from Unity's
  `ListPool`, and the gradient's key times are re-read only when the gradient has changed.
- **Tangents.** The Inspector-only option *Modify Tangents* writes the blended colour into the vertex tangent
  instead of the colour, for a custom shader that reads it there.

### EmptyGraphic

The hit area is the `RectTransform` rect, as for any graphic: `GraphicRaycaster` tests the rect, then asks
`ICanvasRaycastFilter` components, and `EmptyGraphic` accepts every location. `raycastTarget` turns it off.
Use it instead of an `Image` with zero alpha, which still builds a mesh and is drawn unless its
`CanvasRenderer` culls transparent meshes.

## Samples

**Components Demo** (Package Manager > OpenUGD uGUI Components > Samples) builds one canvas in code with all
three components: text flipped four ways, the four gradient shapes (one on a graphic with its pivot in the
corner) and an invisible button. Put `UIComponentsDemo` on an empty GameObject and enter Play mode.

## Upgrading from 0.1.x

2.0.0 joins the OpenUGD 2.0 family. Everything breaking is listed in [CHANGELOG.md](CHANGELOG.md); the
points that reach most projects:

- **Namespace.** The three components moved from `UnityEngine.UI` to `OpenUGD.UI`. Add `using OpenUGD.UI;` to
  scripts that name them; keep `using UnityEngine.UI;` for `Image`, `Graphic` and the rest of uGUI. Each type
  carries `[MovedFrom(true, sourceNamespace: "UnityEngine.UI")]`, which asks Unity's API Updater to rewrite old
  references when it runs on a script that no longer compiles; if it leaves a script unchanged, add the
  `using` line by hand. File names, class names and script GUIDs are unchanged, so scenes and prefabs keep
  their components and serialized values.
- **`UIFlippable` no longer moves itself** above the other mesh effects in the editor. Order them in the
  Inspector: flip first to keep an effect's direction, last to mirror it.
- **Radial and Diamond look different.** With `ModifyVertices` on, Radial built its extra vertices around the
  pivot instead of the centre of the graphic and replaced the mesh with an ellipse the size of the rect, UVs
  0 to 1; it now keeps the graphic's outline and its sprite's UVs. Diamond measured the straight-line
  distance from a wrong centre; it is now the Manhattan distance from the centre of the graphic.
- **Menu.** *UI > EmptyGraphic* is now *UI > Empty Graphic*.

## Requirements

- Unity 6000.0 or newer
- uGUI 2.0.0 (`com.unity.ugui`), which Unity 6 includes by default

## Licence

Apache-2.0 — see [LICENSE.md](LICENSE.md).
