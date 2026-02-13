# Lip Daemon

The Lip Daemon (`lipd`) is a standalone JSON-RPC 2.0 server that exposes the core functionality of Lip for programmatic access. It is designed to be used by external tools, such as IDE extensions, GUIs, or other automation scripts, that need to interact with Lip packages and configuration.

> [!IMPORTANT]
> `lipd` is **not** the backend for the `lip` command-line interface. The `lip` CLI operates independently using the `Lip.Core` library directly. `lipd` is a separate entry point intended for machine-to-machine communication.

## Communication Protocol

- **Protocol**: [JSON-RPC 2.0](https://www.jsonrpc.org/specification)
- **Transport**: Standard Input/Output (stdio)
- **Encoding**: UTF-8

When `lipd` starts, it listens for JSON-RPC requests on `stdin` and writes responses to `stdout`.

## Exposed Methods

The daemon exposes the methods defined in the `ILipClient` interface.

### `Init`
Initializes a new `tooth.json` manifest in the current directory.
- **Parameters**: None
- **Returns**: `void`

### `Install`
Installs packages into the workspace.
- **Parameters**:
    - `packages`: `IEnumerable<string>` - List of packages to install (e.g. `owner/repo`, `package@version`, or local paths).
    - `dryRun`: `bool` - If true, simulates the installation without making changes.
    - `ignoreScripts`: `bool` - If true, skips running lifecycle scripts.
    - `noDependencies`: `bool` - If true, installs only the specified packages without their dependencies.
- **Returns**: `void`

### `Uninstall`
Uninstalls packages from the workspace.
- **Parameters**:
    - `packages`: `IEnumerable<string>` - List of packages to remove.
    - `dryRun`: `bool`
    - `ignoreScripts`: `bool`
    - `noDependencies`: `bool`
- **Returns**: `void`

### `Update`
Updates specified packages to their latest compatible versions.
- **Parameters**:
    - `packages`: `IEnumerable<string>`
    - `dryRun`: `bool`
    - `ignoreScripts`: `bool`
- **Returns**: `void`

### `List`
Lists installed packages.
- **Parameters**: None
- **Returns**: A tuple object containing:
    - `ExplicitInstalled`: List of explicitly installed packages.
    - `ImplicitInstalled`: List of implicitly installed (dependency) packages.

### `View`
Retrieves manifest information for a specific package.
- **Parameters**:
    - `package`: `string` - The package identifier.
- **Returns**: `string` - JSON string of the package manifest.

### `CacheClean`
Clears the global package cache.
- **Parameters**: None
- **Returns**: `void`

### `ConfigGet`
Retrieves a configuration value.
- **Parameters**: 
    - `key`: `string`
- **Returns**: `string` - The configuration value.

### `ConfigSet`
Sets a configuration value.
- **Parameters**:
    - `key`: `string`
    - `value`: `string`
- **Returns**: `void`

### `ConfigDelete`
Removes a configuration value.
- **Parameters**: 
    - `key`: `string`
- **Returns**: `void`

### `ConfigList`
Lists all configuration values.
- **Parameters**: None
- **Returns**: `IDictionary<string, string>` - Key-value pairs of configuration.

### `Migrate`
Migrates a `tooth.json` file from an older format to the current version.
- **Parameters**:
    - `file`: `string` - Path to the input file.
    - `output`: `string` - Path to the output file.
- **Returns**: `void`

### `Version`
Returns the version of the daemon.
- **Parameters**: None
- **Returns**: `string`

## Client Contract

Clients connecting to `lipd` must implement the `IClientContract` to handle notifications and callbacks from the daemon. These are JSON-RPC notifications sent from the daemon to the client.

| Method | Parameters | Description |
| :--- | :--- | :--- |
| `PrintInfo` | `message` (string) | Displays an informational message. |
| `PrintSuccess` | `message` (string) | Displays a success message. |
| `PrintWarning` | `message` (string) | Displays a warning message. |
| `PrintError` | `message` (string) | Displays an error message. |
| `ReportProgress` | `id` (string), `message` (string), `percentage` (double) | Reports progress for a long-running operation. `percentage` is between 0.0 and 1.0. |
