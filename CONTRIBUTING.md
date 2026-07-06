# Contributing

This repository is being prepared as a product-quality Unity package.
Contributions should preserve package stability, Unity import cleanliness, and
the left-control / right-preview editor workflow.

## Development Rules

- Keep Unity `.meta` files for every package asset.
- Do not commit `Library/`, `Temp/`, generated projects, local screenshots, or user settings.
- Prefer small, scoped changes with a clear changelog entry.
- Keep public-facing text professional and product-ready.
- Do not change license terms or package identity without explicit owner approval.

## Validation

Before submitting changes:

1. Run `git diff --check`.
2. Run the Unity EditMode/batch test suite.
3. Check `git status --short` for unwanted generated files.
4. For public-release changes, run a tracked-file secret scan.

## UI Standard

Editor tools should follow:

- [Docs/CommonEditorToolLayoutSpec.md](Docs/CommonEditorToolLayoutSpec.md)

Water material look presets should follow:

- [Docs/MaterialLookPresetGuide.md](Docs/MaterialLookPresetGuide.md)
