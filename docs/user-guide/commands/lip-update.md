# lip update

Update packages and their dependencies from various sources

## Usage

```
lip update <package ...> [OPTIONS]
```

## Arguments

- `<package ...>`: The package to update

## Options

- `-h, --help`: Prints help information
- `-q, --quiet`: Show only errors
- `-v, --verbose`: Show verbose output
- `--dry-run`: Simulate the update without making any changes. Files will still be downloaded and cached
- `--ignore-scripts`: Do not run any scripts during updating
- `--no-dependencies`: Bypass dependency resolution and only install the specified packages
