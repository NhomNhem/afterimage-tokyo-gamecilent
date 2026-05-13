# Changelog

## 6.1.0 - 2026-05-13

### Added
- Marker-first VContainer workflow direction across runtime attributes, generator output, and diagnostics.
- Scope-marker mapping support with `LifetimeScopeFor` bridging marker identities to concrete `LifetimeScope` owners.
- Generated installer entry points for marker scopes (`RegisterGeneratedFor<TScopeMarker>` and non-generic variants).
- Expanded generator tests for scope mapping, bindings, entry points, components, and cross-assembly discovery.
- Expanded analyzer test suites for scope mapping, injection style, lifetime diagnostics, and resolver misuse patterns.
- Unity Editor diagnostics smoke tests for window creation and render path stability.
- Package samples for:
  - Basic registration
  - Scope marker architecture
  - MessagePipe integration
  - Scene component registration

### Changed
- Documentation now emphasizes marker-based architecture and generated installer usage as the default workflow.
- Diagnostics and validation messaging aligned with architecture guardrails and preflight usage.

### Validation
- Generator tests, analyzer tests, and CLI validation tests executed as implementation signoff gates.
