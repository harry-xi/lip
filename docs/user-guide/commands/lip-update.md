# lip update

## Usage

```shell
lip update <package ...>
```

## Description

Update packages and their dependencies from various sources. Equivalent to `lip install --update <package...>`.

## Arguments

- `package`

  The packages to update.

## Options

- `--dry-run`

  Do not actually update any packages. Be aware that files will still be downloaded and cached.



- `--ignore-scripts`

  Do not run any scripts during updating.

- `--no-dependencies`

  Bypass dependency resolution and only install the specified packages.
