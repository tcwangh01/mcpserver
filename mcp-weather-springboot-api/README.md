# MCP Weather API Server

整合型天氣查詢服務，同時提供 **HTTP REST API** 和 **MCP Server** 兩種接入方式。

## 功能特色

- 🌐 **雙模式運行**
  - HTTP API 模式：提供 REST API 端點供外部應用程式呼叫
  - MCP 模式：透過 STDIO 與 Claude Desktop 整合

- 🌤️ **天氣查詢功能**
  - 根據經緯度查詢天氣預報
  - 查詢美國各州的天氣警報

- 🔌 **彈性部署**
  - 可作為獨立 API 服務運行
  - 可作為 Claude Desktop 的 MCP 工具使用

## 環境需求

- Java 17 或以上版本
- Maven 3.6+
- Claude Desktop 應用程式（僅 MCP 模式需要）

## 專案結構

```
mcp-weather-springboot-api/
├── src/main/
│   ├── java/com/example/mcpweather/
│   │   ├── McpWeatherApiApplication.java    # 主程式
│   │   ├── controller/
│   │   │   └── WeatherController.java        # REST API Controller
│   │   ├── service/
│   │   │   └── WeatherService.java           # 天氣服務（共用邏輯）
│   │   ├── mcp/
│   │   │   └── McpServerRunner.java          # MCP Server 實作
│   │   └── dto/                              # 資料傳輸物件
│   └── resources/
│       ├── application.properties            # HTTP 模式配置
│       ├── application-mcp.properties        # MCP 模式配置
│       └── logback-spring.xml                # 日誌配置
├── pom.xml
└── README.md
```

## 編譯專案

```bash
cd mcp-weather-springboot-api
mvn clean package -DskipTests
```

編譯完成後，JAR 檔案位於 `target/mcp-weather-springboot-api-1.0.0.jar`

## 使用方式

### 模式 1：HTTP API Server

啟動 REST API 服務（預設 port 8080）：

```bash
java -jar target/mcp-weather-springboot-api-1.0.0.jar
```

#### API 端點

**1. 查詢天氣預報**
```bash
GET http://localhost:8080/api/weather/forecast?lat=37.7749&lon=-122.4194
```

回應範例：
```json
{
  "latitude": 37.7749,
  "longitude": -122.4194,
  "periods": [
    {
      "name": "Tonight",
      "temperature": 52,
      "temperatureUnit": "F",
      "windSpeed": "5 to 10 mph",
      "windDirection": "NW",
      "detailedForecast": "Partly cloudy, with a low around 52..."
    },
    ...
  ]
}
```

**2. 查詢天氣警報**
```bash
GET http://localhost:8080/api/weather/alerts?state=CA
```

回應範例：
```json
{
  "state": "CA",
  "alerts": [
    {
      "event": "High Wind Warning",
      "areaDesc": "San Francisco Bay Area",
      "severity": "Severe",
      "description": "...",
      "instruction": "..."
    },
    ...
  ]
}
```

**3. 健康檢查**
```bash
GET http://localhost:8080/api/weather/health
```

#### 測試 API

使用 curl 測試：

```bash
# 查詢舊金山天氣
curl "http://localhost:8080/api/weather/forecast?lat=37.7749&lon=-122.4194"

# 查詢加州警報
curl "http://localhost:8080/api/weather/alerts?state=CA"

# 健康檢查
curl "http://localhost:8080/api/weather/health"
```

### 模式 2：MCP Server（支援多種客戶端）

本 MCP server 支援所有實作 MCP 協議的客戶端，包括：
- **Claude Desktop** (Anthropic)
- **Cline** (VS Code Extension)
- **Continue.dev** (VS Code/JetBrains Extension)

#### 配置 Claude Desktop

編輯 Claude Desktop 配置檔案：

**macOS:**
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

**Windows:**
```
%APPDATA%\Claude\claude_desktop_config.json
```

加入 MCP server 配置：

```json
{
  "mcpServers": {
    "weather-api": {
      "command": "java",
      "args": [
        "-jar",
        "/完整路徑/到/mcp-weather-springboot-api/target/mcp-weather-springboot-api-1.0.0.jar",
        "--spring.profiles.active=mcp"
      ]
    }
  }
}
```

#### 配置 Cline (VS Code)

Cline 的 MCP 配置檔案位置：

**macOS/Linux:**
```
~/.cline/mcp_settings.json
```

**Windows:**
```
%APPDATA%\Cline\mcp_settings.json
```

加入相同的配置：

```json
{
  "mcpServers": {
    "weather-api": {
      "command": "java",
      "args": [
        "-jar",
        "/完整路徑/到/mcp-weather-springboot-api/target/mcp-weather-springboot-api-1.0.0.jar",
        "--spring.profiles.active=mcp"
      ]
    }
  }
}
```

重新載入 VS Code 後即可使用。

#### 配置 Continue.dev

在專案根目錄或 `~/.continue/config.json` 中加入：

