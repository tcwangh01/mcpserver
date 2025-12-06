# 開發第一個 MCP Server 步驟

## 初始化專案

+ 建立專案目錄 mcpserver1
+ 切換到該目錄下
+ 使用 uv 指令初始化 Python 專案
    + 初始化專案之後會產生 .gitignore, .python-version, main.py, pyproject.toml, README.md
```
uv init
```
+ 執行 main.py 程式
    + uv 會自動建立 Python 虛擬環境．完成後會出現 .venv，這就是虛擬環境的目錄，預設會排除到 Git 管控外
```
uv run main.py
```

+ 安裝 MCP (Model Context Protocol) 的 CLI 工具套件
```
uv add "mcp[clii]"
```

+ 透過工具確認安裝完成的 MCP 工具版本
```
uv run mcp server
```

