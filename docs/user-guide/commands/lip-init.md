# lip init

Create a tooth.json file

## Usage

```shell
lip init
lip init <package-specifier>
```

## Description

This command can be used to set up a new or existing lip package.

lip will ask a bunch of questions, and then write a tooth.json for you. You can also use `-y` / `--yes` to skip the questionnaire altogether, and the values will be set to empty strings. You can also use the `--init-*` options to set the values directly and skip the corresponding questions.

When a [package specifier](#package-specifier) is provided, lip uses it as a template for the new tooth.json file. It copies:

- The info.tags field
- The variants configuration

If the package specifier includes a variant label, lip will:

1. Extract only the specified variant
2. Set it as the default variant in the new tooth.json

Note: This only copies configuration data, not the actual package files.

## Examples

Basic initialization:

```shell
# Create a new project using the questionnaire
lip init

# Create a new project using all default values
lip init -y
```

Initialization with specific values:

```shell
# Create a new project with initial values
lip init --init-name "my-mod" \
    --init-description "A cool LeviLamina mod" \
    --init-author "developer" \
    --init-tooth "github.com/developer/my-mod" \
    --init-version "0.1.0"
```

Using templates:

```shell
# Create from the LeviLamina mod template
lip init github.com/LiteLDev/levilamina-mod-template@0.1.0

# Create from a template and specify a variant
lip init github.com/futrime/template#variant1@1.0.0
```

## Package Specifier

A package specifier is a string that identifies a package's [tooth path](../files/tooth-json.md#tooth-required), an optional variant, and a version.

The format is `<tooth-path>[#<variant>]@<version>`.

- `<tooth-path>` is the tooth path of the package.
- `<variant>` is the label of the package variant to use. If omitted, lip will use the default variant.
- `<version>` is the version of the package to use.

Examples:

- `github.com/futrime/example-package@1.0.0`
- `github.com/futrime/example-package#variant_1@1.0.0`

## Options

- `-f, --force`

  Overwrite the existing tooth.json file.

- `--init-author <author>`

  The author to use.

- `--init-avatar-url <avatar-url>`

  The avatar URL to use.

- `--init-description <description>`

  The description to use.

- `--init-name <name>`

  The name to use.

- `--init-tooth <tooth-path>`

  The package's tooth path to use.

- `--init-version <version>`

  The version to use.

- `-w, --workspace <path>`

  Specify where to create the tooth.json file.

- `-y, --yes`

  Skip confirmation prompts.
