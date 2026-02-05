# lip

lip is a general package manager

## Usage

```
lip [OPTIONS] [COMMAND]
```

## Options

- `-h, --help`: Prints help information
- `-q, --quiet`: Show only errors
- `-v, --verbose`: Show verbose output
- `-V, --version`: Show version and exit

## Commands

- `cache`: Inspect and manage lip's cache
- `config`: Manage the lip configuration files
- `init`: Initialize a new tooth in the current directory
- `install`: Install packages and their dependencies from various sources
- `list`: List installed packages
- `migrate <path>`: Migrate a tooth.json file to the latest format
- `uninstall <package ...>`: Uninstall packages
- `update <package ...>`: Update installed packages to new versions
- `view <package>`: View package information
