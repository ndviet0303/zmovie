# Backend domain time convention

Application code owns access to the wall clock. `AddZMovieApplication` registers
`TimeProvider.System` as the default, and tests or other hosts may register a
replacement before calling that method.

When time affects a domain decision:

1. Inject `TimeProvider` into the application handler or coordinator.
2. Read `GetUtcNow()` once for the operation.
3. Pass the resulting `DateTimeOffset` into the domain factory or behavior method.
4. Persist the value chosen by the domain object.

Domain code must not introduce `DateTime.UtcNow`, `DateTimeOffset.UtcNow`,
`DateTime.Now`, or `TimeProvider.System`. The architecture test keeps an exact
temporary baseline for legacy entity initializers. Each aggregate migration must
remove its corresponding baseline entry rather than adding another exception.
