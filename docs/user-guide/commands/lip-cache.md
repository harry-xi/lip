# lip cache

## Usage

```shell
lip cache add <package-spec>
lip cache clean
lip cache list
```

## Description

Inspect and manage lip’s cache.

lip stores cache data in `%LocalAppData%\lip-cache` for Windows and `~/.cache/lip` for POSIX-like systems by default. This can be configured by configuration item `cache.dir`.

## Sub-commands

### add

```shell
lip cache add <package-spec>
```

Add a package to the cache. `<package-spec>` is a [package specifier](#), e.g. `github.com/futrime/example-package#subpack@1.0.0`.

If the configuration item `download.goproxy` is set, lip will download the package via Goproxy. Otherwise, lip will download the package directly from the Git repository.

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
