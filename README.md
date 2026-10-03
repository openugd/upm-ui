# OpenUGD uGUI Components (com.openugd.ui)

`com.openugd.ui` adds three components to uGUI (Unity UI) that change the mesh a graphic already builds, so they
need no shader, material or texture of their own: `UIFlippable` mirrors a graphic, `GradientMeshEffect` colours
it with a gradient, and `EmptyGraphic` is an invisible raycast target. Use it when an `Image`, `Text` or other
uGUI graphic needs a flip without a negative scale, a gradient without a gradient texture, or a clickable area
without a transparent `Image`.

All three live in the `OpenUGD.UI` namespace. The package depends only on `com.unity.ugui`; it needs no other
OpenUGD package, and no OpenUGD package needs it.

## Install

### openupm-cli

```sh
openupm add com.openugd.ui@2.0.0
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

A git URL does not resolve OpenUGD dependencies from OpenUPM, but this package has none: the line above is
all a git install needs. Unity resolves `com.unity.ugui` from its own registry.

## Requirements

- Unity 6000.0 or newer.
- uGUI `com.unity.ugui` 2.0.0 or a later 2.x, which Unity 6 projects include by default.
- No other OpenUGD package.

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

Every setter rebuilds the graphic's mesh when the value changes (`GradientColor` on every assignment), so
animating `GradientOffset` or toggling `horizontal` needs no `SetVerticesDirty` call. Like every uGUI
component, the three are used from Unity's main thread.

## Concepts

### Mesh effects run in component order

uGUI runs the mesh effects on a GameObject in the order of its components, top to bottom in the Inspector.
`UIFlippable` above `GradientMeshEffect` mirrors the graphic and leaves the gradient's direction alone; below
it, the gradient is mirrored with the graphic. The same holds for Unity's `Shadow` and `Outline`. Neither
component reorders itself. A disabled or inactive effect leaves the mesh alone, and enabling or disabling it
rebuilds the mesh.

### UIFlippable: a mirror without a negative scale

`UIFlippable` moves each vertex to the other side of the centre of the `RectTransform` rect. The texture is
mirrored with the geometry; the transform, the layout and the raycast area stay as they are. Unlike a negative
scale, it mirrors about the centre of the rect whatever the pivot, and it leaves the children alone. Mirroring
on one axis reverses the triangle winding, as a negative scale does; uGUI's default shader draws both faces.

```csharp
var flip = icon.gameObject.AddComponent<UIFlippable>();
flip.horizontal = true;     // mirror left to right
flip.vertical = true;       // and top to bottom: together, a half turn about the centre
```

### GradientMeshEffect: a gradient in the vertex colours

**Shape and layout.** `GradientType` is `Horizontal`, `Vertical`, `Radial` or `Diamond`. All four are laid out
on the bounds of the mesh's vertices, not around the pivot. At zoom 1 and offset 0, Horizontal and Vertical
run from one edge (0) to the other (1); Radial reaches 1 on the ellipse inscribed in the bounds, Diamond on
the diamond whose corners touch the middle of each edge.

**Zoom and offset.** `GradientZoom` (0.1 to 10) magnifies the gradient: Horizontal and Vertical about the
middle of the bounds, Radial and Diamond from the centre. Below 1 the gradient ends inside the bounds and its
end colours fill the rest. `GradientOffset` (-1 to 1) slides it; for Radial and Diamond a positive offset
pushes the colours outwards. Both setters clamp.

**Blend.** `BlendMode` decides how the gradient combines with the colour the vertex already has (the
graphic's `color`, or the result of an earlier effect): `Override` replaces it, `Add` adds per channel, and
`Multiply` (the default) multiplies per channel, alpha included.

**Extra vertices.** A GPU interpolates vertex colours linearly across each triangle, so a plain quad cannot
show a key between the ends, and a Radial or Diamond gradient would give all four corners the same colour.
`ModifyVertices` (on by default) cuts the triangles where the gradient needs vertices: at every colour and
alpha key, and around the centre for Radial and Diamond. The outline is kept and every new vertex is
interpolated from the triangle it was cut from, UVs included, so sliced, tiled and atlas sprites keep their
texturing. With the `Gradient`'s default mode (`GradientMode.Blend`), Diamond and the linear shapes come out
exact; Radial is split into 32 wedges and stays within 0.5% of the true distance. Turn `ModifyVertices` off
for meshes that are already dense, such as text. A mesh that would reach uGUI's 65,000-vertex limit is
coloured without extra vertices instead.

**Changing a gradient in place.** `GradientColor` returns the component's own `Gradient`. After changing its
keys, assign it back: the setter refreshes the key positions the effect has cached and rebuilds the mesh.
`SetVerticesDirty()` alone rebuilds with the colours of the new keys but the vertices of the old ones.
Detecting the change on every rebuild would need `Gradient.Equals`, which allocates in the Unity runtime.
The cache is also refreshed when the component is enabled and when it is edited in the Inspector.

```csharp
using OpenUGD.UI;
using UnityEngine;

