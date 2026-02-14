# lipd

`lipd` is a standalone JSON-RPC 2.0 server that exposes the core functionality of lip for programmatic access. It is designed to be used by external tools, such as IDE extensions, GUIs, or other automation scripts, that need to interact with lip packages and configuration.

## Communication Protocol

- **Protocol**: [JSON-RPC 2.0](https://www.jsonrpc.org/specification)
- **Transport**: Standard Input/Output (stdio)
- **Encoding**: UTF-8

When `lipd` starts, it listens for JSON-RPC requests on `stdin` and writes responses to `stdout`.

## Exposed Methods

The daemon exposes the following JSON-RPC methods:

### `Init`
Initializes a new `tooth.json` manifest in the current directory.
- **Parameters**: None
- **Returns**: `null`

### `Install`
Installs packages into the workspace.
- **Parameters**:
    - `packages`: List of strings - List of packages to install (e.g. `owner/repo`, `package@version`, or local paths).
    - `dryRun`: Boolean - If true, simulates the installation without making changes.
    - `ignoreScripts`: Boolean - If true, skips running lifecycle scripts.
    - `noDependencies`: Boolean - If true, installs only the specified packages without their dependencies.
- **Returns**: `null`

### `Uninstall`
Uninstalls packages from the workspace.
- **Parameters**:
    - `packages`: List of strings - List of packages to remove.
    - `dryRun`: Boolean
    - `ignoreScripts`: Boolean
    - `noDependencies`: Boolean
- **Returns**: `null`

### `Update`
Updates specified packages to their latest compatible versions.
- **Parameters**:
    - `packages`: List of strings
    - `dryRun`: Boolean
    - `ignoreScripts`: Boolean
- **Returns**: `null`

### `List`
Lists installed packages.
- **Parameters**: None
- **Returns**: An object containing:
    - `ExplicitInstalled`: List of explicitly installed packages.
    - `ImplicitInstalled`: List of implicitly installed (dependency) packages.

### `View`
Retrieves manifest information for a specific package.
- **Parameters**:
    - `package`: String - The package identifier.
- **Returns**: String - JSON string of the package manifest.

### `CacheClean`
Clears the global package cache.
- **Parameters**: None
- **Returns**: `null`

### `ConfigGet`
Retrieves a configuration value.
- **Parameters**:
    - `key`: String
- **Returns**: String - The configuration value.

### `ConfigSet`
Sets a configuration value.
- **Parameters**:
    - `key`: String
    - `value`: String
- **Returns**: `null`

### `ConfigDelete`
Removes a configuration value.
- **Parameters**:
    - `key`: String
- **Returns**: `null`

### `ConfigList`
Lists all configuration values.
- **Parameters**: None
- **Returns**: Dictionary (Map) - Key-value pairs of configuration.

### `Migrate`
Migrates a `tooth.json` file from an older format to the current version.
- **Parameters**:
    - `file`: String - Path to the input file.
    - `output`: String - Path to the output file.
- **Returns**: `null`

### `Version`
Returns the version of the daemon.
- **Parameters**: None
- **Returns**: String

## Client Contract

Clients connecting to `lipd` must handle the following JSON-RPC notifications sent from the daemon:

| Method | Parameters | Description |
| :--- | :--- | :--- |
| `PrintInfo` | `message` (string) | Displays an informational message. |
| `PrintSuccess` | `message` (string) | Displays a success message. |
| `PrintWarning` | `message` (string) | Displays a warning message. |
| `PrintError` | `message` (string) | Displays an error message. |
| `ReportProgress` | `id` (string), `message` (string), `percentage` (number) | Reports progress for a long-running operation. `percentage` is between 0.0 and 1.0. |
