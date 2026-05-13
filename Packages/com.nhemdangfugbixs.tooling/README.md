<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="docs/assets/brand/nhem-studio-logo-dark.svg">
    <source media="(prefers-color-scheme: light)" srcset="docs/assets/brand/nhem-studio-logo-light.svg">
    <img alt="NHEM Studio Logo" src="docs/assets/brand/nhem-studio-logo-light.svg" width="220">
  </picture>
</p>

<h1 align="center">NhemDangFugBixs.VContainer.SourceGenerator</h1>

<p align="center">
  <strong>Compile-time VContainer workflow tooling for Unity projects.</strong>
</p>

<p align="center">
  Attribute-driven registration · Source-generated installers · DI analyzers · Scope marker architecture · CLI preflight
</p>

<p align="center">
  <a href="https://github.com/NhomNhem/NhemDangFugBixs.Tooling/actions">
    <img src="https://img.shields.io/github/actions/workflow/status/NhomNhem/NhemDangFugBixs.Tooling/ci.yml?branch=main&style=flat-square&label=CI" alt="CI Status">
  </a>
  <a href="https://github.com/NhomNhem/NhemDangFugBixs.Tooling/releases">
    <img src="https://img.shields.io/github/v/release/NhomNhem/NhemDangFugBixs.Tooling?style=flat-square&label=Release" alt="Release">
  </a>
  <a href="https://openupm.com/packages/com.nhemdangfugbixs.tooling/">
    <img src="https://img.shields.io/npm/v/com.nhemdangfugbixs.tooling?label=OpenUPM&registry_uri=https://package.openupm.com&style=flat-square" alt="OpenUPM">
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/NhomNhem/NhemDangFugBixs.Tooling?style=flat-square" alt="License">
  </a>
</p>

---

## Overview

