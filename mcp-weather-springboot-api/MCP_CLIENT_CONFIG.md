# MCP 客戶端配置指南

本 MCP server 基於標準的 MCP (Model Context Protocol) 協議，支援所有實作該協議的客戶端。

## 支援的客戶端

✅ **Claude Desktop** (Anthropic 官方桌面應用)
✅ **Cline** (VS Code 擴充功能，原 Claude Dev)
✅ **Continue.dev** (VS Code/JetBrains 擴充功能)
✅ **其他 MCP 相容客戶端**

---

## 1. Claude Desktop 配置

### 配置檔案位置

**macOS:**
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

**Windows:**
```
%APPDATA%\Claude\claude_desktop_config.json
```

### 配置內容

```json
{
  "mcpServers": {
    "weather-api": {
      "command": "java",
      "args": [
        "-jar",
        "/Users/timmacpro/PyProjects/mcpserver/mcp-weather-springboot-api/target/mcp-weather-springboot-api-1.0.0.jar",
        "--spring.profiles.active=mcp"
      ]
    }
  }
}
```

### 重啟應用

完全關閉並重新啟動 Claude Desktop。

### 測試

在 Claude Desktop 中輸入：
```
請幫我查詢舊金山的天氣預報（緯度：37.7749，經度：-122.4194）
```

---

## 2. Cline (VS Code) 配置

### 安裝 Cline

在 VS Code 中搜索並安裝 "Cline" 擴充功能。

### 配置檔案位置

**macOS/Linux:**
```
~/.cline/mcp_settings.json
```

**Windows:**
```
%APPDATA%\Cline\mcp_settings.json
```

### 配置內容

```json
{
  "mcpServers": {
    "weather-api": {
      "command": "java",
      "args": [
        "-jar",
        "/Users/timmacpro/PyProjects/mcpserver/mcp-weather-springboot-api/target/mcp-weather-springboot-api-1.0.0.jar",
        "--spring.profiles.active=mcp"
      ]
    }
  }
}
```

### 重新載入

在 VS Code 中：
1. 按 `Cmd/Ctrl + Shift + P`
2. 輸入 "Reload Window"
3. 或重新啟動 VS Code

### 測試

開啟 Cline 側邊欄，輸入：
```
請幫我查詢舊金山的天氣預報
```

---

## 3. Continue.dev 配置

### 安裝 Continue.dev

在 VS Code 或 JetBrains IDE 中搜索並安裝 "Continue" 擴充功能。

### 配置檔案位置

**專案級別（推薦）:**
```
<your-project>/.continue/config.json
```

**全域級別:**
```
~/.continue/config.json
```

### 配置內容

```json
{
  "models": [
    {
      "title": "Claude 3.5 Sonnet",
      "provider": "anthropic",
      "model": "claude-3-5-sonnet-20241022",
      "apiKey": "your-api-key"
    }
  ],
  "mcpServers": {
    "weather-api": {
      "command": "java",
      "args": [
        "-jar",
        "/Users/timmacpro/PyProjects/mcpserver/mcp-weather-springboot-api/target/mcp-weather-springboot-api-1.0.0.jar",
        "--spring.profiles.active=mcp"
      ]
    }
  }
}
```

### 重新載入

重新啟動 IDE 或重新載入 Continue 擴充功能。

### 測試

開啟 Continue 側邊欄，輸入：
```
@weather-api 查詢舊金山天氣
```

---

## 配置檔案對照表

| 客戶端 | 配置檔案路徑 (macOS) | 配置檔案路徑 (Windows) |
|--------|---------------------|---------------------|
| **Claude Desktop** | `~/Library/Application Support/Claude/claude_desktop_config.json` | `%APPDATA%\Claude\claude_desktop_config.json` |
| **Cline** | `~/.cline/mcp_settings.json` | `%APPDATA%\Cline\mcp_settings.json` |
| **Continue.dev** | `~/.continue/config.json` | `%USERPROFILE%\.continue\config.json` |

---

## 重要注意事項

### 1. 路徑設定

⚠️ **必須使用絕對路徑**

