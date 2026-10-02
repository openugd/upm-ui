# Components Demo

The three components of `com.openugd.ui` on one canvas, built in code by `UIComponentsDemo.cs`, so there is
no scene or prefab to import.

## How to run it

1. Create an empty scene.
2. Add an empty GameObject and put **UI Components Demo** on it (Add Component > OpenUGD > Samples).
3. To click the invisible button, also add **GameObject > UI > Event System**. Unity adds the input module
   that matches **Project Settings > Player > Active Input Handling**.
4. Enter Play mode.

## What you see

| Row | What to look at |
| --- | --- |
| `UIFlippable` | The word "Flip" mirrored four ways (none, horizontal, vertical, both) about the centre of its rect, plus one copy that flips every second. The flip only moves vertices: the `RectTransform` keeps a positive scale. |
| `GradientMeshEffect` | One three-key gradient in the four shapes. Horizontal slides back and forth because `Update` sets `GradientOffset`. The last image is Radial again, with its pivot in the bottom-left corner: it looks the same, because every shape is laid out on the mesh's bounds, not around the pivot. |
| `EmptyGraphic` | A `Button` whose target graphic is an `EmptyGraphic`: the hit area is the rect inside the thin frame, and nothing inside it is drawn. The counter goes up on every click. |

## Things worth copying

- **No `SetVerticesDirty` calls.** Both effects rebuild the graphic's mesh from their own setters, so
  animating `GradientOffset` or toggling `horizontal` is a plain property assignment.
- **Mesh effects run in component order.** On a GameObject with both a `UIFlippable` and a
  `GradientMeshEffect`, the one higher in the Inspector runs first. Put the flip first to keep the gradient's
  direction, or the gradient first to mirror it with the graphic.
- **An invisible button** is `EmptyGraphic` + `Button` with `Transition` set to `None`.
