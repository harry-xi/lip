#!/usr/bin/env sh

# Generate the platform suffix based on the architecture.
ARCH=$(arch)
if [ "$ARCH" = "arm64" ]; then
  PLATFORM_SUFFIX="arm64"
elif [ "$ARCH" = "x86_64" ]; then
  PLATFORM_SUFFIX="x64"
else
    echo "Unsupported architecture: $ARCH"
    exit 1
fi

# Build the download URL.
if [ -z "$GITHUB_MIRROR" ]; then
  GITHUB_MIRROR="https://github.com"
fi
GITHUB_MIRROR=$(echo "$GITHUB_MIRROR" | sed 's:/*$::')
DOWNLOAD_URL="$GITHUB_MIRROR/futrime/lip/releases/latest/download/lip-osx-$PLATFORM_SUFFIX.zip"

# Download the release.
TEMP_DIR=$(mktemp -d)
echo "Downloading $DOWNLOAD_URL"
curl -fLsS $DOWNLOAD_URL | tar -x -C $TEMP_DIR

# Install the binary.
INSTALL_DIR="$HOME/Library/Application\ Support/lip"
echo "Installing to $INSTALL_DIR"
if [ ! -d "$INSTALL_DIR" ]; then
  mkdir -p "$INSTALL_DIR"
fi
chmod 755 "$TEMP_DIR/lip"
mv "$TEMP_DIR/lip" "$INSTALL_DIR"
rm -rf $TEMP_DIR

# Check if the binary is in PATH.
if echo $PATH | grep -Fq "$INSTALL_DIR"; then
  exit 0
fi

# Add to PATH.
if [ "$SHELL" = "/bin/zsh" ]; then
  SHELL_RC="$HOME/.zshrc"
elif [ "$SHELL" = "/bin/bash" ]; then
  SHELL_RC="$HOME/.bashrc"
else
  echo "Unsupported shell: $SHELL"
  echo "Please add $INSTALL_DIR to your PATH manually."
  exit 0
fi

# Add if not already in PATH.
SET_PATH_CMD="export PATH=\"\$PATH:$INSTALL_DIR\""
if grep -Fq "$SET_PATH_CMD" $SHELL_RC; then
  exit 0
fi
echo "Adding $INSTALL_DIR to PATH..."
echo >> $SHELL_RC
echo "export PATH=\"\$PATH:$INSTALL_DIR\"" >> $SHELL_RC
echo >> $SHELL_RC
echo "Please restart your shell or run 'source $SHELL_RC' to use the 'lip' command."
