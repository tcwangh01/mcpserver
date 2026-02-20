// ============================================================================
// Program.cs — MCP Order Server (Streamable HTTP) 進入點
// ============================================================================
//
// ┌─────────────────────────────────────────────────────────────────────────┐
// │                         整體架構說明                                    │
// ├─────────────────────────────────────────────────────────────────────────┤
// │  本專案是一個 MCP（Model Context Protocol）Server，                      │
// │  透過 Streamable HTTP 傳輸模式對外暴露「訂單查詢工具」，                   │
// │  供 AI Agent（如 semantic-mcp-shttp-server-console-client）              │
// │  透過 HTTP 協定呼叫。                                                    │
// │                                                                         │
// │  請求流程：                                                              │
// │    Console Client                                                        │
// │      → HTTP POST http://localhost:5121/                                  │
// │      → MCP JSON-RPC over Streamable HTTP                                 │
// │      → MCP SDK 路由到對應的 [McpServerTool] 方法                          │
// │      → 回傳 JSON 結果給 Client                                            │
// └─────────────────────────────────────────────────────────────────────────┘
//
// ============================================================================
// ★ Streamable HTTP vs stdio 傳輸模式的核心差異 ★
// ============================================================================
//
//  ┌─────────────────┬──────────────────────────────┬──────────────────────────────┐
//  │ 比較項目         │ stdio 版本                    │ Streamable HTTP 版本（本專案）│
//  ├─────────────────┼──────────────────────────────┼──────────────────────────────┤
//  │ .csproj Sdk     │ Microsoft.NET.Sdk             │ Microsoft.NET.Sdk.Web        │
//  │ Host 類型        │ Generic Host                  │ ASP.NET Core WebApplication  │
//  │ Builder         │ Host.CreateApplicationBuilder │ WebApplication.CreateBuilder  │
//  │ 傳輸層設定       │ .WithStdioServerTransport()   │ .WithHttpTransport()         │
//  │ 路由掛載         │ 不需要                         │ app.MapMcp()（必要）         │
//  │ 啟動方式         │ Client 以子程序啟動本程式       │ 獨立執行，持續監聽 HTTP 請求  │
//  │ 通訊方式         │ Stdin / Stdout（JSON-RPC）    │ HTTP POST + SSE（JSON-RPC）  │
//  │ Client 連線      │ StdioClientTransport          │ SseClientTransport           │
//  │ 部署場景         │ 本機單一用戶、IDE 整合          │ 網路服務、多 Client 同時連線  │
//  │ Server 生命週期  │ 隨 Client 子程序啟動 / 結束    │ 獨立長期運行                 │
//  └─────────────────┴──────────────────────────────┴──────────────────────────────┘
//
// ============================================================================
//
// 【using 引用說明】
//
//   using semantic_mcp_order_shttp_server.Tools;
//     ↑ 引用 tools/OrderTool.cs 的命名空間。
//       雖然 WithToolsFromAssembly() 會自動掃描 Assembly，
//       但明確 using 可讓 IDE 正確解析型別並提供程式碼提示。
//
//   using ModelContextProtocol.Server;
//     ↑ 提供 .AddMcpServer()、.WithToolsFromAssembly() 等
//       MCP Server 核心服務的 DI 擴充方法。
//
//   using ModelContextProtocol.AspNetCore;
//     ↑ 提供 .WithHttpTransport() 和 app.MapMcp() 等
//       ASP.NET Core 整合的 HTTP 傳輸擴充方法。
//       ★ stdio 版本不需要此 using，這是 HTTP 版本特有的引用。
//
// ============================================================================

using semantic_mcp_order_shttp_server.Tools;
using ModelContextProtocol.Server;
using ModelContextProtocol.AspNetCore;