❌ 錯誤範例：
```json
"args": ["./target/app.jar"]
```

✅ 正確範例：
```json
"args": ["/Users/username/project/target/app.jar"]
```

### 2. MCP 模式參數

⚠️ **必須包含 `--spring.profiles.active=mcp` 參數**

這個參數告訴應用程式啟動 MCP 模式而非 HTTP API 模式。

### 3. Java 環境

確保系統已安裝 Java 17 或以上版本：

```bash
java -version
```

如果 `java` 指令不在 PATH 中，可以使用完整路徑：

```json
{
  "command": "/usr/bin/java",
  "args": [...]
}
```

或 macOS 上使用：
```json
{
  "command": "/usr/libexec/java_home -v 17",
  "args": [...]
}
```

---

## 驗證配置

### 查看日誌

應用程式運行時會產生日誌檔案：

```bash
tail -f ~/mcp-weather-api.log
```

如果看到類似以下內容，表示啟動成功：
```
2024-01-12 23:00:00 [main] INFO  c.e.mcpweather.McpWeatherApiApplication - Starting McpWeatherApiApplication
2024-01-12 23:00:01 [main] INFO  c.e.mcpweather.mcp.McpServerRunner - MCP Weather Server started (STDIO mode)
```

### 檢查客戶端日誌

**Claude Desktop:**
```bash
tail -f ~/Library/Logs/Claude/mcp-server-weather-api.log
```

**Cline:**
查看 VS Code 的 Output 面板，選擇 "Cline" 頻道。

**Continue.dev:**
查看 VS Code 的 Output 面板，選擇 "Continue" 頻道。

---

## 常見問題

### Q1: 客戶端顯示 "Failed to connect to MCP server"

**原因：**
- 路徑錯誤
- Java 未安裝或不在 PATH 中
- JAR 檔案不存在
- 缺少 MCP 模式參數

**解決方案：**
1. 確認 JAR 檔案路徑正確
2. 確認 Java 版本：`java -version`
3. 確認有 `--spring.profiles.active=mcp` 參數
4. 查看日誌檔案

### Q2: MCP server 啟動但工具無法使用

**原因：**
- MCP 模式未正確啟用
- 應用程式啟動為 HTTP 模式

**解決方案：**
1. 確認配置中有 `--spring.profiles.active=mcp` 參數
2. 查看日誌確認是否有 "MCP Weather Server started (STDIO mode)" 訊息
3. 確認日誌輸出到檔案而非 console

### Q3: 可以同時在多個客戶端使用嗎？

**答案：可以！**

每個客戶端會啟動自己的 MCP server 實例，互不干擾。您可以：
- 在 Claude Desktop 中使用
- 同時在 Cline 中使用
- 同時在 Continue.dev 中使用
- 同時運行 HTTP API 模式供其他應用呼叫

---

## 進階配置

### 自訂日誌位置

在配置中加入 JVM 參數：

```json
{
  "command": "java",
  "args": [
    "-Dlogging.file.name=/custom/path/weather.log",
    "-jar",
    "/path/to/mcp-weather-springboot-api-1.0.0.jar",
    "--spring.profiles.active=mcp"
  ]
}
```

### 調整記憶體

如果需要更多記憶體：

```json
{
  "command": "java",
  "args": [
    "-Xmx512m",
    "-jar",
    "/path/to/mcp-weather-springboot-api-1.0.0.jar",
    "--spring.profiles.active=mcp"
  ]
}
```

### 開啟除錯模式

```json
{
  "command": "java",
  "args": [
    "-jar",
    "/path/to/mcp-weather-springboot-api-1.0.0.jar",
    "--spring.profiles.active=mcp",
    "--logging.level.com.example.mcpweather=DEBUG"
  ]
}
```

---

## 參考連結

- [MCP 官方規範](https://modelcontextprotocol.io/)
- [Claude Desktop 下載](https://claude.ai/download)
- [Cline VS Code 擴充](https://marketplace.visualstudio.com/items?itemName=saoudrizwan.claude-dev)
- [Continue.dev 官網](https://continue.dev/)
