# Lip Daemon (`lipd`)

The `lipd` (lip daemon) serves as the core engine for lip operations. The command-line interface (`lip`) acts as a client that communicates with `lipd` using JSON-RPC over standard input/output (stdio).

This architecture allows for:
- Separation of concern between UI (CLI) and logic (Daemon).
- Potential for other clients (IDEs, GUIs) to interact with lip programmatically.

## Communication Protocol

- **Protocol**: JSON-RPC 2.0
- **Transport**: Standard I/O (stdin/stdout)
- **Encoding**: UTF-8

When you run `lipd`, it starts a JSON-RPC server listening on stdin and writing responses to stdout.

## Exposed Methods

The daemon exposes a set of methods via the `ILipClient` interface.

### `Init`
Initializes a new `tooth.json` manifest in the current directory.
- **Returns**: `void`

### `Install`
Installs packages into the workspace.
- **Parameters**: 
    - `packages`: `IEnumerable<string>` - List of packages to install.
    - `dryRun`: `bool` - If true, simulates the installation.
    - `ignoreScripts`: `bool` - If true, skips running lifecycle scripts.
    - `noDependencies`: `bool` - If true, installs only the specified packages without their dependencies.

### `Uninstall`
Uninstalls packages from the workspace.
- **Parameters**:
    - `packages`: `IEnumerable<string>` - List of packages to remove.
    - `dryRun`: `bool`
    - `ignoreScripts`: `bool`
    - `noDependencies`: `bool`

### `Update`
Updates specified packages to their latest compatible versions.
- **Parameters**:
    - `packages`: `IEnumerable<string>`
    - `dryRun`: `bool`
    - `ignoreScripts`: `bool`

### `List`
Lists installed packages.
- **Returns**: A tuple containing `ExplicitInstalled` and `ImplicitInstalled` package lists.

### `View`
Retrieves manifest information for a specific package.
- **Parameters**:
    - `package`: `string` - The package identifier.
- **Returns**: `string` (JSON serialization of `PackageManifest`).

### `CacheClean`
Clears the global package cache.
- **Returns**: `void`

### `ConfigGet`
Retrieves a configuration value.
- **Parameters**: `key` (string)
- **Returns**: `string`

### `ConfigSet`
Sets a configuration value.
- **Parameters**: 
    - `key`: `string`
    - `value`: `string`

### `ConfigDelete`
Removes a configuration value.
- **Parameters**: `key` (string)

### `ConfigList`
Lists all configuration values.
- **Returns**: `IDictionary<string, string>`

### `Migrate`
Migrates a `tooth.json` file from an older format to the current version.
- **Parameters**:
    - `file`: `string` - Path to input file.
    - `output`: `string` - Path to output file.

### `Version`
Returns the version of the daemon.
- **Returns**: `string`

## Client Contract

The client connecting to `lipd` (like the `lip` CLI) must implement `IClientContract` to handle notifications and callbacks from the daemon.

- `PrintInfo(string message)`
- `PrintSuccess(string message)`
- `PrintWarning(string message)`
- `PrintError(string message)`
- `ReportProgress(string id, string message, double percentage)`
