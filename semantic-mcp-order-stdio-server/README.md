# Semantic Kernel MCP Order Server 訂單查詢 MCP-STDIO伺服器示範

這是一個展示 **Model Context Protocol（MCP）** 的 .NET MCP Server 應用程式，透過 Stdio 傳輸模式對外暴露「訂單查詢工具」，供 AI Agent 透過 MCP 協定呼叫。

## 為什麼使用 MCP Server？

### 問題：AI Agent 無法直接存取外部系統資料

AI Agent（如 Semantic Kernel 的 ChatCompletionAgent）本身只能處理語言推理，無法直接查詢資料庫、呼叫業務 API 或存取企業系統的即時資料：

```
// 沒有工具的 Agent — 只能依靠訓練資料，無法查詢真實訂單
agent.Instructions = "你是訂單查詢助手...";
// 使用者問：「請查詢訂單 1001 的狀態」
// Agent：「很抱歉，我無法存取即時訂單資料...」❌
```

### 解決方案：MCP Server 橋接 AI 與業務系統

MCP Server 將業務邏輯封裝為「工具（Tool）」，讓 AI Agent 透過標準化的 MCP 協定呼叫，取得即時業務資料：

```
// 有 MCP 工具的 Agent — 可以查詢真實訂單
agent.Instructions = "你是訂單查詢助手...";
// 使用者問：「請查詢訂單 1001 的狀態」
// Agent 呼叫 MCP Tool → GetOrderById(1001) → 回傳真實資料 ✅
```

## MCP Server 的核心優勢

| 優勢 | 說明 |
|------|------|
| **標準化協定** | 使用 Model Context Protocol，任何相容 MCP 的 Client 均可對接 |
| **解耦設計** | Server 與 Client 完全分離，可獨立部署、維護、升級 |
| **自動探索** | Client 可呼叫 `ListToolsAsync()` 自動取得所有可用工具清單 |
| **零侵入整合** | 新增工具只需在 Server 新增類別，Client 無需修改任何程式碼 |
| **Stdio 傳輸** | 透過標準輸入/輸出通訊，Client 以子程序方式啟動 Server，**無需手動預先啟動 Server** |

## 與其他整合方式的差異

| 項目 | 直接引用 DLL | REST API | MCP Server（本專案） |
|------|------------|---------|-------------------|
| **通訊方式** | 行程內呼叫 | HTTP/HTTPS | JSON-RPC over Stdio |
| **啟動方式** | 不需啟動 | 需預先啟動服務 | Client 自動以子程序啟動 |
| **工具探索** | 需手動閱讀 API 文件 | 需 OpenAPI Spec | `ListToolsAsync()` 自動取得 |
| **AI 整合** | 需手動撰寫 Kernel Function | 需手動包裝 | MCP SDK 自動轉換為 KernelFunction |
| **跨語言** | 僅限 .NET | 任何語言 | 任何支援 MCP 的語言 |
| **部署複雜度** | 低 | 高（需網路、Port） | 低（隨 Client 啟動） |

## 功能特性

- **MCP Server**：基於 Model Context Protocol SDK 的標準化工具伺服器
- **Stdio 傳輸**：透過標準輸入/輸出與 Client 通訊，由 Client 自動以子程序方式啟動
- **自動工具掃描**：`WithToolsFromAssembly()` 自動掃描所有 `[McpServerToolType]` 類別
- **訂單查詢工具**：
  - `GetOrderById`：依訂單編號查詢單筆訂單
  - `SearchOrdersByCustomer`：依客戶姓名關鍵字模糊查詢

## 系統架構

```
┌─────────────────────────────────────────────────────────────┐
│          semantic-mcp-server-console-client（Client 端）      │
│                                                             │
│   StdioClientTransport                                      │
│   ↓ 以子程序方式啟動 MCP Server（dotnet run）               │
│   ↓ 透過 Stdin/Stdout 傳送 MCP JSON-RPC 訊息               │
└─────────────────────────────────────────────────────────────┘
                    ↕ JSON-RPC over Stdio
┌─────────────────────────────────────────────────────────────┐
│          semantic-mcp-order-server（本專案 / Server 端）     │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  .NET Generic Host（應用程式生命週期管理）           │    │
│  └─────────────────────────────────────────────────────┘    │
│                         ↕                                   │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  MCP Server（WithStdioServerTransport）             │    │
│  │  接收 JSON-RPC 請求，路由到對應工具方法             │    │
│  └─────────────────────────────────────────────────────┘    │
│                         ↕                                   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  OrderTool（[McpServerToolType]）                    │   │
│  │                                                      │   │
│  │  GetOrderById(orderId)          → OrderDto?          │   │
│  │  SearchOrdersByCustomer(keyword) → IEnumerable<OrderDto>│   │
│  └──────────────────────────────────────────────────────┘   │
│                         ↕                                   │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  模擬訂單資料（靜態清單，可替換為資料庫查詢）       │    │
│  │  訂單 1001 王小美 已出貨                            │    │
│  │  訂單 1002 陳錢錢 處理中                            │    │
│  │  訂單 1003 阿土伯 已取消                            │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## 執行流程

```
Client 端（console-client）
        │
        ▼