public class GradientRecolour : MonoBehaviour
{
    [SerializeField] private GradientMeshEffect _effect;

    public void SetColours(Color start, Color middle, Color end)
    {
        var gradient = _effect.GradientColor;
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(middle, 0.5f),
                new GradientColorKey(end, 1f)
            },
            gradient.alphaKeys);

        // Required: the setter re-reads the keys and rebuilds the mesh.
        _effect.GradientColor = gradient;
    }
}
```

**Allocation.** Once warm, a rebuild allocates no managed memory: the work lists come from Unity's
`ListPool`, and the key positions are re-read only after the gradient is assigned, the component is enabled
or it is edited in the Inspector.

**Tangents.** The Inspector-only option *Modify Tangents* writes the blended colour into the vertex tangent
instead of the colour, for a custom shader that reads it there. The vertex colour is then left unchanged.

### EmptyGraphic: an invisible raycast target

`EmptyGraphic` builds a mesh with no vertices, so nothing is drawn, and accepts every raycast location. The
hit area is the `RectTransform` rect, as for any graphic: `GraphicRaycaster` tests the rect, then asks
`ICanvasRaycastFilter` components. `raycastTarget` turns it off. Use it as a `Button`'s target graphic (with
`Transition` set to `None`), a drag handle or a click blocker, instead of an `Image` with zero alpha, which
still builds a mesh and is drawn unless its `CanvasRenderer` culls transparent meshes.

```csharp
var blocker = new GameObject("ClickBlocker", typeof(RectTransform), typeof(EmptyGraphic));
blocker.transform.SetParent(canvas.transform, false);   // stretch it over what it should block
```

## API overview

| Type | Base | Purpose |
| --- | --- | --- |
| `UIFlippable` | `BaseMeshEffect` | Mirrors the graphic's mesh about the centre of its `RectTransform` rect. |
| `GradientMeshEffect` | `BaseMeshEffect` | Writes a `Gradient` into the graphic's vertex colours. |
| `GradientMeshEffect.Type` | `enum : byte` | `Horizontal`, `Vertical`, `Radial` (elliptical distance from the centre), `Diamond` (Manhattan distance from the centre). |
| `GradientMeshEffect.Blend` | `enum : byte` | `Override`, `Add`, `Multiply`: how the gradient combines with the vertex colour. |
| `EmptyGraphic` | `Graphic`, `ICanvasRaycastFilter` | Draws nothing and receives raycasts over its whole rect. |

| Member | Type | Default | Contract |
| --- | --- | --- | --- |
| `UIFlippable.horizontal` | `bool` | `false` | Mirror left to right. Rebuilds the mesh when the value changes. |
| `UIFlippable.vertical` | `bool` | `false` | Mirror top to bottom. Rebuilds the mesh when the value changes. |
| `GradientMeshEffect.GradientType` | `Type` | `Horizontal` | The shape. Rebuilds the mesh when the value changes. |
| `GradientMeshEffect.BlendMode` | `Blend` | `Multiply` | How the colours combine. Rebuilds the mesh when the value changes. |
| `GradientMeshEffect.GradientColor` | `Gradient` | black to white | Returns the component's own instance; assign it back after changing it in place. Every assignment rebuilds the mesh. `null` throws `ArgumentNullException`. |
| `GradientMeshEffect.GradientOffset` | `float` | `0` | Slides the gradient; clamped to -1..1. |
| `GradientMeshEffect.GradientZoom` | `float` | `1` | Magnifies the gradient; clamped to 0.1..10. |
| `GradientMeshEffect.ModifyVertices` | `bool` | `true` | Adds the vertices the gradient needs. |
| `UIFlippable.ModifyMesh`, `GradientMeshEffect.ModifyMesh` | `void (VertexHelper)` | | Called by the graphic while it rebuilds its mesh; does nothing while the component is disabled or inactive. |
| `EmptyGraphic.IsRaycastLocationValid` | `bool (Vector2, Camera)` | | Always `true`: the rect test has already passed. |

*Modify Tangents* is a serialized field without a property.

## Samples

Import from **Package Manager > OpenUGD uGUI Components > Samples**:

| Sample | What it shows |
| --- | --- |
| **Components Demo** | One canvas built in code: text mirrored four ways (one copy flipping every second), the four gradient shapes (one on a graphic with its pivot in the corner, one animating its offset), and an invisible button whose hit area is an `EmptyGraphic`. Put `UIComponentsDemo` on an empty GameObject and enter Play mode; add an Event System to click the button. |

## Running the tests

The EditMode tests are in the assembly `com.openugd.ui.tests`. Unity compiles a package's tests only when the
package is listed under `testables` in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.openugd.ui": "2.0.0"
  },
  "testables": [
    "com.openugd.ui"
  ]
}
```

