# Project Guidelines

- Before building a feature, component, utility, or integration from scratch, first search for and evaluate existing packages, libraries, framework capabilities, and project dependencies that can solve the need.
- Prefer a maintained, well-supported existing solution when it meets the requirements; implement a custom solution only when the available options are unsuitable, introduce unacceptable trade-offs, or do not exist.
- When adding a dependency, choose the smallest appropriate package and avoid duplicating functionality already available in the codebase or its current dependencies.
- Before creating page-local UI, identify recurring layout or interaction patterns. Extract components that are reused or likely to be reused across routes (for example navigation, language controls, cards, filters, and dialogs); keep only truly page-specific markup in page files.
