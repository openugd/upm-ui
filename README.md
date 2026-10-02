# UI Elements (com.openugd.ui)

A small set of uGUI helpers: a flip effect, a gradient mesh effect, and an empty
raycast target. Each component attaches to an existing `Graphic` (or is a
`Graphic` itself) and works by modifying the mesh uGUI already generates, so
there are no custom shaders, materials, or extra draw calls to manage. All types
live in the `UnityEngine.UI` namespace so they sit alongside the built-in uGUI
components in the Add Component menu.

## Install

### openupm-cli

```sh
openupm add com.openugd.ui
```

### Scoped registry

Add the following to `Packages/manifest.json`:

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
    "com.openugd.ui": "0.1.1"
  }
}
```

### Git URL

In `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.openugd.ui": "https://github.com/openugd/upm-ui.git"
  }
}
```

## Quick start

```csharp
using UnityEngine;
using UnityEngine.UI;

public class GradientButtonSetup : MonoBehaviour
{
    [SerializeField] private Image _target;

    private void Start()
    {
        // Flip the graphic horizontally without touching its transform scale.
        var flippable = _target.gameObject.AddComponent<UIFlippable>();
        flippable.horizontal = true;
        flippable.vertical = false;

        // Tint the generated mesh with a vertical gradient.
        var gradient = _target.gameObject.AddComponent<GradientMeshEffect>();
        gradient.GradientType = GradientMeshEffect.Type.Vertical;
        gradient.BlendMode = GradientMeshEffect.Blend.Multiply;
        gradient.GradientOffset = 0f;
        gradient.GradientZoom = 1f;
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

        // An invisible, still-clickable area (no vertices are emitted).
        var hitArea = new GameObject("HitArea", typeof(EmptyGraphic));
        hitArea.transform.SetParent(_target.transform, false);
    }
}
```

## API overview

| Type | Base | Purpose |
| --- | --- | --- |
| `UIFlippable` | `BaseMeshEffect` | Mirrors the graphic's mesh about the `RectTransform` centre. Properties: `horizontal`, `vertical`. |
| `GradientMeshEffect` | `BaseMeshEffect` | Applies a `Gradient` to the mesh vertex colours. Properties: `GradientType`, `BlendMode`, `GradientColor`, `GradientOffset`, `GradientZoom`, `ModifyVertices`. |
| `GradientMeshEffect.Type` | `enum` | `Horizontal`, `Vertical`, `Radial`, `Diamond`. |
| `GradientMeshEffect.Blend` | `enum` | `Override`, `Add`, `Multiply`. |
| `EmptyGraphic` | `Graphic`, `ICanvasRaycastFilter` | Draws nothing but still receives raycasts — use it for invisible hit areas instead of a transparent `Image`. |

Notes:

- `GradientMeshEffect.ModifyVertices` adds vertices at the gradient's colour and
  alpha stops for a more accurate result. Turn it off for meshes that are
  already dense, such as text.
- `UIFlippable` moves itself above other `BaseMeshEffect` components in the
  editor so the flip is applied before them.

## Requirements

- Unity 2022.3 or newer
- The uGUI package (`com.unity.ugui`), which ships with Unity by default

## Licence

Apache-2.0 — see [LICENSE.md](LICENSE.md).
