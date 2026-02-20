# Semantic Kernel MCP Order Server 訂單查詢 MCP-Streamable HTTP 伺服器示範

這是一個展示 **Model Context Protocol（MCP）** 的 .NET MCP Server 應用程式，透過 **Streamable HTTP** 傳輸模式對外暴露「訂單查詢工具」，供 AI Agent 透過 HTTP 協定呼叫。

本專案是 `semantic-mcp-order-stdio-server` 的 **HTTP 版本**，功能完全相同，差別僅在傳輸層由 Stdio 改為 Streamable HTTP，使 Server 成為獨立運行的 HTTP 服務，可同時服務多個 Client。

---

## ★ Stdio vs Streamable HTTP：傳輸模式完整比較

> 理解兩種模式的差異，是選擇正確架構的關鍵。

### Server 端差異

| 比較項目 | stdio 版本（semantic-mcp-order-stdio-server） | Streamable HTTP 版本（本專案） |
|---------|---------------------------------------------|-------------------------------|
| **.csproj Sdk** | `Microsoft.NET.Sdk` | `Microsoft.NET.Sdk.Web` |
| **Host 類型** | .NET Generic Host | ASP.NET Core WebApplication |
| **Builder** | `Host.CreateApplicationBuilder()` | `WebApplication.CreateBuilder()` |
| **傳輸層設定** | `.WithStdioServerTransport()` | `.WithHttpTransport()` |
| **路由掛載** | 不需要 | `app.MapMcp()`（必要，否則 HTTP 404） |
| **啟動方式** | 由 Client 自動以子程序啟動 | 需獨立手動啟動，持續監聽 Port |
| **通訊機制** | Stdin / Stdout（JSON-RPC） | HTTP POST + SSE（JSON-RPC） |
| **監聽 Port** | 不佔用 Port | 預設 http://localhost:5121 |
| **多 Client** | 每個 Client 各啟動一個 Server 實例 | 多個 Client 可同時連線同一 Server |
| **部署場景** | 本機單一用戶、IDE 整合、CLI 工具 | 微服務、共享工具服務、跨網路存取 |
| **Server 生命週期** | 跟隨 Client 子程序，Client 結束即停止 | 獨立長期運行，與 Client 無關 |

### 程式碼差異（Server 端）

```csharp
// ── stdio 版本 ──────────────────────────────────────────────────────────────
// .csproj: Sdk="Microsoft.NET.Sdk"
using ModelContextProtocol.Server;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);   // Generic Host
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()                      // ← 差異點
    .WithToolsFromAssembly();

await builder.Build().RunAsync();                    // 阻塞等待 Client 關閉

// ── Streamable HTTP 版本（本專案）────────────────────────────────────────────
// .csproj: Sdk="Microsoft.NET.Sdk.Web"
using ModelContextProtocol.Server;
using ModelContextProtocol.AspNetCore;               // ← 新增引用

var builder = WebApplication.CreateBuilder(args);    // ← ASP.NET Core WebApplication
builder.Services
    .AddMcpServer()
    .WithHttpTransport()                             // ← 差異點（ModelContextProtocol.AspNetCore）
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapMcp();                                        // ← 差異點（必要，掛載 HTTP 路由）
app.Run();                                           // Kestrel 持續監聽 HTTP 請求
```

### Client 端差異（對應的 Console Client）

| 比較項目 | stdio Client（semantic-mcp-stdio-server-console-client） | HTTP Client（semantic-mcp-shttp-server-console-client） |
|---------|--------------------------------------------------------|------------------------------------------------------|
| **Transport 類別** | `StdioClientTransport` | `SseClientTransport` |
| **Options 類別** | `StdioClientTransportOptions` | `SseClientTransportOptions` |
| **連線目標** | 指定可執行檔路徑（`dotnet run`） | 指定 HTTP URL（`Endpoint`） |
| **Server 啟動** | Client 自動啟動子程序 | 需事先手動啟動 Server |
| **TransportMode** | 無此設定 | `HttpTransportMode.StreamableHttp` |

---

## 為什麼需要 Streamable HTTP 模式？

### stdio 模式的限制

