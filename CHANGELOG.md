# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - Unreleased

The package joins the OpenUGD 2.0 family as a leaf: its only dependency is `com.unity.ugui`, and no other
OpenUGD package depends on it. The jump from 0.1.1 straight to 2.0.0 puts it on the family's synchronized
major version. Read *Changed* before upgrading: the namespace moved and three behaviours changed.

### Changed

- **Breaking: the three components moved from `UnityEngine.UI` to `OpenUGD.UI`** — `EmptyGraphic`,
  `GradientMeshEffect` (with its nested `Type` and `Blend`) and `UIFlippable`. Declaring types inside Unity's
  own namespace risked clashing with uGUI, and the README's reason for it was false: the Add Component menu
  comes from `[AddComponentMenu]`, not from the namespace. File names, class names and script `.meta` GUIDs
  are unchanged, so scenes and prefabs keep their components and values. Each type carries
  `[MovedFrom(true, sourceNamespace: "UnityEngine.UI")]` for Unity's API Updater.
  *Migration:* add `using OpenUGD.UI;` to scripts that name the components.
- **Breaking: `UIFlippable` no longer moves itself above the other mesh effects.** In the editor its `Awake`
  and `OnValidate` called `UnityEditorInternal.ComponentUtility.MoveComponentUp` until it was the first
  `BaseMeshEffect` (WG-24). That moved the component in the scene, without undo, whenever another effect sat
  above it, and made it impossible to mirror the result of another effect. Effects now run in the order the components have.
  *Migration:* to keep the old result, drag `UIFlippable` above the other effects in the Inspector.
- **Breaking: Radial with `ModifyVertices` keeps the graphic's shape.** It used to replace the whole mesh with
  a 64-segment ellipse the size of the rect, which cropped rectangular graphics, discarded sliced and tiled
  geometry and reset the UVs to 0..1. It now cuts the existing triangles into 32 wedges around the centre and
  along a ring at every gradient key, interpolating every attribute, so the outline and the sprite's UVs
  stay. *Migration:* for a round shape, use a round sprite or a `Mask`.
- **Breaking: Diamond is the Manhattan distance from the centre of the vertex bounds**, reaching the
  gradient's end on the diamond whose corners touch the middle of each edge (the bounds' corners are at 2).
  It used to be the straight-line distance from `(center.y / 2, center.y / 2)`, scaled by the height only,
  so it was neither centred nor a diamond. *Migration:* none; for the old circular falloff use Radial.
- **Breaking: assigning `null` to `GradientMeshEffect.GradientColor` throws `ArgumentNullException`.** It used
  to be accepted and fail with a `NullReferenceException` at the next mesh rebuild. *Migration:* assign a
  `Gradient`.
- **Breaking: the display name is "OpenUGD uGUI Components"** instead of "UI Elements", the former name of
  UI Toolkit. *Migration:* none; the package ID `com.openugd.ui` is unchanged.
- **Menu entries.** *UI > EmptyGraphic* is now *UI > Empty Graphic*; *UI > Effects > Flippable* and
  *UI > Effects > Gradient* keep their paths and now sort after Unity's Shadow, Outline and Position As UV1.
  *Migration:* none.
- The Inspector-only *Modify Tangents* option now applies to all four gradient shapes; Radial and Diamond
  ignored it and wrote the vertex colour. *Migration:* to keep colouring a Radial or Diamond gradient
  through the vertex colour, turn *Modify Tangents* off on it.
- The property setters of both effects rebuild the mesh only when the value changes (`GradientColor` always
  does), so assigning a property its current value no longer forces a rebuild. *Migration:* call
  `SetVerticesDirty()` on the graphic to force one.
- A mesh that `ModifyVertices` would grow to 65,000 vertices or more, where `VertexHelper.FillMesh` throws,
  is coloured without extra vertices instead.
- **Breaking: minimum supported editor version raised to Unity 6000.0**, with `com.unity.ugui` 2.0.0.
  *Migration:* projects on an older editor stay on 0.1.1.
- `package.json`: version 2.0.0; `licensesUrl` and `changelogUrl` point at the default branch `main`
  instead of `master` (PK-14); the sample is listed under `samples`. It was earlier updated to the current
  Unity package manifest schema: a real `description`, `author` as an object, `licensesUrl`,
  `documentationUrl`, `changelogUrl` and `repository`; the obsolete `category` key was removed.
