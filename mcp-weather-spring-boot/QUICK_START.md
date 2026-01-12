# 快速開始指南

## 專案已編譯完成！

可執行的 JAR 檔案位於：
```
target/mcp-weather-spring-boot-1.0.0.jar
```

## 快速測試

1. **直接執行應用程式**
```bash
java -jar target/mcp-weather-spring-boot-1.0.0.jar
```

或使用啟動腳本：
```bash
./run.sh
```

應用程式會輸出初始化訊息，然後等待來自 stdin 的 MCP 協議請求。

## 整合到 Claude Desktop

### 步驟 1: 找到 Claude Desktop 配置檔案

**macOS:**
```bash
~/Library/Application Support/Claude/claude_desktop_config.json
```

**Windows:**
```
%APPDATA%\Claude\claude_desktop_config.json
```

### 步驟 2: 編輯配置檔案

加入以下設定（記得修改路徑為您的實際路徑）：

```json
{
  "mcpServers": {
    "weather-spring-boot": {
      "command": "java",
      "args": [
        "-jar",
        "/Users/timmacpro/PyProjects/mcpserver/mcp-weather-spring-boot/target/mcp-weather-spring-boot-1.0.0.jar"
      ]
    }
  }
}
```

**重要提示：**
- 請將路徑替換為您的實際專案路徑
- 必須使用絕對路徑
- Windows 系統使用 `\\` 或 `/` 作為路徑分隔符

### 步驟 3: 重啟 Claude Desktop

完全關閉並重新啟動 Claude Desktop，讓配置生效。

### 步驟 4: 驗證功能

啟動 Claude Desktop 後，可以檢查 MCP 伺服器是否成功載入。

嘗試以下查詢：

**查詢天氣預報：**
```
請幫我查詢舊金山的天氣（緯度：37.7749，經度：-122.4194）
```

**查詢天氣警報：**
```
請查詢加州（CA）的天氣警報
```

## 重新編譯專案

如果您修改了程式碼，可以重新編譯：

```bash
# 完整重新編譯並打包
mvn clean package -DskipTests

# 只編譯不打包
mvn clean compile
```

## 常見問題

### 1. 編譯錯誤

確認 Java 版本：
```bash
java -version  # 需要 Java 17 或以上
```

確認 Maven 版本：
```bash
mvn -version   # 需要 Maven 3.6 或以上
```

### 2. Claude Desktop 無法連接

- 確認 JAR 檔案路徑正確
- 確認使用絕對路徑
- 查看 Claude Desktop 的錯誤訊息
- 檢查 `claude_desktop_config.json` 的 JSON 格式

### 3. 查看應用程式日誌

日誌會輸出到 stderr，如需保存日誌：
```bash
java -jar target/mcp-weather-spring-boot-1.0.0.jar 2> app.log
```

## 技術規格

- **Java 版本**: 17+
- **Spring Boot 版本**: 3.2.1
- **MCP 協議版本**: 2024-11-05
- **JAR 檔案大小**: ~22MB（包含所有依賴）

## 支援的工具

1. **get_forecast** - 查詢天氣預報
   - 參數：latitude（緯度）、longitude（經度）
   - 返回：未來 5 個時段的天氣預報

2. **get_alerts** - 查詢天氣警報
   - 參數：state（美國州代碼，如 CA、NY）
   - 返回：該州的活動天氣警報

## 更多資訊

請參考完整的 [README.md](README.md) 文件。