Then open **Window > General > Test Runner**, choose **EditMode** and run `com.openugd.ui.tests`. The project
needs the Test Framework package (`com.unity.test-framework`), which new Unity 6 projects include. There are
no PlayMode tests.

The tests of the components themselves carry `[Category("RequiresUnity")]`. The rest cover the mesh
algorithms behind them (mirroring, triangle cutting, gradient layout and tessellation), which make no engine
calls and also run on .NET without the editor.

## Upgrading to 2.0

2.0.0 follows 0.1.1; there is no 1.x. The package jumped to 2 to share the major version of the OpenUGD 2.0
family. What a 0.1.1 project meets, and what to do:

| What | 0.1.1 | 2.0.0 | What to do |
| --- | --- | --- | --- |
| Namespace | `UnityEngine.UI` | `OpenUGD.UI` | Add `using OpenUGD.UI;` to scripts that name the components. |
| Scenes and prefabs | bind by script GUID | bind by the same GUIDs | Nothing. |
| Minimum Unity | 2021.3 | 6000.0 | Stay on 0.1.1 on older editors. |
| `UIFlippable` order | moved itself above the other mesh effects | stays where you put it | Order the effects in the Inspector. |
| Radial with `ModifyVertices` | mesh replaced by an ellipse | outline and UVs kept | Use a round sprite or a `Mask` for a round shape. |
| Diamond | straight-line distance from an off-centre point | Manhattan distance from the centre | Check every Diamond gradient. |
| Gradient changed in place | keys re-read on every rebuild | keys re-read when it is assigned back | Assign it back to `GradientColor`. |
| `GradientColor = null` | accepted, failed at the next rebuild | `ArgumentNullException` | Assign a `Gradient`. |
| Menu | *UI > EmptyGraphic* | *UI > Empty Graphic* | Nothing. |
| Licence | MIT | Apache-2.0 | See [LICENSE.md](LICENSE.md). |

### The namespace moved; scenes and prefabs keep their components

The three components moved from `UnityEngine.UI` to `OpenUGD.UI`. Keep `using UnityEngine.UI;` for `Image`,
`Graphic` and the rest of uGUI:

```diff
 using UnityEngine;
 using UnityEngine.UI;   // Image, Button, Graphic
+using OpenUGD.UI;       // UIFlippable, GradientMeshEffect, EmptyGraphic
```

A fully qualified name changes the same way: `UnityEngine.UI.GradientMeshEffect.Type.Radial` is now
`OpenUGD.UI.GradientMeshEffect.Type.Radial`. The assembly is still `com.openugd.ui`, so asmdef references need
no change.

Scenes and prefabs need nothing. Unity binds a component to its script by the script's GUID, not by its
namespace, and the file names, class names and `.meta` GUIDs are unchanged. So are the serialized field names,
except `UIFlippable`'s misspelt `_veritical`, now `_vertical`; `[FormerlySerializedAs]` reads the old name, so
saved values survive, and a scene saved with 2.0 writes the new one.

Each type carries `[MovedFrom(true, sourceNamespace: "UnityEngine.UI")]`, which asks Unity's API Updater to
rewrite old references when it runs on a script that no longer compiles. If it leaves a script unchanged, add
the `using` line by hand.

### UIFlippable stays where you put it

