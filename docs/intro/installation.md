# Installation

`lip` is a general package installer. To get started, you need to install the `lip` executable on your system.

## Pre-built Binaries

You can download the latest pre-built binaries from the [Releases page](https://github.com/futrime/lip/releases/latest).

1. Download the archive matching your operating system and architecture.
2. Extract the archive.
3. Add the extracted directory to your system's `PATH` environment variable.

## Install Scripts

### Windows (Winget)

The easiest way to install `lip` on Windows is via winget:

```shell
winget install futrime.lip
```

### GNU/Linux and macOS

You can use the installation script to download and install `lip` automatically:

```shell
curl -fsSL https://raw.githubusercontent.com/futrime/lip/HEAD/scripts/install.sh | sh
```

## Building from Source

If you prefer to build `lip` from source, ensure you have the following installed:

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)

### Steps

1. **Clone the repository:**

   ```shell
   git clone https://github.com/futrime/lip.git
   cd lip
   ```

2. **Build and Publish:**

   Run the following command to build the CLI tool:

   ```shell
   dotnet publish src/Lip.Cli/Lip.Cli.csproj -c Release -o out
   ```

3. **Install:**

   The `lip` executable will be located in the `out` directory. Add this directory to your `PATH` or move the executable to a directory already in your `PATH` (e.g., `/usr/local/bin` on Linux/macOS).
