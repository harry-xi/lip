# 開始使用

要開始使用 lip，你需要喺你嘅系統上[安裝 lip](./installation.md)。

## 檢查 lip 是否正常運作

首先，打開終端機，執行以下指令確認 lip 已正確安裝：

```bash
$ lip --version
0.26.0
```

如果輸出正常，就代表一切就緒—lip 運作得非常順利。

如果顯示唔啱，請參考[安裝](./installation.md)頁面，跟住步驟指引喺 Windows、macOS 或 Linux 上安裝 lip。

## 常用操作

### 安裝套件

```bash
$ lip install example.com/pkg@1.0.0
```

默認情況下，lip 會透過 [goproxy.io](https://goproxy.io) 獲取套件。如果你想直接用 Git 獲取，只需要清空代理列表：

```bash
lip config set go_module_proxies=
```

或者，你可以設定自定義代理：

```bash
lip config set go_module_proxies=https://proxy.example.com
```

### 從本地目錄安裝套件

```bash
$ lip install /path/to/pkg/
```

lip 會自動檢測目錄中的 tooth.json 檔案並安裝該套件。

### 從本地壓縮檔安裝套件

```bash
$ lip install /path/to/pkg.tar.gz
```

### 使用 tooth.json 同時安裝多個套件

喺當前目錄建立一個 tooth.json 檔案，然後執行 `lip install` 一次過安裝多個套件 —— 超級方便嘅流程。

### 更新套件

```bash
$ lip update example.com/pkg@1.0.0
```

更新套件時記得一定要指定版本。由於套件來源唔完全同步，lip 唔支援自動升級到最新版本。

### 卸載套件

```bash
$ lip uninstall example.com/pkg
```

卸載時請避免指定版本。