In the editor, whenever it woke or was validated, 0.1.1 moved `UIFlippable` up the component list, one slot for
every mesh effect above it, and it mirrored the mesh even while disabled. In 2.0 it does neither. To keep the 0.1.1
result, drag it above the other effects once and save the scene or prefab; to mirror another effect's result
too, put it below.

The setters now rebuild the mesh themselves:

```csharp
// 0.1.1: the setter only stored the value.
flippable.horizontal = true;
image.SetVerticesDirty();

// 2.0: the setter rebuilds the mesh when the value changes. The extra call still works but is not needed.
flippable.horizontal = true;
```

A subclass that overrode `UIFlippable`'s editor-only `Awake()` or `OnValidate()` now overrides
`UIBehaviour.Awake` and `BaseMeshEffect.OnValidate` instead; calls to `base.Awake()` and `base.OnValidate()`
still compile.

### Radial and Diamond look different

**Radial.** With `ModifyVertices` on, 0.1.1 replaced the whole mesh with a 64-segment ellipse the size of the
vertex bounds, laid out around the pivot, with UVs from 0 to 1. Rectangular graphics were cropped, sliced and
tiled geometry was lost, atlas sprites showed the wrong part of the atlas, and the shape was off-centre for any
pivot other than the middle. 2.0 cuts the existing triangles instead, into 32 wedges around the centre and
along a ring at every key, so the graphic keeps its outline and its sprite's UVs, centred whatever the pivot.
The colour at a given point is computed as before, so with `ModifyVertices` off a Radial gradient looks as it
did (unless *Modify Tangents* is on, below). For a round shape, use a round sprite or a `Mask`.

**Diamond.** 0.1.1 measured the straight-line distance from the point `(center.y / 2, center.y / 2)`, scaled by
the height only, so the shape was neither centred nor a diamond. 2.0 measures the Manhattan distance from the
centre of the vertex bounds and reaches the gradient's end on the diamond whose corners touch the middle of
each edge. Every Diamond gradient looks different; recheck its keys and zoom. For a circular falloff use
Radial.

**Modify Tangents** now applies to Radial and Diamond as well; 0.1.1 ignored it there and wrote the vertex
colour. If it is on for a Radial or Diamond gradient, turn it off to keep colouring through the vertex colour.

Horizontal and Vertical compute each vertex's colour as 0.1.1 did. The vertices `ModifyVertices` adds at a key
were white in 0.1.1, so with `Multiply` or `Add` they lost the graphic's colour; they now take it from the
triangle they were cut from.

### Changing a gradient in place: assign it back

`GradientMeshEffect` now caches where its gradient's keys are, because reading them allocates. An in-place
change is picked up when the gradient is assigned back:

```csharp
// 0.1.1: every rebuild re-read the keys.
effect.GradientColor.SetKeys(colorKeys, alphaKeys);
image.SetVerticesDirty();

// 2.0: assigning the gradient back re-reads the keys and rebuilds the mesh.
var gradient = effect.GradientColor;
gradient.SetKeys(colorKeys, alphaKeys);
effect.GradientColor = gradient;
```

Assigning `null` now throws `ArgumentNullException`; 0.1.1 accepted it and threw a `NullReferenceException` at
the next rebuild.

### Smaller changes

- The setters of both effects rebuild the mesh only when the value changes (`GradientColor` on every
  assignment). To force a rebuild, call `SetVerticesDirty()` on the graphic.
- A mesh that `ModifyVertices` would grow to 65,000 vertices or more, where uGUI throws, is coloured without
  extra vertices.
- The display name is "OpenUGD uGUI Components" instead of "UI Elements", the former name of UI Toolkit. The
  package ID `com.openugd.ui` is unchanged.
- *UI > Effects > Flippable* and *UI > Effects > Gradient* keep their paths and now sort after Unity's own
  effects.
- `package.json` declares `com.unity.ugui` 2.0.0; 0.1.1 declared no dependency.

The complete list is in [CHANGELOG.md](CHANGELOG.md).

## Versioning

The OpenUGD packages share their major version; minor and patch versions are independent. Each 2.x package
works with the 2.x versions of its dependencies at or above the minimums declared in its `package.json`.
`com.openugd.ui` has no OpenUGD dependency and no OpenUGD package depends on it, so it can be updated on its
own.

## Licence

Apache-2.0 — see [LICENSE.md](LICENSE.md). Releases before 2.0.0 remain under the terms they were published
with.
