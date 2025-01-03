# lip view

## Usage

```shell
lip view [<package-spec>] [<field>[.<subfield>[<...>]]...]
```

## Description

Show information about a package. If not installed and cached, lip will download the package. If no package spec is provided, and a `tooth.json` file is found, lip will show information about the package specified in the `tooth.json` file.
