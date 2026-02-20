// ============================================================================
// Program.cs — MCP Order Server 進入點
// ============================================================================
//
// 【using 引用說明】
//
//   using McpOrderServer.Tools;
//     ↑ 引用同專案內 tools/ 資料夾的 OrderTool。
//       雖然 WithToolsFromAssembly() 會自動掃描，
//       但明確 using 可讓 IDE 正確解析型別並提供程式碼提示。
//
//   using Microsoft.Extensions.DependencyInjection;
//     ↑ 提供 .AddMcpServer() 等擴充方法（IServiceCollection 的 DI 容器）。
//
//   using Microsoft.Extensions.Hosting;
//     ↑ 提供 Host.CreateApplicationBuilder()、builder.Build().RunAsync()
//       等 .NET Generic Host 生命週期管理功能。
//
//   using Microsoft.Extensions.Logging;
//     ↑ 提供日誌介面（ILogger），Host 內部會使用。
//
//   using ModelContextProtocol.Server;
//     ↑ 提供 .AddMcpServer()、.WithStdioServerTransport()、
//       .WithToolsFromAssembly() 等 MCP Server 核心 API。
//
// ============================================================================
//
// 【整體架構說明】
//
//   本 Server 以 Stdio（標準輸入/輸出）作為傳輸層，
//   外部 Client 透過子程序方式啟動本程式，
//   並使用 MCP 協定（JSON-RPC over Stdio）進行工具的呼叫與結果回傳。
//
//   呼叫流程：
//     Console Client
//       → 子程序啟動 dotnet run --project ../semantic-mcp-order-server
//       → MCP JSON-RPC over Stdio
//       → MCP SDK 路由到對應的 [McpServerTool] 方法
//       → 回傳 JSON 結果給 Client
//
// ============================================================================

using McpOrderServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

Console.WriteLine("Hello, MCP Server!");

// ============================================================================
// 建立 .NET Generic Host
// ============================================================================
// Generic Host 負責管理應用程式的生命週期、DI 容器、設定與日誌。
// CreateApplicationBuilder(args) 會讀取命令列參數與環境設定。
// ============================================================================
var builder = Host.CreateApplicationBuilder(args);

// ============================================================================
// 註冊 MCP Server 服務
// ============================================================================
builder.Services
    // AddMcpServer()：向 DI 容器註冊 MCP Server 所需的核心服務
    .AddMcpServer()

    // WithStdioServerTransport()：採用標準輸入/輸出（Stdio）作為傳輸層。
    // Client 啟動本程式作為子程序後，透過 Stdin/Stdout 進行 JSON-RPC 通訊。
    .WithStdioServerTransport()

    // WithToolsFromAssembly()：自動掃描目前 Assembly 中
    // 所有標記了 [McpServerToolType] 的類別（如 OrderTool），
    // 並將其中標記了 [McpServerTool] 的方法註冊為可呼叫的 MCP 工具。
    // 新增工具時只需建立新類別並加上 Attribute，不需修改此處。
    .WithToolsFromAssembly();

// ============================================================================
// 啟動 Host，開始監聽 MCP 請求
// ============================================================================
// RunAsync() 會阻塞主執行緒直到程式被終止（例如 Client 關閉子程序）。
// ============================================================================
await builder.Build().RunAsync();