- `Runtime/com.openugd.ui.asmdef`: `rootNamespace` is `OpenUGD.UI`, it references `UnityEngine.UI` by name
  instead of relying on Unity adding it, and it spells out the full canonical key set.
- README rewritten: the namespace rationale is gone, the install snippets name 2.0.0, and it describes how the
  effects behave and how to upgrade.
- **Licence changed from MIT to Apache-2.0.** The previous `LICENSE` was a mutated MIT whose copyright
  line had been deleted and whose attribution clause was replaced with the literal text "No conditions.",
  which left it legally ambiguous. It is now the verbatim Apache License 2.0 with an explicit copyright
  holder, the file is named `LICENSE.md`, and `package.json` declares `"license": "Apache-2.0"`.
  Apache-2.0 adds an express patent grant and requires that changes to the files be stated; releases made
  before this version remain under their original terms.

### Fixed

- `UIFlippable` mirrored the mesh while disabled or inactive; `ModifyMesh` now returns when `IsActive()` is
  false (WG-23, UH-22).
- Setting `UIFlippable.horizontal` or `vertical` did not rebuild the mesh, so the change showed only at the
  next unrelated rebuild (WG-23). The setters of both effects now call `SetVerticesDirty`, through a Unity
  null check on the graphic, so they no longer throw when the graphic is missing or destroyed.
- `UIFlippable`'s serialized field `_veritical` is spelled `_vertical`; `[FormerlySerializedAs]` keeps saved
  values.
- `GradientMeshEffect` Radial was off-centre for any pivot other than the middle: its extra vertices were laid
  out around the local origin, while the colours were measured from the centre of the bounds (WG-25).
- Radial and Diamond with `ModifyVertices` never showed a gradient key between the ends, because no vertex
  sat at the key's distance. Diamond and the linear shapes now get vertices on every key, and Radial a ring.
- Vertices added by `ModifyVertices` were white, with no tangent and zeroed `uv1`..`uv3`, so with `Multiply`
  or `Add` they lost the graphic's colour and a custom shader lost its extra channels. Every attribute is now
  interpolated from the original triangle.
- `GradientMeshEffect` allocated on every mesh rebuild: a new vertex list, and with `ModifyVertices` a new
  stop list, the gradient's key arrays, and three lists and an array per triangle (WG-26, UH-18). The work lists now come from `ListPool`, and the gradient's key times are
  cached until the gradient changes, so a warm rebuild allocates nothing.

### Added

- XML documentation for every public type and member.
- EditMode tests (`Tests/Editor`, assembly `com.openugd.ui.tests`): the mirror, the triangle cutting, the
  gradient coordinates and the tessellation run without the engine; the component tests (flip about the rect
  centre, nothing while disabled, setters dirty the mesh, gradient end colours, blend modes, Radial and
  Diamond centred with a (0, 0) pivot, UVs inside the original UV rect, no allocation once warm, and
  `EmptyGraphic` emitting no vertices while accepting raycasts) carry `[Category("RequiresUnity")]`.
- The **Components Demo** sample (`Samples~/ComponentsDemo`): all three components on one canvas, built in
  code.
- README with install instructions, quick start, and an API overview.
- This changelog.
- `.gitignore` for a Unity UPM package repository.

### Removed

- `UIFlippable`'s editor-only `protected override` methods `Awake()` and `OnValidate()`, which did the
  reordering. *Migration:* a subclass that overrides them now overrides `UIBehaviour.Awake` and
  `BaseMeshEffect.OnValidate` instead; calls to `base.Awake()` and `base.OnValidate()` still compile.

## [0.1.1] - 2025-02-10

### Fixed

- Compilation error.

## [0.1.0] - 2025-01-31

### Added

- Initial release: `UIFlippable`, `GradientMeshEffect`, and `EmptyGraphic`.

[2.0.0]: https://github.com/openugd/upm-ui/compare/0.1.1...2.0.0
[0.1.1]: https://github.com/openugd/upm-ui/compare/0.1.0...0.1.1
[0.1.0]: https://github.com/openugd/upm-ui/releases/tag/0.1.0