StdioClientTransport 啟動子程序
dotnet run --project ../semantic-mcp-order-server
        │
        ▼
┌───────────────────────────────────────────┐
│  MCP Server 啟動，開始監聽 Stdin          │
│  等待 Client 發送 JSON-RPC 請求           │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  Client 呼叫 ListToolsAsync()             │
│  Server 回傳工具清單：                    │
│  - GetOrderById                           │
│  - SearchOrdersByCustomer                 │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  AI Agent 判斷需呼叫工具                  │
│  透過 MCP 協定傳入參數                    │
│  Server 執行 OrderTool 方法               │
│  回傳 JSON 格式結果                       │
└───────────────────────────────────────────┘
```

## 重要：無需手動啟動 MCP Server

本專案設計為由 Client 端自動啟動，**不需要手動執行 `dotnet run` 來啟動 Server**。

`semantic-mcp-server-console-client` 的 `StdioClientTransport` 在執行時會自動以子程序方式啟動本 Server：

```csharp
// Console Client 端的程式碼（semantic-mcp-server-console-client/Program.cs）
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "dotnet",
        Arguments = ["run", "--project", "../semantic-mcp-order-server"],
        Name = "OrderServer"
    }));
// ↑ 呼叫此行時，MCP SDK 會自動啟動 semantic-mcp-order-server 子程序
//   Server 隨 Client 啟動，也隨 Client 結束而停止
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本

> **注意**：本 Server 通常由 `semantic-mcp-server-console-client` 自動啟動，不需單獨設定環境變數或執行。

## 專案結構

```
semantic-mcp-order-server/
├── Program.cs                              # 主程式（MCP Server 啟動、服務註冊）
├── semantic-mcp-order-server.csproj        # 專案設定
├── model/
│   └── OrderDto.cs                         # 訂單資料傳輸物件（Record）
├── tools/
│   └── OrderTool.cs                        # MCP 工具實作（訂單查詢邏輯）
└── README.md                               # 本文件
```

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-mcp-order-server
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package ModelContextProtocol --version 0.2.0-preview.3
dotnet add package Microsoft.Extensions.Hosting --version 9.0.2
dotnet add package Microsoft.Extensions.Logging.Console --version 9.0.2
```

### 3. 設定 RootNamespace（.csproj）

```xml
<PropertyGroup>
    <RootNamespace>McpOrderServer</RootNamespace>
</PropertyGroup>
```

## 執行專案

本 Server 通常由 `semantic-mcp-server-console-client` 自動啟動，**無需手動執行**。

若需單獨測試 Server 是否正常編譯，可執行：

```bash
# 僅編譯，確認語法正確
dotnet build

# 單獨執行（僅用於測試，正式使用由 Client 端自動啟動）
dotnet run
```

> 單獨執行時，Server 會啟動並等待 Stdin 輸入（MCP JSON-RPC 訊息），終端機會看起來像是「卡住」，這是正常行為。按 `Ctrl+C` 終止。

## 核心程式碼說明

### 1. MCP Server 啟動（Program.cs）

使用 .NET Generic Host 管理應用程式生命週期，並註冊 MCP Server 服務：

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()       // 採用 Stdio 傳輸
    .WithToolsFromAssembly();         // 自動掃描所有 [McpServerToolType] 類別

await builder.Build().RunAsync();
```

### 2. 工具定義（tools/OrderTool.cs）

使用 Attribute 標記類別與方法，MCP SDK 會自動將其暴露為可呼叫的工具：

```csharp
[McpServerToolType]  // 標記此類別包含 MCP 工具
public class OrderTool
{
    [McpServerTool, Description("Query order data by order ID")]
    public OrderDto? GetOrderById(
        [Description("The order ID to query, e.g. 1001")] int orderId) =>
        _orders.FirstOrDefault(o => o.Id == orderId);

    [McpServerTool, Description("Query order list by customer name keyword")]
    public IEnumerable<OrderDto> SearchOrdersByCustomer(
        [Description("Customer name or keyword to search, e.g. 王小美")] string keyword) =>
        _orders.Where(o =>
            o.Customer.Contains(keyword, StringComparison.OrdinalIgnoreCase));
}
```

