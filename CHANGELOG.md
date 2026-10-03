# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-10-03

The package joins the OpenUGD 2.0 family as 2.0.0, straight from 0.1.1. How to upgrade, with before and after
code, is in the README section *Upgrading to 2.0*.

### Added

- XML documentation for every public type and member.
- EditMode tests (`com.openugd.ui.tests`); the ones that need the engine carry `[Category("RequiresUnity")]`.
- The **Components Demo** sample (`Samples~/ComponentsDemo`): all three components on one canvas, built in code.
- A full README (0.1.1's held only a title) and this changelog.

### Changed

- **Breaking:** `EmptyGraphic`, `GradientMeshEffect` (with `Type` and `Blend`) and `UIFlippable` moved from
  `UnityEngine.UI` to `OpenUGD.UI`, with `[MovedFrom]`; script GUIDs are unchanged, so scenes and prefabs keep
  their components. Affects you if a script names them: add `using OpenUGD.UI;`.
- **Breaking:** `UIFlippable` no longer moves itself above the other mesh effects in the editor (WG-24).
  Affects you if it sat below another effect: drag it above to keep the 0.1.1 result.
- **Breaking:** Radial with `ModifyVertices` cuts the graphic's triangles instead of replacing the mesh with an
  ellipse, so the outline and UVs stay. Affects you if you relied on the round shape: use a round sprite or a
  `Mask`.
- **Breaking:** Diamond is the Manhattan distance from the centre of the vertex bounds. It was a straight-line
  distance scaled by the height, from a point that was the centre only for a middle pivot. Affects every
  Diamond gradient.
- **Breaking:** `GradientMeshEffect` caches its key positions. Affects you if you change `GradientColor` in
  place: assign it back.
- **Breaking:** assigning `null` to `GradientColor` throws `ArgumentNullException` (it failed later, at the next
  rebuild).
- **Breaking:** requires Unity 6000.0 and declares `com.unity.ugui` 2.0.0 (was Unity 2021.3, no dependency).
  Affects you on an older editor: stay on 0.1.1.
- *Modify Tangents* applies to Radial and Diamond too. Affects you if it is on for one: turn it off to keep
  the vertex colour.
- The setters of both effects rebuild the mesh only when the value changes; `GradientColor` always does.
- A mesh that `ModifyVertices` would grow to 65,000 vertices or more is coloured without extra vertices.
- Display name "OpenUGD uGUI Components" (was "UI Elements"); the package ID is unchanged.
- Menu: *UI > EmptyGraphic* is *UI > Empty Graphic*; *Flippable* and *Gradient* sort after Unity's effects.
- `package.json` follows the current manifest schema, with URLs on `main` (PK-14); the asmdef's root namespace
  is `OpenUGD.UI` and it references `UnityEngine.UI` explicitly.
- Licence: Apache-2.0, in `LICENSE.md`; 0.1.1 shipped the MIT licence in `LICENSE`. Releases before 2.0.0
  keep their original terms.

### Removed

- `UIFlippable`'s editor-only `Awake()` and `OnValidate()` overrides, which did the reordering. Affects you if
  a subclass overrides them: it now overrides the base-class methods; `base` calls still compile.

### Fixed

- `UIFlippable` mirrored the mesh while disabled or inactive (WG-23, UH-22).
- Setting `UIFlippable.horizontal` or `vertical` did not rebuild the mesh (WG-23).
- `GradientMeshEffect`'s setters threw when the graphic was missing or destroyed.
- `UIFlippable`'s serialized field `_veritical` is `_vertical`; `[FormerlySerializedAs]` keeps saved values.
- With `ModifyVertices`, Radial's mesh was laid out around the pivot, off-centre for any pivot other than the
  middle (WG-25).
- Radial and Diamond with `ModifyVertices` never showed a key between the ends.
- Vertices added by `ModifyVertices` were white, with no tangent and zeroed `uv1`..`uv3`, so `Multiply` and
  `Add` lost the graphic's colour there.
- `GradientMeshEffect` allocated on every rebuild (WG-26, UH-18); a warm rebuild allocates nothing.

## [0.1.1] - 2025-02-10

### Fixed

- Compilation error.

## [0.1.0] - 2025-01-31

### Added

- Initial release: `UIFlippable`, `GradientMeshEffect`, and `EmptyGraphic`.

[2.0.0]: https://github.com/openugd/upm-ui/compare/0.1.1...2.0.0
[0.1.1]: https://github.com/openugd/upm-ui/compare/0.1.0...0.1.1
[0.1.0]: https://github.com/openugd/upm-ui/releases/tag/0.1.0
