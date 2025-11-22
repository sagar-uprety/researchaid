# Copilot Instructions for ResearchAid Plugin

## Project Overview

This is a **Loupedeck plugin** (.NET 8.0) that extends the Loupedeck/Razer Stream Controller hardware. The plugin architecture is event-driven, where user interactions with dials and buttons trigger commands and adjustments defined in the codebase.

**Official Documentation**: Reference the [Loupedeck Actions SDK Documentation](https://logitech.github.io/actions-sdk-docs/) for API details, best practices, and code examples when implementing actions.

## Architecture & Key Concepts

### Plugin Entry Point

- `ResearchAidPlugin.cs` is the main plugin class (inherits `Plugin`)
- Set `UsesApplicationApiOnly = true` and `HasNoApplication = true` for standalone plugins
- Initialize helpers in constructor: `PluginLog.Init()` and `PluginResources.Init()`

### Action Types (in `src/Actions/`)

Create classes inheriting from these base types:

- **Commands** (`PluginDynamicCommand`): Handle button presses
  - Override `RunCommand()` for execution logic
  - Override `GetCommandDisplayName()` to update button display text
  - Call `ActionImageChanged()` after state changes to refresh UI
- **Adjustments** (`PluginDynamicAdjustment`): Handle dial rotations
  - Override `ApplyAdjustment()` to handle rotation ticks (`diff` parameter)
  - Override `RunCommand()` for reset functionality (if `hasReset: true` in constructor)
  - Override `GetAdjustmentValue()` to display current value
  - Call `AdjustmentValueChanged()` after state changes

### Application Integration

- `ResearchAidApplication.cs` links plugin to desktop apps (currently unused/empty)
- Override `GetProcessName()` (Windows) and `GetBundleName()` (macOS) to integrate

## Development Workflow

### Building & Testing

```bash
# Build for development (auto-reloads plugin)
dotnet build -c Debug

# Build for distribution
dotnet build -c Release
```

**Auto-reload mechanism**: Debug builds create `.link` files in `~/Library/Application Support/Logi/LogiPluginService/Plugins/` (macOS) or `%LocalAppData%\Logi\LogiPluginService\Plugins\` (Windows) and trigger `loupedeck:plugin/ResearchAid/reload`

### Debugging

Attach to `LogiPluginService` process using `.vscode/launch.json` configuration

### Packaging & Distribution

```bash
logiplugintool pack ./bin/Release ./ResearchAid.lplug4
logiplugintool install ./ResearchAid.lplug4
```

## Project Structure Conventions

### Namespace

All code uses `Loupedeck.ResearchAidPlugin` namespace (defined in `.csproj` as `RootNamespace`)

### Output Paths

- Build output: `bin/{Configuration}/bin/` (not standard)
- Package metadata: Copied from `src/package/metadata/` to output during build
- Plugin short name: `ResearchAid` (used in reload URLs and package name)

### Critical Files

- `src/package/metadata/LoupedeckPackage.yaml`: Plugin metadata, device support, capabilities
- `src/ResearchAidPlugin.csproj`: Build configuration, platform-specific paths, auto-reload setup

### Helper Utilities

- `PluginLog`: Static wrapper around `PluginLogFile` (must call `Init()` in plugin constructor)
  - Methods: `Verbose()`, `Info()`, `Warning()`, `Error()`
- `PluginResources`: Access embedded resources (set Build Action to "Embedded Resource")

## Platform-Specific Notes

- Plugin API DLL location differs by OS (defined in `.csproj`):
  - Windows: `C:\Program Files\Logi\LogiPluginService\PluginApi.dll`
  - macOS: `/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/PluginApi.dll`
- URL scheme differs: `start loupedeck:` (Windows) vs `open loupedeck:` (macOS)

## Device Support

Target devices in `LoupedeckPackage.yaml`:

- `LoupedeckCtFamily`: Loupedeck CT, Live, Live S, Razer Stream Controller/X
- `LoupedeckPlusFamily`: Loupedeck+ (commented out by default)

## Common Patterns

1. **State Management**: Use instance variables in command/adjustment classes
2. **UI Updates**: Always call `ActionImageChanged()` or `AdjustmentValueChanged()` after state changes
3. **Logging**: Use `PluginLog.Info($"Message {variable}")` for diagnostics
4. **Null Safety**: Nullable disabled project-wide (`<Nullable>disable</Nullable>`)
