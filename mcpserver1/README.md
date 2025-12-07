# 開發第一個 MCP Server 教學指南

本專案示範如何使用 FastMCP 框架建立 MCP (Model Context Protocol) 伺服器，讓 AI 模型能夠透過標準化協議與外部工具和資源進行互動。這是一個簡單的入門範例，用於學習如何建構和測試 MCP 伺服器。

## 目錄

- [系統需求](#系統需求)
- [初始化專案](#初始化專案)
- [開發第一個 MCP Server](#開發第一個-mcp-server)
- [測試 MCP Server](#測試-mcp-server)
- [整合 Claude Desktop](#整合-claude-desktop)
- [驗證與使用](#驗證與使用)
- [常見問題](#常見問題)

## 系統需求

在開始之前，請確保您的系統已安裝以下工具：

- **Python 3.10 或更高版本**
- **uv** - Python 套件管理工具
- **Node.js** - 用於 MCP Inspector 工具
- **Claude Desktop** - 用於測試 MCP Server 整合

## 初始化專案

### 1. 建立專案目錄

```bash
mkdir mcpserver1
cd mcpserver1
```

### 2. 初始化 Python 專案

使用 uv 指令初始化專案：

```bash
uv init
```

**執行後會自動產生以下檔案：**
- `.gitignore` - Git 版本控制忽略檔案
- `.python-version` - Python 版本指定檔案
- `main.py` - 主程式檔案
- `pyproject.toml` - 專案設定檔
- `README.md` - 專案說明文件

### 3. 測試基本環境

執行預設的 main.py 程式以驗證環境：

```bash
uv run main.py
```

**說明：** uv 會自動建立 Python 虛擬環境，完成後會出現 `.venv` 目錄，這是虛擬環境的存放位置，預設會排除在 Git 版本控制之外。

### 4. 安裝 MCP CLI 工具套件

安裝 MCP (Model Context Protocol) 的 CLI 工具套件：

```bash
uv add "mcp[cli]"
```

### 5. 驗證安裝

確認 MCP 工具已成功安裝並檢查版本：

```bash
uv run mcp version
```

## 開發第一個 MCP Server

### 建立 MCP Server 程式檔案

請參考專案中的 `mcpserverlab.py` 範例檔案，該檔案展示了如何建立一個基本的 MCP Server，包含：

- **工具定義** - 定義 AI 可以調用的函數
- **參數處理** - 處理來自 AI 的輸入參數
- **回應格式** - 以標準格式回傳結果給 AI

## 測試 MCP Server

### 使用 MCP Inspector 進行測試

MCP Inspector 是一個互動式測試工具，可讓您在整合到 Claude Desktop 之前測試 MCP Server 的功能。

#### 啟動測試工具

```bash
uv run mcp dev mcpserverlab.py
```

#### 啟動說明

- `mcp dev` 是 MCP CLI 的子命令，用於啟動 MCP 伺服器並啟用偵錯工具
- 首次執行時，系統可能會提示需要安裝 Node.js 的 `@modelcontextprotocol/inspector` 套件
- 請按下 `y` 確認安裝，讓程式繼續執行
- 安裝成功後，會自動在瀏覽器中開啟 MCP Inspector 工具

#### MCP Inspector 介面

![MCP Inspector](mcp-inspector.png)

透過 MCP Inspector，您可以：
- 查看已定義的工具列表
- 測試工具的輸入參數
- 檢視工具的執行結果
- 除錯 MCP Server 的運作流程

## 整合 Claude Desktop

### 安裝 Claude Desktop

1. 前往 [Claude 官方網站](https://claude.ai) 下載 Claude Desktop 應用程式
2. 依照安裝程式指示完成安裝

### 初次啟動 Claude

進入專案目錄並啟動 Claude：

```bash
cd mcpserver1
claude
```

![Claude 啟動畫面](./docs/start-claude.png)

### 測試基本對話功能

在整合 MCP Server 之前，可先測試 Claude 的基本功能。例如詢問：

**問題範例：** "What does the project do?"

![Claude 對話範例](./docs/ask-claude.png)

### 註冊 MCP Server 至 Claude Desktop

#### 執行安裝指令

```bash
uv run mcp install mcpserverlab.py
```

![MCP Server 安裝過程](./docs/image.png)

#### 設定檔說明

這道指令會自動完成以下動作：

1. **定位設定檔** - 依據作業系統找出 `claude_desktop_config.json` 設定檔
2. **更新設定** - 在設定檔中新增或更新 MCP Server 的連線資訊
3. **設定通訊協議** - Claude Desktop 會透過 JSON-RPC 2.0 或 STDIO 與 MCP Server 進行通訊

#### Mac OS 設定檔位置

![Claude Desktop 設定檔路徑](./docs/claude-config-path.png)

在 Mac OS 環境下，設定檔通常位於：
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

#### 重要說明

從設定檔內容可以觀察到：

- **通訊方式** - Claude Desktop 並非透過 HTTP 協議與 MCP Server 通訊
- **執行機制** - 直接使用 `uv` 執行 MCP Server 的 Python 程式
- **關鍵依賴** - 必須確保以下條件：
  - `uv` 已正確安裝在系統中
  - MCP Server 檔案路徑正確無誤
  - Python 環境設定正常

若任一條件不滿足，將無法順利建立連線。

## 驗證與使用

### 重新啟動 Claude Desktop

完成 MCP Server 註冊後，請重新啟動 Claude Desktop 應用程式以載入新的設定。

### 檢查工具可用性

![Claude Desktop 工具列表](./docs/claude-desktop.png)

若 MCP Server 已成功安裝與註冊，重新啟動後應該可以在 Claude Desktop 介面中看到可用的工具。

### 測試工具調用

#### 發送測試請求

嘗試輸入一個會觸發 MCP Server 工具的請求。例如，若您的工具與姓名相關，可以輸入一個姓名。

![測試工具調用](./docs/claude-ask-mymcpserver.png)

#### 首次使用確認

- Claude 會自動辨識使用者意圖
- 系統會判斷需要調用哪個工具
- **重要：** 首次使用工具時，系統會要求使用者確認是否允許執行
- 請檢視工具的功能說明後，點選確認執行

#### 查看執行結果

![工具執行結果](./docs/claude-use-mymcpserver.png)

工具調用成功後，Claude Desktop 會顯示：

1. **工具回應內容** - 來自 MCP Server 的執行結果
2. **請求資訊** - 發送給工具的 Request 詳細內容
3. **回應資訊** - 工具返回的 Response 詳細資訊
4. **執行狀態** - 工具執行是否成功

這些資訊有助於理解整個調用流程，對於除錯和優化非常有用。

## 常見問題

### MCP Server 無法連線

**可能原因：**
- `uv` 未正確安裝或不在系統 PATH 中
- MCP Server 檔案路徑錯誤
- Python 環境設定問題

**解決方法：**
1. 確認 `uv` 安裝：`which uv` 或 `uv --version`
2. 檢查 `claude_desktop_config.json` 中的路徑是否正確
3. 手動測試 MCP Server：`uv run mcp dev mcpserverlab.py`

### 重新啟動後看不到工具

**可能原因：**
- 設定檔未正確更新
- Claude Desktop 快取問題

**解決方法：**
1. 檢查 `claude_desktop_config.json` 內容是否包含您的 MCP Server
2. 完全關閉 Claude Desktop 後重新開啟
3. 若問題持續，嘗試重新執行 `uv run mcp install mcpserverlab.py`

### MCP Inspector 無法啟動

**可能原因：**
- Node.js 未安裝
- `@modelcontextprotocol/inspector` 套件安裝失敗

**解決方法：**
1. 確認 Node.js 已安裝：`node --version`
2. 手動安裝 inspector：`npx @modelcontextprotocol/inspector`
3. 檢查網路連線是否正常

### 工具執行時發生錯誤

**可能原因：**
- MCP Server 程式碼邏輯錯誤
- 輸入參數格式不符
- Python 套件依賴問題

**解決方法：**
1. 使用 MCP Inspector 進行除錯測試
2. 檢查 MCP Server 的錯誤訊息和日誌
3. 確認所有必要的 Python 套件已安裝

## 相關資源

### 參考書籍

- **AI Agent 奇幻旅程** - MCP 通往異世界金鑰

### 線上資源

- [MCP 官方文件](https://modelcontextprotocol.io/)
- [FastMCP 框架](https://github.com/jlowin/fastmcp)
- [Claude Desktop 下載](https://claude.ai)
- [uv 套件管理工具](https://github.com/astral-sh/uv)

## 授權

請參考專案中的 LICENSE 檔案。
