# 快速開始指南

## ✅ 專案已編譯完成

JAR 檔案位於：`target/mcp-weather-springboot-api-1.0.0.jar`

---

## 🌐 模式 1：HTTP API Server

### 啟動服務

```bash
./start-api.sh
```

或

```bash
java -jar target/mcp-weather-springboot-api-1.0.0.jar
```

服務將在 **http://localhost:8080** 啟動

### 測試 API

開啟新的終端機視窗，執行以下命令：

```bash
# 查詢舊金山天氣
curl "http://localhost:8080/api/weather/forecast?lat=37.7749&lon=-122.4194"

# 查詢加州警報
curl "http://localhost:8080/api/weather/alerts?state=CA"

# 健康檢查
curl "http://localhost:8080/api/weather/health"
```

### 使用瀏覽器測試

直接在瀏覽器開啟：
- http://localhost:8080/api/weather/health
- http://localhost:8080/api/weather/forecast?lat=37.7749&lon=-122.4194
- http://localhost:8080/api/weather/alerts?state=CA

---

## 🤖 模式 2：MCP Server（Claude Desktop）

### 步驟 1：配置 Claude Desktop

編輯配置檔案：
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

加入以下配置：

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

**重要：** 請確認路徑是否正確！

### 步驟 2：重啟 Claude Desktop

完全關閉並重新開啟 Claude Desktop

### 步驟 3：測試 MCP 工具

在 Claude Desktop 輸入：

```
請幫我查詢舊金山的天氣預報（緯度：37.7749，經度：-122.4194）
```

或

```
請查詢加州（CA）的天氣警報
```

### 除錯

如果無法連接，查看日誌：

```bash
# 應用程式日誌
tail -f ~/mcp-weather-api.log

# Claude Desktop 日誌
tail -f ~/Library/Logs/Claude/mcp-server-weather-api.log
```

---

## 🔄 同時運行兩種模式

可以在不同終端機同時運行：

**終端機 1 - HTTP API:**
```bash
./start-api.sh
```

**終端機 2 - MCP Server:**
```bash
./start-mcp.sh
```

**Claude Desktop:**
配置為 MCP 模式

這樣就可以同時：
- 透過 HTTP API 呼叫服務
- 在 Claude Desktop 中使用 MCP 工具

---

## 📊 API 端點說明

### 1. 查詢天氣預報

**端點:** `GET /api/weather/forecast`

**參數:**
- `lat`: 緯度（必填）
- `lon`: 經度（必填）

**範例:**
```bash
curl "http://localhost:8080/api/weather/forecast?lat=37.7749&lon=-122.4194"
```

### 2. 查詢天氣警報

**端點:** `GET /api/weather/alerts`

**參數:**
- `state`: 美國州代碼（必填，如 CA, NY, TX）

**範例:**
```bash
curl "http://localhost:8080/api/weather/alerts?state=CA"
```

### 3. 健康檢查

**端點:** `GET /api/weather/health`

**範例:**
```bash
curl "http://localhost:8080/api/weather/health"
```

---

## ⚙️ 重新編譯

如果修改了程式碼：

```bash
mvn clean package -DskipTests
```

---

## 🔧 常見問題

### HTTP API 無法啟動

**問題:** Port 8080 already in use

**解決:**
```bash
# 查找佔用 port 的進程
lsof -i :8080

# 結束該進程
kill -9 <PID>
```

### MCP 模式無法連接

1. 確認路徑正確（必須是絕對路徑）
2. 確認有 `--spring.profiles.active=mcp` 參數
3. 查看日誌：`tail -f ~/mcp-weather-api.log`
4. 重啟 Claude Desktop

### 清理殘留進程

```bash
# 清理所有 Java 進程（謹慎使用）
pkill -f "mcp-weather-springboot-api"
```

---

## 📖 更多資訊

請參考完整的 [README.md](README.md) 文件。
