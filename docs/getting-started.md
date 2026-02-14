# Getting Started

## Installation

lip is a standalone executable. Download the latest version from the [releases page](https://github.com/futrime/lip/releases/latest).

### Windows

```shell
winget install futrime.lip
```

### Linux / macOS

```shell
curl -fsSL https://raw.githubusercontent.com/futrime/lip/HEAD/scripts/install.sh | sh
```

## Quick Start

Initialize a new project:

```shell
lip init
```

This creates a `tooth.json` manifest file in the current directory.

Install a package:

```shell
lip install github.com/LiteLDev/LeviLamina@1.0.0
```

List installed packages:

```shell
lip list
```

View package details from a registry:

```shell
lip view github.com/LiteLDev/LeviLamina@1.0.0
```
