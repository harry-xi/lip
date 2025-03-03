# liprc.json

The `liprc.json` file serves as the configuration file for lip, enabling you to configure settings such as caching, proxies, and script execution. lip stores this file at `%APPDATA%\lip\liprc.json` for Windows and `~/.config/lip/liprc.json` for POSIX-like systems.

Project-specific settings take precedence over user-wide settings. All configuration fields are optional.

## Configuration Fields

### cache

- Type: `string`
- Default: `%LocalAppData%\lip\cache` (Windows) or `~/.local/share/lip/cache` (POSIX-like systems)

Defines the directory path for storing cached files.

### github_proxies

- Type: `string`
- Default: "" (empty string)

Sets proxy URLs (separated by commas) for GitHub connections. When left empty, lip connects to GitHub directly.

### go_module_proxies

- Type: `string`
- Default: `https://proxy.golang.org`

Defines Go module proxy URLs (separated by commas) for Go module downloads.
