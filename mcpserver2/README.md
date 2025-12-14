# MCP Server 2 - OpenAI 整合範例

本專案示範如何建立一個 Model Context Protocol (MCP) 伺服器，並與 OpenAI 平台整合，透過 ngrok 提供公開存取。

## 功能特色

- 使用 FastAPI 建立 MCP 伺服器
- 透過 ngrok 將本地服務公開到網際網路
- 與 OpenAI 平台整合，提供自訂工具
- 範例：查詢便利店商品庫存

## 環境需求

- Python 3.10+
- uv (Python 套件管理工具)
- ngrok (用於建立公開通道)
- OpenAI 平台帳號

## 安裝步驟

### 1. 建立專案目錄

```bash
mkdir mcpserver2
cd mcpserver2
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

### 3. 安裝相依套件

```bash
uv add fastapi uvicorn mcp
```
![alt text](./docs/init-env.png)

### 4. 撰寫程式碼

在 `main.py` 中實作 MCP 伺服器邏輯，定義 API 端點和工具函式。詳細程式碼請參考專案中的 `main.py` 檔案。

### 5. 執行應用程式
```bash
uv run python main.py
```
![alt text](./docs/run-app.png)

## 設定 ngrok 公開通道

### 6. 安裝 ngrok

使用 Homebrew 安裝 ngrok：

```bash
brew install ngrok
```
![alt text](./docs/install-ngrok.png)

### 7. 設定 ngrok 認證

前往 [ngrok 官方網站](https://ngrok.com/) 註冊並取得 Authtoken。登入後可在 dashboard 中找到您的 Authtoken。

![alt text](./docs/ngrok-authtoken.png)

使用以下指令設定 Authtoken（只需執行一次）：

```bash
ngrok config add-authtoken <YOUR_AUTHTOKEN>
```
![alt text](./docs/add-authtoken.png)

設定檔路徑：

![alt text](./docs/ngrok-path.png)

設定檔內容：

![alt text](./docs/ngrok-yml.png)

### 8. 啟動 ngrok HTTP 通道

在終端機中執行以下指令，將本地 8000 埠公開到網際網路：

```bash
ngrok http 8000
```
![alt text](./docs/ngrok-http-channel.png)

此指令會建立一個公開的臨時網域，並將所有 HTTP 請求轉發到本地的 8000 埠。

### 9. 驗證 ngrok 連線

設定完成後，可以透過 ngrok 提供的臨時網域存取本地應用程式：

![alt text](./docs/ngrok-dev-zone.png)

ngrok 會顯示本地伺服器（Port 8000）回傳的訊息：

![alt text](./docs/ngrok-show-hello.png)

## 整合 OpenAI 平台

### 10. 登入 OpenAI 開發者平台

前往 [OpenAI Platform](https://platform.openai.com/docs/overview) 並使用您的帳號登入。

![alt text](./docs/openai-platform.png)

### 11. 建立 Prompt 並連接 MCP Server

#### 11.1 建立新的 Prompt

在左側工具列選取 **Chat**，然後點擊右側視窗的 **Create** 按鈕建立新的 Prompt。

![alt text](./docs/openai-create-prompt.png)

#### 11.2 新增 MCP Server

在 **Tools** 設定中選擇新增 MCP Server：

![alt text](./docs/opanai-add-mcp.png)

點擊 **+Server** 按鈕：

![alt text](./docs/openai-add-server.png)

#### 11.3 設定 Server URL

輸入 ngrok 提供的公開網址，並在路徑後加上 `/mcp`。

範例：`https://savourless-mullishly-vanda.ngrok-free.dev/mcp`

設定完成後點擊 **Connect**。

![alt text](./docs/openai-server-url.png)

#### 11.4 驗證連接

若設定正確，OpenAI 將會顯示 MCP 伺服器提供的工具列表：

![alt text](./docs/openai-mcp-tools.png)

完成設定的 Prompt 如下圖所示：

![alt text](./docs/openai-prompt-setting.png)

### 12. 測試 MCP Server

#### 12.1 發送查詢

在對話視窗中輸入查詢，例如：「KOKO便利店的茶葉蛋還有幾個」

![alt text](./docs/openai-prompt-ask.png)

#### 12.2 授權工具使用

OpenAI 會識別相關的 MCP Tool，並請求使用權限。選擇 **Approve** 授權。

![alt text](./docs/openai-mcp-approve.png)

#### 12.3 取得結果

OpenAI 會呼叫 MCP Tool 並根據回傳結果產生回應：

![alt text](./docs/openai-mcp-resp.png)

## 注意事項

- ngrok 提供的免費網址是臨時的，每次重啟 ngrok 都會變更
- 確保 FastAPI 應用程式在 8000 埠正常運行後，再啟動 ngrok
- OpenAI 平台需要可公開存取的 HTTPS URL，因此必須使用 ngrok 或類似服務
- 請妥善保管您的 ngrok Authtoken 和 OpenAI API 金鑰

## 疑難排解

### MCP Server 連接失敗

1. 確認 FastAPI 應用程式正在運行
2. 檢查 ngrok 通道是否正常啟動
3. 驗證 Server URL 是否正確（包含 `/mcp` 路徑）

### OpenAI 無法找到工具

1. 確認 MCP 伺服器的工具定義格式正確
2. 檢查 FastAPI 的 `/mcp` 端點是否正常回應
3. 查看 OpenAI 平台的錯誤訊息

## 參考資源

- [Model Context Protocol 規範](https://modelcontextprotocol.io/)
- [FastAPI 官方文件](https://fastapi.tiangolo.com/)
- [ngrok 文件](https://ngrok.com/docs)
- [OpenAI Platform 文件](https://platform.openai.com/docs)

