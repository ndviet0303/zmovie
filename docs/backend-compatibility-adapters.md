# Backend compatibility adapter registry

Compatibility adapters are temporary migration tools, not permanent application
ports. A slice may introduce one only when all callers cannot move atomically
while keeping the backend deployable.

Every adapter must:

- use the `CompatibilityAdapter` suffix;
- delegate from one named legacy contract to one named owner-module use case;
- preserve the characterized behavior and contain no new business rule;
- have an owner, an atomic-cutover constraint, and a task-number deletion
  checkpoint in the active registry below;
- be deleted as part of that checkpoint, together with its registry row.

Existing stores such as `EfUserLibraryStore` and `EfAdminStore` are legacy source
implementations. They are not compatibility adapters and are removed by their
slice-specific tasks.

## Active registry

No compatibility adapters are active. Review, library/progress, Identity,
Catalog, Analytics, Personalization, and Backoffice should each move atomically
unless implementation evidence demonstrates that a temporary adapter is needed.