```json
{
  "mcpServers": {
    "weather-api": {
      "command": "java",
      "args": [
        "-jar",
        "/完整路徑/到/mcp-weather-springboot-api/target/mcp-weather-springboot-api-1.0.0.jar",
        "--spring.profiles.active=mcp"
      ]
    }
  }
}
```

**重要提示：**
- 請將路徑替換為您的實際專案路徑
- 必須使用絕對路徑
- 注意 `--spring.profiles.active=mcp` 參數啟用 MCP 模式
- 每個客戶端會啟動獨立的 MCP server 實例

#### 重啟 Claude Desktop

完全關閉並重新啟動 Claude Desktop。

#### 使用 MCP 工具

在 Claude Desktop 中輸入：

```
請幫我查詢舊金山的天氣預報（緯度：37.7749，經度：-122.4194）
```

或

```
請查詢加州（CA）的天氣警報
```

## 運行模式比較

| 特性 | HTTP API 模式 | MCP 模式 |
|------|--------------|----------|
| 啟動方式 | `java -jar app.jar` | `java -jar app.jar --spring.profiles.active=mcp` |
| Web Server | ✅ 啟動（port 8080） | ❌ 不啟動 |
| REST API | ✅ 可用 | ❌ 不可用 |
| MCP Server | ❌ 不啟動 | ✅ 啟動（STDIO） |
| Claude Desktop | ❌ 無法連接 | ✅ 可連接 |
| 日誌輸出 | Console | 檔案 (`~/mcp-weather-api.log`) |
| 適用場景 | 微服務、API 整合 | Claude Desktop 工具 |

## 同時運行兩種模式

可以在不同終端機同時運行兩種模式：

**終端機 1 - HTTP API:**
```bash
java -jar target/mcp-weather-springboot-api-1.0.0.jar
```

**終端機 2 - MCP Server（或 Claude Desktop 配置）:**
```bash
java -jar target/mcp-weather-springboot-api-1.0.0.jar --spring.profiles.active=mcp
```

兩個模式共用相同的業務邏輯，但提供不同的接入方式。

## 技術架構

### 核心元件

**WeatherService**
- 封裝與 NWS API 的互動
- 提供兩種格式的資料：
  - 結構化資料（REST API）
  - 格式化文字（MCP）
- 使用 Spring WebFlux 進行非同步 HTTP 請求

**WeatherController**
- 提供 REST API 端點
- 返回 JSON 格式資料
- 只在 HTTP 模式下啟動

**McpServerRunner**
- 實作 MCP 協議
- 透過 STDIO 與 Claude Desktop 通訊
- 只在 MCP 模式下啟動（`@ConditionalOnProperty`）

### 雙模式設計

使用 Spring Profile 機制實現模式切換：

- **預設模式（HTTP）**: 啟動 Web Server，提供 REST API
- **MCP 模式**: 關閉 Web Server，啟動 STDIO 通訊

## 日誌管理

### HTTP API 模式
- 日誌輸出到 console
- 方便開發和除錯

### MCP 模式
- 日誌輸出到檔案：`~/mcp-weather-api.log`
- 避免干擾 STDIO 通訊
- 查看日誌：`tail -f ~/mcp-weather-api.log`

## 疑難排解

### HTTP API 無法啟動

1. 檢查 port 8080 是否被佔用
   ```bash
   lsof -i :8080
   ```

2. 更改 port（在 `application.properties`）
   ```properties
   server.port=9090
   ```

### MCP 模式無法連接 Claude Desktop

1. 確認配置檔案路徑正確
2. 確認使用絕對路徑
3. 確認有 `--spring.profiles.active=mcp` 參數
4. 查看日誌檔案：`~/mcp-weather-api.log`
5. 查看 Claude Desktop 日誌：`~/Library/Logs/Claude/mcp-server-weather-api.log`

### API 查詢失敗

1. 確認網路連線正常
2. 確認經緯度或州代碼格式正確
3. 測試 NWS API：
   ```bash
   curl https://api.weather.gov/points/39.7456,-97.0892
   curl https://api.weather.gov/alerts/active/area/CA
   ```

## 擴展建議

1. **新增快取機制**
   - 使用 Spring Cache 減少 API 呼叫
   - 設定合理的快取過期時間

2. **新增資料庫支援**
   - 儲存歷史查詢記錄
   - 分析查詢趨勢

3. **新增更多端點**
   - 城市名稱查詢（需整合地理編碼 API）
   - 雷達圖、衛星雲圖

4. **新增監控與指標**
   - Spring Boot Actuator
   - Prometheus metrics

5. **容器化部署**
   - Docker 化
   - Kubernetes 部署

## 參考資源

- [Spring Boot 文件](https://spring.io/projects/spring-boot)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [National Weather Service API](https://www.weather.gov/documentation/services-web-api)
- [Claude Desktop](https://claude.ai/download)

## 授權

本專案僅供學習與示範用途。
