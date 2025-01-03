# lip prune

## Usage

```shell
lip prune [<package-spec>...]
```

## Description

Remove unused packages. When run without arguments, removes all unused packages. Packages specified as arguments that are not installed will be ignored. Dependencies of other packages will be preserved and skipped during pruning.

## Options

- `--dry-run`

  Do not actually remove any packages.

- `--ignore-scripts`

  Do not run any scripts during pruning.
