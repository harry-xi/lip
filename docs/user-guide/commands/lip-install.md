# lip install

Install packages and their dependencies from various sources

## Usage

```
lip install [package ...] [OPTIONS]
```

## Arguments

- `[package ...]`: The packages to install. If no packages are specified, lip will install the package in the current directory

## Options

- `-h, --help`: Prints help information
- `-q, --quiet`: Show only errors
- `-v, --verbose`: Show verbose output
- `--dry-run`: Do not actually install any packages. Be aware that files will still be downloaded and cached
- `--ignore-scripts`: Do not run any scripts during installation
- `--no-dependencies`: Bypass dependency resolution and only install the specified packages
- `--overwrite-files`: Overwrite existing files in the folder