```
Client A ─── 子程序 ──→ Server 實例 A（僅服務 Client A）
Client B ─── 子程序 ──→ Server 實例 B（僅服務 Client B）
Client C ─── 子程序 ──→ Server 實例 C（僅服務 Client C）
                           ↑ 每個 Client 各自佔用資源，無法共享
```

### Streamable HTTP 模式的解決方案

```
Client A ─┐
Client B ─┼──→ HTTP POST http://localhost:5121/ ──→ 單一 Server 實例（共享）
Client C ─┘                                          ↑ 資源集中管理，可跨網路部署
```

## 功能特性

- **MCP Server**：基於 Model Context Protocol SDK 的標準化工具伺服器
- **Streamable HTTP 傳輸**：透過 ASP.NET Core 承載，支援 HTTP POST + SSE 雙模式
- **向下相容**：同時支援新版 Streamable HTTP（2025-03-26）與舊版 SSE（2024-11-05）協定
- **自動工具掃描**：`WithToolsFromAssembly()` 自動掃描所有 `[McpServerToolType]` 類別
- **訂單查詢工具**：
  - `GetOrderById`：依訂單編號查詢單筆訂單
  - `SearchOrdersByCustomer`：依客戶姓名關鍵字模糊查詢

## 系統架構

```
┌──────────────────────────────────────────────────────────────┐
│   semantic-mcp-shttp-server-console-client（Client 端）       │
│                                                              │
│   SseClientTransport（TransportMode = StreamableHttp）        │
│   Endpoint = http://localhost:5121/                          │
└──────────────────────────────────────────────────────────────┘
                  ↕ JSON-RPC over HTTP POST / SSE
┌──────────────────────────────────────────────────────────────┐
│   semantic-mcp-order-shttp-server（本專案 / Server 端）       │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  ASP.NET Core WebApplication（Kestrel HTTP Server）    │  │
│  │  監聽 http://localhost:5121                            │  │
│  └────────────────────────────────────────────────────────┘  │
│                         ↕                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  MCP Server（WithHttpTransport + app.MapMcp()）        │  │
│  │  POST / → Streamable HTTP JSON-RPC 處理               │  │
│  │  GET  /sse + POST /message → 舊版 SSE 相容端點        │  │
│  └────────────────────────────────────────────────────────┘  │
│                         ↕                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  OrderTool（[McpServerToolType]）                      │  │
│  │  GetOrderById(orderId)          → OrderDto?            │  │
│  │  SearchOrdersByCustomer(keyword) → IEnumerable<OrderDto>│ │
│  └────────────────────────────────────────────────────────┘  │
│                         ↕                                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  模擬訂單資料（可替換為資料庫查詢）                    │  │
│  │  訂單 1001 王小美 1,299元 已出貨                       │  │
│  │  訂單 1002 陳錢錢 2,599元 處理中                       │  │
│  │  訂單 1003 阿土伯   499元 已取消                       │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

## app.MapMcp() 掛載的 HTTP 端點

```
HTTP 端點                方法    說明
────────────────────────────────────────────────────────────
/                        POST    Streamable HTTP（MCP 2025-03-26 規範）
/sse                     GET     SSE 長連線（MCP 2024-11-05 舊版，向下相容）
/message                 POST    SSE 模式的請求端點（向下相容）
```

> `MapMcp()` 的 `pattern` 參數預設為 `""`（空字串），端點掛載於根路徑 `/`。
> 若需指定路徑前綴，可改為 `app.MapMcp("/mcp")`，並同步修改 Client 的 `Endpoint`。

## 重要：需手動事先啟動 Server

與 stdio 版本不同，**本 Server 需要事先手動啟動**，Client 才能連線。

```bash
# 步驟 1：在終端機 A 啟動 MCP Server
cd semantic-mcp-order-shttp-server
dotnet run

# 步驟 2：在終端機 B 啟動 Console Client（Server 已在運行中）
cd semantic-mcp-shttp-server-console-client
dotnet run
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本

## 專案結構

