# lip config

## Usage

```shell
lip config set <key>=<value> [<key>=<value> ...]
lip config get [<key> [<key> ...]]
lip config delete <key> [<key> ...]
lip config list
lip config edit
```

## Description

Manage the lip configuration files.

lip stores configuration files in `~/.liprc`.

## Sub-commands

### set

```shell
lip config set <key>=<value> [<key>=<value> ...]
```

Set a configuration value. `<key>` is the configuration key, e.g. `cache.dir`. `<value>` is the configuration value, e.g. `~/.cache/lip`.

### get

```shell
lip config get [<key> [<key> ...]]
```

Get a configuration value. `<key>` is the configuration key, e.g. `cache.dir`. If no `<key>` is specified, list all configuration values.

### delete

```shell
lip config delete <key> [<key> ...]
```

Delete a configuration value. `<key>` is the configuration key, e.g. `cache.dir`.

### list

```shell
lip config list
```

List all configuration values.

### edit

```shell
lip config edit
```

Edit the configuration file. This will open the configuration file in the default editor, which is determined by the `EDITOR` or `VISUAL` environment variable, or `%SYSTEMROOT%\notepad.exe` on Windows, or `vi` POSIX-like systems.