`NhemDangFugBixs.VContainer.SourceGenerator` is a Unity package and Roslyn toolchain for making [VContainer](https://github.com/hadashiA/VContainer) dependency registration safer, faster, and easier to maintain.

It provides:

- Source-generated VContainer registration.
- Analyzer diagnostics for invalid or duplicate registrations.
- Marker-based scope mapping across asmdef boundaries.
- CLI validation through `di-smoke`.
- Unity package assets for runtime attributes, editor tooling, analyzers, samples, and documentation.

The package auto-registers types decorated with `[AutoRegister]` and `[AutoRegisterIn]`, validates scope usage, and catches common DI mistakes before Play Mode.

```txt
Core principle:
Your architecture stays yours.
The package makes it compile-time checked.
```

---

## Why This Exists

In real Unity projects, VContainer setup can become fragile as the project grows.

Common problems include:

- `LifetimeScope.Configure()` becomes too large.
- Services are registered in the wrong scope.
- Duplicate registrations are difficult to find.
- Runtime gameplay services are accidentally registered as global singletons.
- Application assemblies reference Composition assemblies.
- Entry points are registered as normal services.
- `IObjectResolver` is used like a service locator.
- AI coding agents add inconsistent registrations.

This package turns DI registration into explicit metadata, generated installers, and analyzer-backed architecture rules.

---

## Recommended Architecture

The recommended pattern is **marker-based scope mapping**.

Services reference a conceptual scope marker:

```csharp
[AutoRegisterIn<IGameplayScope>(Lifetime = NhemLifetime.Scoped)]
[As<IPhaseStateMachine>]
public sealed class PhaseStateMachine : IPhaseStateMachine
{
}
```

Composition maps that marker to a real VContainer `LifetimeScope`:

```csharp
[LifetimeScopeFor<IGameplayScope>]
public sealed class GameplayLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterGeneratedFor<IGameplayScope>();
    }
}
```

This keeps dependency direction clean:

```txt
Application    -> Shared
Infrastructure -> Shared
Composition    -> Shared
Composition    -> Application
Composition    -> Infrastructure
```

Avoid this:

```txt
Application -> Composition
```

---

## Features

<table>
  <tr>
    <td width="50%">
      <h3>Attribute-driven registration</h3>
      <p>Declare registration intent directly on services using attributes such as <code>[AutoRegisterIn]</code>, <code>[As]</code>, and <code>[EntryPoint]</code>.</p>
    </td>
    <td width="50%">
      <h3>Source-generated installers</h3>
      <p>Generate readable VContainer registration code instead of maintaining large manual <code>LifetimeScope.Configure()</code> methods.</p>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <h3>Scope marker architecture</h3>
      <p>Keep lower-level asmdefs independent from Unity Composition assemblies through marker-based scope mapping.</p>
    </td>
    <td width="50%">
      <h3>Analyzer guardrails</h3>
      <p>Catch duplicate registrations, invalid scope mappings, unsafe injection styles, and service locator misuse early.</p>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <h3>CLI preflight</h3>
      <p>Validate project DI setup before entering Play Mode or before shipping CI builds.</p>
    </td>
    <td width="50%">
      <h3>Unity package-ready</h3>
      <p>Ships with Runtime, Editor, Analyzers, Samples, and Documentation folders for Unity Package Manager workflows.</p>
    </td>
  </tr>
</table>

---

## Installing

### OpenUPM

Preferred for production projects once the package is published to the registry:

```bash
openupm add com.nhemdangfugbixs.tooling
```

Install VContainer separately in the Unity project before using this package.

### Git URL Fallback

If you need the Git-based package before or alongside OpenUPM, add the Unity-ready branch:

```text
https://github.com/NhomNhem/NhemDangFugBixs.Tooling.git?path=/&branch=deploy
```

The `deploy` branch stays minimal for Unity Package Manager imports.

The source branch keeps the package self-contained so release tags are also suitable for registry publishing workflows such as OpenUPM.

> Prerequisite: install VContainer in the Unity project first.  
> This package does not auto-install VContainer for Git-based imports.

---

## Quick Start

### 1. Build the toolchain

```bash
dotnet build Source~/NhemDangFugBixs.Tooling.sln -c Release
```

### 2. Run the preflight validator

```bash
dotnet di-smoke preflight MyGame.csproj
```

### 3. Optionally validate an emitted assembly

```bash
dotnet di-smoke validate bin/Debug/net10.0/MyGame.dll --format json
```

---

## Minimal Usage Example

Create a scope marker in a shared assembly:

```csharp
public interface IScopeMarker
{
}

public interface IGameplayScope : IScopeMarker
{
}
```

Register a service with attributes:

```csharp
[AutoRegisterIn<IGameplayScope>(Lifetime = NhemLifetime.Scoped)]
[As<IPhaseStateMachine>]
public sealed class PhaseStateMachine : IPhaseStateMachine
{
}
```

Map the marker to a real VContainer scope:

```csharp
[LifetimeScopeFor<IGameplayScope>]
public sealed class GameplayLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterGeneratedFor<IGameplayScope>();
    }
}
```

Generated registration intent:

```csharp
builder.Register<PhaseStateMachine>(Lifetime.Scoped)
    .As<IPhaseStateMachine>();
```

---

## Repository Layout

```txt
.
├── Source~/
│   └── C# solution containing CLI, generators, analyzers, and supporting libraries.
├── Runtime/
│   └── Unity runtime attributes and lightweight models.
├── Editor/
│   └── Unity Editor diagnostics, menu items, and workflow tools.
├── Analyzers/
│   └── Analyzer and source generator assets for Unity.
├── Samples~/
│   └── Sample architectures for basic registration, scope markers, MessagePipe, and scene components.
├── Documentation~/
│   └── Unity Package Manager documentation.
├── website/
│   └── Astro Starlight documentation site.
├── docs/
│   └── Project documentation assets, diagrams, and branding.
└── .github/workflows/
    └── Validation, release, docs, and deploy automation.
```

---

## Package Deployment

The repository uses two package surfaces:

| Branch | Purpose |
|---|---|
| `main` / source branch | Full source, tooling, docs, CI, and release-ready package files. |
| `deploy` | Minimal Unity Package Manager import surface. |

`deploy.yml` builds the .NET projects, filters `package.json` down to UPM-safe fields, copies Unity assets into `deploy/`, and publishes that directory to the `deploy` branch.

Source tags must still contain a valid package manifest plus Unity assets so OpenUPM or any tag-based packaging flow can consume the repository without relying on the deploy workflow output.

---

## Building, Testing, and Docs

### Build everything

```bash
dotnet build Source~/NhemDangFugBixs.Tooling.sln --no-restore -c Release
```

### Run tests

```bash
dotnet test Source~/NhemDangFugBixs.Tooling.sln --no-build -c Release
```

### Build docs

```bash
cd website
pnpm install
pnpm run build
```

---

## CLI

The CLI command is:

```bash
di-smoke
```

Common commands:

```bash
dotnet di-smoke preflight MyGame.csproj
```

```bash
dotnet di-smoke validate bin/Debug/net10.0/MyGame.dll --format json
```

```bash
dotnet di-smoke graph MyGame.csproj --scope IGameplayScope --format mermaid
```

```bash
dotnet di-smoke report MyGame.csproj --format markdown --out docs/generated/di-map.md
```

---

## Troubleshooting Duplicate Registrations

Common causes:

- Old generated `.g.cs` files remain in `Generated/` or `Assets/Plugins/Analyzers/`.
- A type with the same full name exists in multiple assemblies.
- `[AutoRegister]` or `[AutoRegisterIn]` annotations overlap scopes.
- `AllowedAssemblies` is misconfigured.
- Stale generated files are still compiled by Unity.

Quick fixes:

1. Upgrade to generator `v7.0.0` or newer for improved dedupe filtering.
2. Delete stale generated files under `**/Generated/*.g.cs` in the Unity project.
3. Rebuild the Unity project.
4. Run `dotnet di-smoke preflight` before Play Mode.
5. Consolidate duplicate implementations or move shared logic into common asmdef references.

---

## Release Process

1. Update `package.json`.
2. Update `CHANGELOG.md`.
3. Update release notes.
4. Run CI and release-readiness validation.
5. Create a tag that exactly matches `package.json.version`.

Example:

```bash
git tag v6.0.5
git push origin v6.0.5
```

Pushing the tag triggers release packaging and docs deployment.

---

## Design Goals

This package is designed to:

- Reduce manual VContainer registration boilerplate.
- Support layered Unity architecture with asmdef boundaries.
- Keep Application and Domain layers independent from Composition assemblies.
- Detect DI mistakes early through analyzers.
- Generate readable and stable registration code.
- Provide generated DI documentation and dependency graphs.
- Support small indie projects and larger modular projects.
- Stay reusable outside a single game project.

---

## Non-goals

This package does not aim to:

- Replace VContainer.
- Hide VContainer concepts completely.
- Force a specific folder structure beyond Unity package requirements.
- Force scope names such as `Project`, `Gameplay`, or `MainMenu`.
- Auto-register everything without explicit opt-in.
- Become a runtime service locator.
- Require game code to depend directly on Editor assemblies.

---

## License

Released under the ISC license.

See [LICENSE](LICENSE).

---

<p align="center">
  <sub>Built by NHEM Studio for safer Unity architecture.</sub>
</p>
