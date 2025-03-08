# 安装 lip

目前，lip 安装程序尚未实现。您可以按照以下说明手动安装 lip。

## lip CLI

要安装 lip CLI，请从 [GitHub](https://github.com/futrime/lip/releases/latest) 下载您平台的最新版本存档，提取它，并将提取的目录添加到您的 PATH 中。

为了简化过程，您可以使用以下命令：


- 在Windows x64:
  
    ```cmd
    mkdir -p %LocalAppData%\lip
    curl -L https://github.com/futrime/lip/releases/latest/download/lip-win-x64.zip -o %LocalAppData%\lip\lip.zip
    tar -xf %LocalAppData%\lip\lip.zip -C %LocalAppData%\lip
    del %LocalAppData%\lip\lip.zip
    setx PATH "%PATH%;%LocalAppData%\lip"
    ```

- 在Linux x64:
  
    ```bash
    mkdir -p ~/.local/share/lip
    curl -L https://github.com/futrime/lip/releases/latest/download/lip-linux-x64.tar.gz -o ~/.local/share/lip/lip.tar.gz
    tar -xf ~/.local/share/lip/lip.tar.gz -C ~/.local/share/lip
    rm ~/.local/share/lip/lip.tar.gz
    export PATH="$PATH:~/.local/share/lip"
    ```

    然后，将以下行添加到您的 shell 配置文件中（例如，`~/.bashrc`、`~/.zshrc` 或 `~/.profile`）：

    ```bash
    export PATH="$PATH:~/.local/share/lip"
    ```

- 在macOS arm64:
  
    ```bash
    mkdir -p ~/Library/Application\ Support/lip
    curl -L https://github.com/futrime/lip/releases/latest/download/lip-osx-arm64.zip -o ~/Library/Application\ Support/lip/lip.zip
    tar -xf ~/Library/Application\ Support/lip/lip.zip -C ~/Library/Application\ Support/lip
    rm ~/Library/Application\ Support/lip/lip.zip
    export PATH="$PATH:~/Library/Application\ Support/lip"
    ```

    然后，将以下行添加到您的 shell 配置文件中（例如，`~/.bashrc`、`~/.zshrc` 或 `~/.profile`）：

    ```bash
    export PATH="$PATH:~/Library/Application\ Support/lip"
    ```

## lip GUI

lip GUI 正在开发中，尚未可供安装。
