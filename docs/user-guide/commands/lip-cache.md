# lip cache

## Usage

```shell
lip cache add <package-spec>
lip cache clean
lip cache list
```

## Description

Inspect and manage lip’s cache.

lip stores cache data in `%LocalAppData%\lip` for Windows and `~/.local/share/lip` for POSIX-like systems by default.

## Sub-commands

### add

```shell
lip cache add <package-spec>
```

Add a package to the cache. `<package-spec>` is a [package specifier](./lip-install.md#package-specifier), e.g. `github.com/futrime/example-package#subpack@1.0.0`.

If a Go module proxy is set in configuration, lip will download the package via Goproxy. Otherwise, lip will download the package directly from the Git repository.

### clean

```shell
lip cache clean
```

Remove all items from the cache.

### list

```shell
lip cache list
```

List items in the cache.