// ============================================================================
// 建立 WebApplication Builder（ASP.NET Core）
// ============================================================================
//
// ★ 與 stdio 版本的差異：
//
//   stdio 版本使用 Generic Host：
//     var builder = Host.CreateApplicationBuilder(args);
//     → 純主控台程式，無 HTTP 管線，僅管理 DI 容器與服務生命週期
//
//   HTTP 版本使用 ASP.NET Core WebApplication：
//     var builder = WebApplication.CreateBuilder(args);
//     → 完整的 Web 應用程式框架，包含 HTTP 管線、中介軟體、路由系統
//     → 監聽 Port 設定於 Properties/launchSettings.json（預設 http://localhost:5121）
//
// ============================================================================
var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 向 DI 容器註冊 MCP Server 服務
// ============================================================================
builder.Services
    // AddMcpServer()
    // ─────────────────────────────────────────────────────────────────────
    // 向 ASP.NET Core 的 DI 容器（IServiceCollection）
    // 註冊 MCP Server 所需的核心服務（session 管理、JSON-RPC 處理等）。
    // 回傳 IMcpServerBuilder，供後續串接傳輸層與工具設定。
    // ─────────────────────────────────────────────────────────────────────
    .AddMcpServer()

    // WithHttpTransport()
    // ─────────────────────────────────────────────────────────────────────
    // ★ 與 stdio 版本的關鍵差異點 ★
    //
    //   stdio 版本：.WithStdioServerTransport()
    //     → 透過 Console.In / Console.Out 進行 JSON-RPC 通訊
    //     → 不需要 HTTP 管線，不佔用任何 Port
    //
    //   HTTP 版本：.WithHttpTransport()（本方法）
    //     → 向 DI 容器註冊 Streamable HTTP 傳輸層所需的服務
    //     → 此方法來自 ModelContextProtocol.AspNetCore 套件
    //     → 支援 MCP 2025-03-26 規範的 Streamable HTTP 協定
    //     → 還需搭配 app.MapMcp() 才能正式掛載 HTTP 路由（見下方）
    // ─────────────────────────────────────────────────────────────────────
    .WithHttpTransport()

    // WithToolsFromAssembly()
    // ─────────────────────────────────────────────────────────────────────
    // 自動掃描目前 Assembly（本專案所有已編譯的 .cs 檔案），
    // 找出所有標記了 [McpServerToolType] 的類別（如 tools/OrderTool），
    // 並將其中標記了 [McpServerTool] 的方法註冊為可呼叫的 MCP 工具。
    //
    // ★ 工具定義本身與傳輸層無關：
    //   OrderTool.cs 的程式碼在 stdio 版本和 HTTP 版本中完全相同，
    //   傳輸模式的切換只發生在 Program.cs 的設定層，工具邏輯不受影響。
    // ─────────────────────────────────────────────────────────────────────
    .WithToolsFromAssembly();

// ============================================================================
// 建置 WebApplication 實例
// ============================================================================
var app = builder.Build();

// ============================================================================
// 掛載 MCP HTTP 端點
// ============================================================================
//
// ★ stdio 版本不需要此步驟，這是 HTTP 版本特有的必要設定 ★
//
// MapMcp()
// ─────────────────────────────────────────────────────────────────────────
// 將 MCP Server 的 HTTP 路由正式掛載到 ASP.NET Core 的路由系統。
// 若不呼叫此方法，Server 雖然會啟動，但不會回應任何 MCP 請求（HTTP 404）。
//
// 此方法會同時掛載兩類端點：
//
//   1. Streamable HTTP（MCP 2025-03-26 規範）
//      - HTTP POST / → Client 送出 JSON-RPC 請求並接收回應串流
//
//   2. 舊版 SSE 端點（MCP 2024-11-05 規範，向下相容）
//      - GET  /sse     → Client 建立 SSE 長連線接收推播
//      - POST /message → Client 送出 JSON-RPC 請求
//
// 【預設路徑說明】
//   MapMcp() 的簽章為 MapMcp(string pattern = "")，
//   預設 pattern 為空字串，因此端點掛載於根路徑「/」。
//
//   Client 端的 Endpoint 應設定為 http://localhost:5121/（根路徑），
//   而非 http://localhost:5121/mcp。
//
//   若需指定路徑前綴，可改寫為：
//     app.MapMcp("/mcp");
//   並同步修改 Client 端的 Endpoint 為 http://localhost:5121/mcp。
// ─────────────────────────────────────────────────────────────────────────
app.MapMcp();

// ============================================================================
// 啟動 WebApplication，開始持續監聽 HTTP 請求
// ============================================================================
//
// ★ 與 stdio 版本的差異：
//
//   stdio 版本：await builder.Build().RunAsync();
//     → Generic Host 阻塞主執行緒，等待 Client 子程序結束
//     → Server 生命週期由 Client 控制
//
//   HTTP 版本：app.Run();
//     → ASP.NET Core 啟動 Kestrel HTTP Server
//     → 持續監聽 http://localhost:5121（由 launchSettings.json 設定）
//     → Server 獨立運行，可同時服務多個 Client 連線
//     → 按 Ctrl+C 終止 Server
//
// ============================================================================
app.Run();