```
semantic-mcp-order-shttp-server/
├── Program.cs                                  # 主程式（WebApplication 建立、MCP Server 設定）
├── semantic-mcp-order-shttp-server.csproj      # 專案設定（Sdk="Microsoft.NET.Sdk.Web"）
├── Properties/
│   └── launchSettings.json                     # HTTP 監聽 Port 設定（預設 5121）
├── model/
│   └── OrderDto.cs                             # 訂單資料傳輸物件（Record）
├── tools/
│   └── OrderTool.cs                            # MCP 工具實作（訂單查詢邏輯）
└── README.md                                   # 本文件
```

## 專案建立步驟

### 1. 建立 ASP.NET Core Web 專案（非 Console）

```bash
dotnet new web -n semantic-mcp-order-shttp-server
```

> **注意**：使用 `dotnet new web`（ASP.NET Core），而非 `dotnet new console`（stdio 版本用法）。

### 2. 安裝 NuGet 套件

```bash
dotnet add package ModelContextProtocol --version 0.2.0-preview.3
dotnet add package ModelContextProtocol.AspNetCore --version 0.2.0-preview.3
```

> stdio 版本需要 `Microsoft.Extensions.Hosting`，HTTP 版本改用 `ModelContextProtocol.AspNetCore`（已內含 Web 所需的 Host 功能）。

### 3. 設定 RootNamespace（.csproj）

```xml
<PropertyGroup>
    <RootNamespace>semantic_mcp_order_shttp_server</RootNamespace>
</PropertyGroup>
```

## 執行專案

```bash
# 編譯
dotnet build

# 啟動 Server（持續運行，按 Ctrl+C 停止）
dotnet run
```

啟動後應看到類似輸出：

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5121
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## 核心程式碼說明

### 1. MCP Server 啟動（Program.cs）

使用 ASP.NET Core WebApplication 管理 HTTP 生命週期，並掛載 MCP HTTP 端點：

```csharp
// ── stdio 版本（比較參考）────────────────────────────────────────────────────
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
await builder.Build().RunAsync();

// ── Streamable HTTP 版本（本專案）────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMcpServer()
    .WithHttpTransport()        // 來自 ModelContextProtocol.AspNetCore，stdio 版本無此行
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapMcp();                   // 掛載 HTTP 路由（必要），stdio 版本無此行
app.Run();
```

### 2. 工具定義（tools/OrderTool.cs）— 與 stdio 版本完全相同

工具定義完全不受傳輸模式影響，兩版本的 `OrderTool.cs` 邏輯一致：

```csharp
[McpServerToolType]
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
| ModelContextProtocol | 0.2.0-preview.3 | MCP Server 核心框架，提供 Attribute 與工具掃描 |
| ModelContextProtocol.AspNetCore | 0.2.0-preview.3 | ASP.NET Core 整合，提供 `WithHttpTransport()` 與 `MapMcp()` |

> stdio 版本額外需要 `Microsoft.Extensions.Hosting` 和 `Microsoft.Extensions.Logging.Console`；HTTP 版本改用 `Microsoft.NET.Sdk.Web`，這些依賴已隱含包含。

## 關鍵概念說明

### WithHttpTransport() vs WithStdioServerTransport()

- `WithStdioServerTransport()`：使用 Console.In / Console.Out 作為通訊通道，屬於 `ModelContextProtocol` 套件
- `WithHttpTransport()`：使用 HTTP POST + SSE 作為通訊通道，屬於 `ModelContextProtocol.AspNetCore` 套件

### 為什麼需要 app.MapMcp()？

stdio 版本的 MCP 通訊管線直接掛載在 Generic Host 上，不需要路由。
HTTP 版本需透過 `app.MapMcp()` 將 MCP 的 HTTP 端點正式注入 ASP.NET Core 的路由系統，才能讓 Kestrel 知道如何處理進來的 MCP 請求。

### Streamable HTTP 與 SSE 的相容

`MapMcp()` 同時掛載 Streamable HTTP（新版，POST `/`）與 SSE（舊版，GET `/sse` + POST `/message`）兩套端點，向下相容不同版本的 MCP Client。

### 工具定義的傳輸無關性

`[McpServerToolType]` / `[McpServerTool]` 等 Attribute 的設計完全不涉及傳輸層，無論 stdio 或 HTTP，工具程式碼保持不變，只有 `Program.cs` 的設定需要調整。

## 如何新增工具

與 stdio 版本相同，只需在 `tools/` 資料夾新增類別，無需修改 `Program.cs`：

```csharp
[McpServerToolType]
public class ProductTool
{
    [McpServerTool, Description("Query product info by product ID")]
    public ProductDto? GetProductById(
        [Description("The product ID to query")] int productId) =>
        null; // 替換為實際查詢邏輯
}
```

## 常見問題

### Q: 與 stdio 版本比，何時應選擇 Streamable HTTP？

| 場景 | 建議模式 |
|------|---------|
| IDE 整合、本機單一用戶 CLI 工具 | **stdio** |
| 多個 Client 需共享同一 Server | **Streamable HTTP** |
| Server 需跨網路（不同主機）部署 | **Streamable HTTP** |
| 需要 REST 工具（curl、Postman）測試 | **Streamable HTTP** |
| 簡單快速整合、無需部署管理 | **stdio** |

### Q: 啟動後終端機顯示的監聽 Port 在哪裡設定？

**A: 在 `Properties/launchSettings.json` 的 `applicationUrl` 中設定**，預設為 `http://localhost:5121`。修改此處即可更換 Port，並記得同步修改 Client 的 `Endpoint`。

