# MCP Weather Server - Spring Boot 版本

本專案是 mcp-weather 的 Spring Boot 實作版本，示範如何使用 Java 和 Spring Boot 建立一個 Model Context Protocol (MCP) 伺服器，並整合至 Claude Desktop，提供美國天氣資訊查詢功能。

## 功能特色

- 使用 Spring Boot 框架建立 MCP 伺服器
- 整合美國國家氣象局 (National Weather Service) API
- 提供兩個實用的天氣查詢工具：
  - **get_forecast**: 根據經緯度查詢天氣預報
  - **get_alerts**: 查詢指定州的天氣警報
- 與 Claude Desktop 無縫整合
- 完整的錯誤處理與日誌記錄
- 使用 WebFlux 實現非同步 HTTP 請求

## 環境需求

- Java 17 或以上版本
- Maven 3.6+
- Claude Desktop 應用程式

## 專案結構

```
mcp-weather-spring-boot/
├── src/
│   └── main/
│       ├── java/
│       │   └── com/
│       │       └── example/
│       │           └── mcpweather/
│       │               ├── McpWeatherApplication.java  # MCP 伺服器主程式
│       │               ├── dto/                        # 資料傳輸物件
│       │               │   ├── AlertsResponse.java
│       │               │   ├── ForecastResponse.java
│       │               │   └── PointsResponse.java
│       │               └── service/                    # 業務邏輯層
│       │                   └── WeatherService.java
│       └── resources/
│           └── application.properties                  # 應用程式設定
├── pom.xml                                            # Maven 設定檔
└── README.md                                          # 本文件
```

## 安裝步驟

### 1. 克隆或建立專案

確保您已經在 `mcp-weather-spring-boot` 目錄中擁有所有專案檔案。

### 2. 編譯專案

使用 Maven 編譯專案：

```bash
cd mcp-weather-spring-boot
mvn clean package
```

這會建立一個可執行的 JAR 檔案在 `target/` 目錄中。

### 3. 測試執行

您可以直接執行應用程式進行測試：

```bash
java -jar target/mcp-weather-spring-boot-1.0.0.jar
```

或使用 Maven：

```bash
mvn spring-boot:run
```

## 整合 Claude Desktop

### 4. 設定 Claude Desktop MCP 配置

#### 4.1 找到配置檔案位置

**macOS:**
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

**Windows:**
```
%APPDATA%\Claude\claude_desktop_config.json
```

#### 4.2 編輯配置檔案

在配置檔案中加入 weather-spring-boot MCP 伺服器設定：

```json
{
  "mcpServers": {
    "weather-spring-boot": {
      "command": "java",
      "args": [
        "-jar",
        "/完整路徑/到/mcp-weather-spring-boot/target/mcp-weather-spring-boot-1.0.0.jar"
      ]
    }
  }
}
```

**重要提示：**
- 請將 `/完整路徑/到/mcp-weather-spring-boot` 替換為您的專案實際路徑
- 路徑必須是絕對路徑，不可使用相對路徑或 `~` 符號
- Windows 系統請使用反斜線 `\\` 或正斜線 `/`
- 確保已先執行 `mvn clean package` 編譯出 JAR 檔案

#### 4.3 重新啟動 Claude Desktop

儲存配置檔案後，完全關閉並重新啟動 Claude Desktop，讓設定生效。

## 使用範例

### 查詢天氣預報

在 Claude Desktop 中輸入：

```
請幫我查詢舊金山的天氣預報（座標：37.7749, -122.4194）
```

Claude 會使用 `get_forecast` 工具取得未來 5 個時段的天氣預報。

### 查詢天氣警報

在 Claude Desktop 中輸入：

```
請查詢加州（CA）目前有哪些天氣警報
```

Claude 會使用 `get_alerts` 工具取得該州的活動警報資訊。

## 技術架構

### 主要元件說明

#### McpWeatherApplication.java
- MCP 伺服器的主程式
- 實作 JSON-RPC 2.0 協議
- 透過 STDIO 與 Claude Desktop 通訊
- 處理 initialize、tools/list、tools/call 等 MCP 請求

#### WeatherService.java
- 封裝與 NWS API 的互動邏輯
- 使用 Spring WebFlux WebClient 進行非同步 HTTP 請求
- 提供 getForecast 和 getAlerts 兩個服務方法
- 完整的錯誤處理和日誌記錄

#### DTO 類別
- **PointsResponse**: 對應 NWS API /points 端點的回應格式
- **ForecastResponse**: 對應天氣預報 API 的回應格式
- **AlertsResponse**: 對應天氣警報 API 的回應格式

