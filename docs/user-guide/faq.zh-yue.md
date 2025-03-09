# 常見問答

## 點樣清除緩存？

```bash
$ lip cache clean
```

## 點樣更新或卸載 lip？

到而家為止，lip 安裝程式未實現。你可以手動更新或卸載 lip，方法係用最新版本替換或移除你 PATH 中嘅二進制文件。如果你要卸載 lip，亦可以移除 PATH 嘅設置。

## 點樣建立一個套件

首先，喺依個目錄入面執行以下命令，以初始化成為一個套件：

```bash
$ lip init
```

之後，編輯 `tooth.json` 文件去定義個套件。詳細資料請參考 [the tooth.json 參考資料](./files/tooth-json.md)。

測試安裝個套件時（注意，因為套件會安裝喺當前目錄，可能會影響現有文件），執行：

```bash
$ lip install
```

要打包個套件，執行：

```bash
$ lip pack my-package.zip
```