### 3. 資料模型（model/OrderDto.cs）

使用 C# record 定義不可變的訂單資料結構，MCP SDK 會自動序列化為 JSON 回傳給 Client：

```csharp
public record OrderDto(
    int Id,              // 訂單編號
    string Customer,     // 客戶姓名
    DateTime OrderDate,  // 下單日期
    int Total,           // 訂單金額（元）
    string Status        // 訂單狀態
);
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| ModelContextProtocol | 0.2.0-preview.3 | MCP Server 核心框架，提供 Attribute 與 Stdio 傳輸 |
| Microsoft.Extensions.Hosting | 9.0.2 | .NET Generic Host，管理應用程式生命週期 |
| Microsoft.Extensions.Logging.Console | 9.0.2 | Console 日誌輸出，便於開發除錯 |

## 關鍵概念說明

### [McpServerToolType] 與 [McpServerTool]

- `[McpServerToolType]`：標記在類別上，告知 MCP SDK 這個類別包含工具方法
- `[McpServerTool]`：標記在方法上，將該方法暴露為 MCP 工具
- `[Description("...")]`：說明工具或參數的用途，AI Agent 會讀取此說明決定何時呼叫

### WithToolsFromAssembly()

自動掃描目前 Assembly 中所有標記了 `[McpServerToolType]` 的類別，無需手動逐一註冊。新增工具時只需新增類別，`Program.cs` 不需修改。

### Stdio 傳輸與子程序模式

MCP Server 透過 Stdin/Stdout 接收與回傳 JSON-RPC 訊息。Client 以子程序方式啟動 Server，Server 的生命週期由 Client 管理，Client 結束時 Server 也會隨之停止。

## 如何新增工具

只需在 `tools/` 資料夾新增類別，不需修改 `Program.cs`：

```csharp
[McpServerToolType]
public class ProductTool
{
    [McpServerTool, Description("Query product info by product ID")]
    public ProductDto? GetProductById(
        [Description("The product ID to query")] int productId) =>
        // 實作查詢邏輯...
        null;
}
```

## 常見問題

### Q: 執行 `dotnet run` 後終端機沒有任何輸出怎麼辦？

**A: 這是正常行為。** 本 Server 採用 Stdio 傳輸，啟動後會等待 Client 透過 Stdin 發送 MCP JSON-RPC 訊息。單獨執行時不會有互動介面，請改用 `semantic-mcp-server-console-client` 啟動完整示範。

### Q: 需要先手動啟動 MCP Server 再執行 Client 嗎？

**A: 不需要。** `semantic-mcp-server-console-client` 使用 `StdioClientTransport`，在執行時會自動以子程序方式啟動本 Server。直接執行 Client 即可，Server 會自動被帶起。

### Q: Description 建議用中文還是英文？

**A: 建議用英文。** Tool 的 `Description` 會透過 MCP 協定傳給 Client，AI Agent（LLM）讀取此說明來判斷何時應呼叫本工具。英文說明通常能提升 AI 模型的理解準確度與工具選擇的精確性。

### Q: 如何將靜態資料替換為真實資料庫查詢？

**A: 只需修改 `OrderTool.cs` 內的資料來源。** 注入 DbContext 或 Repository 即可，MCP 框架層無需任何修改：

```csharp
[McpServerToolType]
public class OrderTool(IOrderRepository repository)
{
    [McpServerTool, Description("Query order data by order ID")]
    public async Task<OrderDto?> GetOrderById(
        [Description("The order ID to query")] int orderId) =>
        await repository.GetByIdAsync(orderId);
}
```

## 注意事項

1. **模擬資料**：目前 `OrderTool` 使用靜態清單作為示範，實務上應替換為資料庫查詢
2. **Preview 套件**：`ModelContextProtocol` 目前為預覽版本，API 可能會變更
3. **Stdio 限制**：本 Server 設計為單一 Client 使用；若需多個 Client 同時連線，應改用 HTTP/SSE 傳輸模式

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** - 學習 ChatHistory
4. **semantic-kernel-multi-function-app** - 多 Plugin + Filter
5. **semantic-chatcomplete-agent-app** - 學習 ChatCompletionAgent
6. **semantic-aiassistant-agent-app** - 學習 OpenAIAssistantAgent
7. **semantic-mcp-order-server** ← **本專案** - 學習 MCP Server
8. **semantic-mcp-server-console-client** - 學習 MCP Client + Agent 整合

## 參考資源

- [Model Context Protocol 官方文檔](https://modelcontextprotocol.io/)
- [ModelContextProtocol .NET SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [.NET Generic Host 文檔](https://learn.microsoft.com/dotnet/core/extensions/generic-host)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
