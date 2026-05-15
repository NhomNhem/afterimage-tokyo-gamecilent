# NhemBootstrap

A Unity Editor tool that automates Clean Architecture project setup by creating folder structures, generating assembly definition files (asmdefs), and installing packages via Unity Package Manager (UPM).

## Requirements

- Unity 2021.3 or higher
- UniTask (installed automatically or via the Bootstrap Window)

## Installation

### Option A: Import UnityPackage
1. Download `NhemBootstrap_*.unitypackage` from the releases page
2. In Unity: **Assets > Import Package > Custom Package...**
3. Select the downloaded file and click **Import**

### Option B: Copy Folder
Copy the `Assets/NhemBootstrap` folder into your Unity project's `Assets` directory.

## Quick Start

1. Open the Bootstrap Window: **Tools > Nhem Bootstrap**
2. (Optional) Drag a `BootstrapConfig` asset into the **Bootstrap Config** field, or select a profile from the **Profile** dropdown
3. Review the step list and toggle steps on/off as needed
4. Click **Apply Selected Steps**
5. Monitor progress in the progress bar and log panel
6. Use the **Cancel** button to stop mid-run if needed

## Configuration

### BootstrapConfig (ScriptableObject)

Create a config asset via **Assets > Create > Nhem > Bootstrap Config**.

| Field | Type | Description |
|-------|------|-------------|
| `profile` | `ConfigProfile` | Preset profile (Minimal, Full, Custom) |
| `packages` | `List<PackageEntry>` | UPM packages to install |
| `folders` | `List<FolderEntry>` | Folders to create |
| `asmdefs` | `List<AsmdefEntry>` | Assembly definitions to generate |

### PackageEntry

| Field | Description |
|-------|-------------|
| `displayName` | Human-readable name shown in the UI |
| `gitUrl` | Git URL for the package |
| `version` | Optional branch/tag/commit (appended as `#version` to the URL) |
| `enabled` | Whether to include in the bootstrap run |

### FolderEntry

| Field | Description |
|-------|-------------|
| `path` | Project-relative path (e.g. `Assets/_Project/Domain`) |
| `enabled` | Whether to include in the bootstrap run |

### AsmdefEntry

| Field | Description |
|-------|-------------|
| `name` | Assembly name (e.g. `MyProject.Domain`) |
| `targetFolder` | Folder where the `.asmdef` file is written |
| `references` | List of assembly names to reference |
| `autoReferenced` | Whether all assemblies auto-reference this one |
| `enabled` | Whether to include in the bootstrap run |

## Configuration Profiles

| Profile | Folders | Packages | Asmdefs |
|---------|---------|----------|---------|
| **Minimal** | `Assets/_Project` only | None | None |
| **Full** | All Clean Architecture folders | UniTask, VContainer, R3, MessagePipe, ZLogger | None |
| **Custom** | User-defined | User-defined | User-defined |

### Generating Sample Configs

Run **Tools > Nhem Bootstrap > Generate Sample Configs** to create `MinimalBootstrapConfig.asset`, `FullBootstrapConfig.asset`, and `CustomBootstrapConfig.asset` in `Assets/NhemBootstrap/Sample/`.

## Async Execution & Cancellation

The Bootstrap Window runs steps asynchronously using UniTask. During execution:
- The **Cancel** button becomes active
- The progress bar shows per-step completion
- The current step name is displayed below the progress bar

Clicking **Cancel** stops execution after the current step completes. Completed steps retain their effects.

## Rollback

If a run fails or is cancelled, you can roll back created folders and asmdefs. The runner tracks all items created during a session and can remove them via `BootstrapRunner.RollbackAsync(snapshot, context)`.

## Export as UnityPackage

Run **Tools > Nhem Bootstrap > Export as UnityPackage** to package the tool for distribution. The output `.unitypackage` file is saved to `<ProjectRoot>/Exports/` and the folder is opened automatically.

## Environment Validation

On every domain reload, `EnvironmentValidator` checks:
- Unity version ≥ 2021.3 (error if below)
- `com.unity.nuget.newtonsoft-json` presence (warning if missing)

Results are shown in the **System Status** panel at the top of the Bootstrap Window.

## Extending the Tool

### Synchronous Step

```csharp
public class MyStep : IBootstrapStep {
    public string Name => "My Custom Step";
    public bool CheckCompleted() => /* check if already done */;
    public void Execute(BootstrapContext context) {
        // your logic here
        context.Log("My step ran!");
    }
}
```

### Async Step

```csharp
public class MyAsyncStep : IAsyncBootstrapStep {
    public string Name => "My Async Step";
    public bool CheckCompleted() => false;
    public void Execute(BootstrapContext context) => ExecuteAsync(context, default).Forget();
    public async UniTask ExecuteAsync(BootstrapContext context, CancellationToken ct) {
        await UniTask.Delay(1000, cancellationToken: ct);
        context.Log("Async step done!");
    }
}
```

Add your step to the window's step list in `BootstrapWindow.Init()`.

## License

MIT License. See `Assets/NhemBootstrap/Documentation/Third-Party Notices.txt` for third-party attributions.
