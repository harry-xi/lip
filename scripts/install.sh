#!/bin/sh
set -eu

REPO="futrime/lip"
INSTALL_DIR="${LIP_INSTALL_DIR:-$HOME/.local/bin}"

OS=$(uname -s)
ARCH=$(uname -m)

case "$OS" in
  Linux)  PLATFORM="linux" ;;
  Darwin) PLATFORM="osx" ;;
  *)      echo "Unsupported OS: $OS" >&2; exit 1 ;;
esac

case "$ARCH" in
  aarch64|arm64) SUFFIX="arm64" ;;
  x86_64|amd64)  SUFFIX="x64" ;;
  *)             echo "Unsupported architecture: $ARCH" >&2; exit 1 ;;
esac

RUNTIME="${PLATFORM}-${SUFFIX}"

VERSION=$(curl -fsSL "https://api.github.com/repos/${REPO}/releases/latest" \
  | grep '"tag_name"' | sed -E 's/.*"v?([^"]+)".*/\1/')

FILENAME="lip-${VERSION}-${RUNTIME}.tar.gz"
URL="https://github.com/${REPO}/releases/download/v${VERSION}/${FILENAME}"

TEMP_DIR=$(mktemp -d)
trap 'rm -rf "$TEMP_DIR"' EXIT

echo "Downloading lip ${VERSION} for ${RUNTIME}..."
curl -fSL "$URL" -o "${TEMP_DIR}/${FILENAME}"
tar -xzf "${TEMP_DIR}/${FILENAME}" -C "$TEMP_DIR"

mkdir -p "$INSTALL_DIR"
install -m 755 "${TEMP_DIR}/lip" "$INSTALL_DIR/lip"

echo "Installed lip to ${INSTALL_DIR}/lip"

case ":${PATH}:" in
  *":${INSTALL_DIR}:"*) exit 0 ;;
esac

echo "Adding ${INSTALL_DIR} to PATH..."

update_profile() {
  [ -f "$1" ] || return 1
  grep -q "$INSTALL_DIR" "$1" && return 0
  echo >> "$1"
  echo "export PATH=\"$INSTALL_DIR:\$PATH\"" >> "$1"
  echo "Added \"$INSTALL_DIR\" to PATH in \"$1\""
}

case "$(basename "$SHELL")" in
  fish)
    CONF="$HOME/.config/fish/config.fish"
    mkdir -p "$(dirname "$CONF")"
    if ! grep -q "$INSTALL_DIR" "$CONF"; then
      echo "fish_add_path $INSTALL_DIR" >> "$CONF"
      echo "Added \"$INSTALL_DIR\" to PATH in \"$CONF\""
    fi
    ;;
  zsh)
    update_profile "$HOME/.zshrc" || update_profile "$HOME/.zshenv"
    ;;
  bash)
    update_profile "$HOME/.bashrc" || update_profile "$HOME/.bash_profile"
    ;;
  *)
    update_profile "$HOME/.profile"
    ;;
esac

echo "Open a new terminal to use lip."
