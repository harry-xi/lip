# lip uninstall

Uninstall packages.

## Synopsis

```shell
lip uninstall <PACKAGES> [options]
```

## Arguments

| Argument | Description |
| --- | --- |
| `<PACKAGES>` | The packages to uninstall, specified as package IDs (`<path>` or `<path>#<variant>`). |

## Options

| Option | Description |
| --- | --- |
| `--dry-run` | Run without making any changes. |
| `--ignore-scripts` | Skip running uninstall scripts. |
| `--no-dependencies` | Skip removing dependencies. |
