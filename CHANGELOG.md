# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Minimum supported editor version raised to Unity 2022.3.
- `package.json` updated to the current Unity package manifest schema: real
  `description`, `author` as an object, `licensesUrl`, `documentationUrl`,
  `changelogUrl` and `repository`; the obsolete `category` key was removed.
- `Runtime/com.openugd.ui.asmdef` now spells out the full canonical key set
  instead of relying on editor defaults.
- **Licence changed from MIT to Apache-2.0.** The previous `LICENSE` was a mutated MIT whose copyright
  line had been deleted and whose attribution clause was replaced with the literal text "No conditions.",
  which left it legally ambiguous. It is now the verbatim Apache License 2.0 with an explicit copyright
  holder, the file is named `LICENSE.md`, and `package.json` declares `"license": "Apache-2.0"`.
  Apache-2.0 adds an express patent grant and requires that changes to the files be stated; releases made
  before this version remain under their original terms.

### Added

- README with install instructions, quick start, and an API overview.
- This changelog.
- `.gitignore` for a Unity UPM package repository.

## [0.1.1] - 2025-02-10

### Fixed

- Compilation error.

## [0.1.0] - 2025-01-31

### Added

- Initial release: `UIFlippable`, `GradientMeshEffect`, and `EmptyGraphic`.

[Unreleased]: https://github.com/openugd/upm-ui/compare/0.1.1...HEAD
[0.1.1]: https://github.com/openugd/upm-ui/compare/0.1.0...0.1.1
[0.1.0]: https://github.com/openugd/upm-ui/releases/tag/0.1.0