### API 流程說明

**天氣預報查詢 (get_forecast):**
1. 接收經緯度參數
2. 呼叫 `/points/{lat},{lon}` 取得該位置的預報網格點
3. 從回應中提取預報 URL
4. 呼叫預報 URL 取得詳細預報資料
5. 格式化並返回未來 5 個時段的預報

**天氣警報查詢 (get_alerts):**
1. 接收州代碼參數
2. 呼叫 `/alerts/active/area/{state}` 取得該州的活動警報
3. 格式化並返回所有警報資訊

### 技術特點

- **Spring Boot 3.2**: 使用最新的 Spring Boot 框架
- **WebFlux**: 非同步、反應式的 HTTP 客戶端
- **Jackson**: JSON 序列化與反序列化
- **Lombok**: 減少樣板程式碼
- **SLF4J**: 標準化的日誌記錄

## 日誌記錄

應用程式使用 SLF4J 進行日誌記錄，日誌會輸出到標準錯誤串流（stderr），不會干擾 MCP 協議的 STDIO 通訊。

日誌格式範例：
```
2024-01-01 12:00:00 - getForecast latitude=37.7749, longitude=-122.4194
2024-01-01 12:00:05 - getAlerts state=CA
2024-01-01 12:00:10 - Error fetching forecast: HTTP 404
```

## 注意事項

- **API 限制**: NWS API 僅提供美國境內的天氣資訊
- **座標範圍**: 經緯度必須在美國境內，否則 API 會返回錯誤
- **州代碼**: 查詢警報時請使用標準的兩字母州代碼（如 CA, NY, TX）
- **網路需求**: MCP 伺服器需要網際網路連線才能存取 NWS API
- **User-Agent**: NWS API 要求所有請求必須包含 User-Agent 標頭（已在程式中設定）
- **Java 版本**: 需要 Java 17 或以上版本

## 疑難排解

### MCP 伺服器無法載入

1. 確認 `claude_desktop_config.json` 中的路徑正確
2. 確認已執行 `mvn clean package` 並成功產生 JAR 檔案
3. 檢查 JAR 檔案路徑是否為絕對路徑
4. 確認 Java 已正確安裝並可在命令列執行
5. 查看 Claude Desktop 的錯誤訊息

### 編譯錯誤

1. 確認 Java 版本為 17 或以上：`java -version`
2. 確認 Maven 版本為 3.6 或以上：`mvn -version`
3. 嘗試清除並重新編譯：`mvn clean install`

### 工具呼叫失敗

1. 檢查應用程式的日誌輸出
2. 確認網際網路連線正常
3. 驗證輸入的經緯度或州代碼格式正確
4. 嘗試直接存取 NWS API 確認服務是否正常：
   - https://api.weather.gov/points/39.7456,-97.0892
   - https://api.weather.gov/alerts/active/area/CA

## 與 Python 版本的差異

本專案是 `mcp-weather` (Python FastMCP 版本) 的 Java/Spring Boot 實作。主要差異：

| 特性 | Python 版本 | Spring Boot 版本 |
|------|-------------|-----------------|
| 框架 | FastMCP | Spring Boot |
| 語言 | Python 3.12+ | Java 17+ |
| HTTP 客戶端 | httpx | WebFlux WebClient |
| 套件管理 | uv | Maven |
| 啟動命令 | uv run | java -jar |
| 型別系統 | 動態型別 | 靜態型別 |

兩個版本提供完全相同的功能，可根據您的技術棧和偏好選擇使用。

## 參考資源

- [Model Context Protocol 規範](https://modelcontextprotocol.io/)
- [Spring Boot 文件](https://spring.io/projects/spring-boot)
- [Spring WebFlux 文件](https://docs.spring.io/spring-framework/reference/web/webflux.html)
- [National Weather Service API 文件](https://www.weather.gov/documentation/services-web-api)
- [Claude Desktop 文件](https://claude.ai/download)

## 擴展建議

本專案可作為基礎進行擴展：

1. **新增單元測試**: 使用 JUnit 和 Mockito 撰寫測試
2. **配置外部化**: 將 API URL 等設定移到配置檔
3. **快取機制**: 使用 Spring Cache 減少 API 呼叫次數
4. **更多資料類型**: 支援雷達圖、衛星雲圖等
5. **國際化**: 支援其他國家的天氣服務
6. **非同步處理**: 優化長時間執行的請求

## 授權

本專案僅供學習與示範用途。