### Q: MapMcp() 不帶參數時，端點路徑是 /mcp 嗎？

**A: 不是，預設路徑是根路徑 `/`（空字串 pattern）。** Client 的 `Endpoint` 應設為 `http://localhost:5121/` 而非 `http://localhost:5121/mcp`。若要改為 `/mcp`，需呼叫 `app.MapMcp("/mcp")` 並同步修改 Client。

### Q: 如何將靜態資料替換為真實資料庫查詢？

**A: 只需修改 `OrderTool.cs`，注入 Repository 即可，Server 設定不需修改：**

```csharp
[McpServerToolType]
public class OrderTool(IOrderRepository repository)
{
    [McpServerTool, Description("Query order data by order ID")]
    public async Task<OrderDto?> GetOrderById(
        [Description("The order ID")] int orderId) =>
        await repository.GetByIdAsync(orderId);
}
```

### Q: Description 建議用中文還是英文？

**A: 建議用英文。** Tool 的 `Description` 會傳給 AI Agent（LLM）作為工具選擇的依據，英文說明通常能提升 AI 的理解準確度。

## 注意事項

1. **需手動啟動**：與 stdio 版本不同，本 Server 需手動啟動後，Client 才能連線
2. **Port 佔用**：Server 啟動後佔用 Port 5121，確認無衝突
3. **模擬資料**：目前使用靜態清單，實務上應替換為資料庫查詢
4. **Preview 套件**：`ModelContextProtocol` 及 `ModelContextProtocol.AspNetCore` 目前為預覽版本，API 可能會變更

## 學習路徑建議

1. **hello-semantic-kernel-app** — 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** — 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** — 學習 ChatHistory
4. **semantic-kernel-multi-function-app** — 多 Plugin + Filter
5. **semantic-chatcomplete-agent-app** — 學習 ChatCompletionAgent
6. **semantic-mcp-order-stdio-server** — 學習 MCP Server（stdio 版本）
7. **semantic-mcp-stdio-server-console-client** — 學習 MCP Client + stdio
8. **semantic-mcp-order-shttp-server** ← **本專案** — 學習 MCP Server（Streamable HTTP）
9. **semantic-mcp-shttp-server-console-client** — 學習 MCP Client + Streamable HTTP

## 參考資源

- [Model Context Protocol 官方文檔](https://modelcontextprotocol.io/)
- [MCP Streamable HTTP 規範（2025-03-26）](https://modelcontextprotocol.io/specification/2025-03-26/basic/transports#streamable-http)
- [MCP SSE 規範（2024-11-05）](https://modelcontextprotocol.io/specification/2024-11-05/basic/transports#http-with-sse)
- [ModelContextProtocol .NET SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [ASP.NET Core 文檔](https://learn.microsoft.com/aspnet/core/)
- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
