# Installation

So far, lip installer has not been implemented. You can install lip manually by following the instructions below.

## lip CLI

To install lip CLI, download the latest release archive for your platform from [GitHub](https://github.com/futrime/lip/releases/latest), extract it, and add the extracted directory to your PATH.

To ease the process, you can use the following commands:

- For Windows x64:
  
    ```cmd
    mkdir -p %LocalAppData%\lip
    curl -L https://github.com/futrime/lip/releases/latest/download/lip-win-x64.zip -o %LocalAppData%\lip\lip.zip
    tar -xf %LocalAppData%\lip\lip.zip -C %LocalAppData%\lip
    del %LocalAppData%\lip\lip.zip
    setx PATH "%PATH%;%LocalAppData%\lip"
    ```

- For Linux x64:
  
    ```bash
    mkdir -p ~/.local/share/lip
    curl -L https://github.com/futrime/lip/releases/latest/download/lip-linux-x64.tar.gz -o ~/.local/share/lip/lip.tar.gz
    tar -xf ~/.local/share/lip/lip.tar.gz -C ~/.local/share/lip
    rm ~/.local/share/lip/lip.tar.gz
    export PATH="$PATH:~/.local/share/lip"
    ```

    Then, add the following line to your shell configuration file (e.g., `~/.bashrc`, `~/.zshrc`, or `~/.profile`):

    ```bash
    export PATH="$PATH:~/.local/share/lip"
    ```

- For macOS arm64:
  
    ```bash
    mkdir -p ~/Library/Application\ Support/lip
    curl -L https://github.com/futrime/lip/releases/latest/download/lip-osx-arm64.zip -o ~/Library/Application\ Support/lip/lip.zip
    tar -xf ~/Library/Application\ Support/lip/lip.zip -C ~/Library/Application\ Support/lip
    rm ~/Library/Application\ Support/lip/lip.zip
    export PATH="$PATH:~/Library/Application\ Support/lip"
    ```

    Then, add the following line to your shell configuration file (e.g., `~/.bashrc`, `~/.zshrc`, or `~/.profile`):

    ```bash
    export PATH="$PATH:~/Library/Application\ Support/lip"
    ```

## lip GUI

lip GUI is under development and not yet available for installation.
