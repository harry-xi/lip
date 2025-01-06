# lip init

## Usage

```shell
lip init [<package-spec>]
```

## Description

Set up a new package in the current directory.

Initialize and writes a new `tooth.json` file in the current directory. The `tooth.json` file must not already exist.

If a [package specifier](./lip-install.md#package-specifier) (`<package-spec>`) is specified, the package will be initialized with the specified package.

## Options

- `-f, --force`

  Overwrite the existing `tooth.json` file.

- `--init-author <author>`

  The author to use.

  If not specified, lip will ask for an author.

- `--init-avatar-url <avatar-url>`

  The avatar URL to use.

  If not specified, lip will ask for an avatar URL.

- `--init-description <description>`

  The description to use.

  If not specified, lip will ask for a description.

- `--init-name <name>`

  The name to use.

  If not specified, lip will ask for a name.

- `--init-tags <tags>`

  The tags to use, separated by commas.

  If not specified, lip will ask for tags.

- `--init-tooth <tooth>`

  The tooth identifier to use.

  If not specified, lip will ask for a tooth identifier.

- `--init-version <version>`

  The version to use.

  If not specified, lip will ask for a version.

- `-y, --yes`

  Skip confirmation.
