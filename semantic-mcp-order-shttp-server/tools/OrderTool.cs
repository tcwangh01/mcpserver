// ============================================================================
// OrderTool.cs — MCP 訂單查詢工具
// ============================================================================
//
// 【using 引用說明】
//
//   using semantic_mcp_order_shttp_server.Models;
//     ↑ 引用同專案內 model/ 資料夾的 OrderDto。
//       沒有加這行，編譯器會找不到 OrderDto 型別（error CS0246）。
//
//   using ModelContextProtocol.Server;
//     ↑ 引用 MCP Server SDK（NuGet：ModelContextProtocol），
//       提供 [McpServerToolType]、[McpServerTool] 等 Attribute，
//       讓本類別的方法能被 MCP 框架自動掃描並對外暴露為工具。
//
//   using System.ComponentModel;
//     ↑ 提供 [Description(...)] Attribute，
//       用來描述工具與參數的用途，
//       AI Agent 在決定呼叫哪個工具時會讀取這些說明文字。
//
// ============================================================================
//
// 【Namespace 定義說明】
//
//   namespace semantic_mcp_order_shttp_server.Tools;
//
//   - 根命名空間 semantic_mcp_order_shttp_server 來自 .csproj 的 <RootNamespace>
//   - 本檔案位於 tools/ 資料夾，加上 .Tools 子層做區隔
//   - Program.cs 的 `using semantic_mcp_order_shttp_server.Tools;` 引用本命名空間，
//     讓 IDE 能提供程式碼提示（WithToolsFromAssembly 本身不需要這行）
//
// ============================================================================
//
// ★ 工具定義與傳輸層完全無關 ★
//
//   本檔案的程式碼與 stdio 版本（semantic-mcp-order-stdio-server）
//   的 OrderTool.cs 內容完全相同（僅命名空間不同）。
//
//   MCP 的設計原則是「工具邏輯」與「傳輸層」分離：
//
//     傳輸層（Program.cs 設定）：
//       stdio → .WithStdioServerTransport()
//       HTTP  → .WithHttpTransport() + app.MapMcp()
//
//     工具層（本檔案）：
//       兩種傳輸模式都使用相同的 [McpServerToolType] / [McpServerTool] 標記，
//       工具邏輯不需因傳輸方式的改變而修改。
//
// ============================================================================
//
// 【Client 如何取得並使用這些工具？】
//
//   1. Client 啟動後呼叫：
//        var tools = await mcpClient.ListToolsAsync();
//      Server 回傳所有標記了 [McpServerTool] 的方法名稱、Description 及參數結構。
//
//   2. Client 將工具轉換為 Semantic Kernel Function 並注入 Kernel：
//        kernel.Plugins.AddFromFunctions("McpTools", tools.Select(t => t.AsKernelFunction()));
//
//   3. AI Agent（ChatCompletionAgent）根據對話內容，
//      自動判斷是否需要呼叫工具，並透過 MCP 協定傳入參數、取得 JSON 回傳結果。
//
//   Client 不需直接引用本專案的 .dll 或 Namespace，
//   所有互動都透過 MCP 協定（HTTP + JSON-RPC）進行。
//
// ============================================================================

using semantic_mcp_order_shttp_server.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

// 命名空間宣告：本檔案所有類別都屬於 semantic_mcp_order_shttp_server.Tools
namespace semantic_mcp_order_shttp_server.Tools;

// ============================================================================
// [McpServerToolType] — 標記本類別為 MCP 工具容器
// ============================================================================
// Program.cs 呼叫 WithToolsFromAssembly() 時，
// MCP SDK 會掃描整個 Assembly，找出所有標記了此 Attribute 的類別，
// 並將其中標記了 [McpServerTool] 的方法自動註冊為可呼叫的 MCP 工具。
//
// 新增工具時，只需建立新的方法並加上 [McpServerTool] 即可，
// 不需要修改 Program.cs 或任何其他設定。
// ============================================================================
[McpServerToolType]
public class OrderTool
{
    // ========================================================================
    // 模擬訂單資料（實務上應替換為資料庫查詢）
    // ========================================================================
    // static readonly：所有請求共用同一份資料（無狀態服務的模擬）
    // ========================================================================
    private static readonly List<OrderDto> _orders =
    [
        new(1001, "王小美",  new(2025, 6, 1), 1299, "已出貨"),
        new(1002, "陳錢錢",  new(2025, 6, 3), 2599, "處理中"),
        new(1003, "阿土伯",  new(2025, 6, 5),  499, "已取消")
    ];

    // ========================================================================
    // [McpServerTool] — 標記此方法為 MCP 工具（對外暴露給 Client）
    // ========================================================================
    // [Description("...")] 的文字會：
    //   1. 透過 MCP 協定傳給 Client（ListToolsAsync 的回傳結果）
    //   2. 被 AI Agent 讀取，用來判斷在什麼情況下應該呼叫本工具
    //   3. 套用到方法本身（工具說明）和每個參數（參數說明）
    //
    // ★ Description 建議使用英文，以提升 AI 模型的理解準確度。
    // ========================================================================

    // ────────────────────────────────────────────────────────────────────────
    // 工具 1：依訂單編號查詢單筆訂單
    // ────────────────────────────────────────────────────────────────────────
    // 回傳型別 OrderDto?（可為 null）：
    //   MCP SDK 會自動將 OrderDto 序列化為 JSON 字串回傳給 Client。
    //   若查無結果則回傳 null，Client 端收到空回應。
    // ────────────────────────────────────────────────────────────────────────
    [McpServerTool,
     Description("Query order data by order ID")]
    public OrderDto? GetOrderById(
        [Description("The order ID to query, e.g. 1001")] int orderId) =>
        _orders.FirstOrDefault(o => o.Id == orderId);

    // ────────────────────────────────────────────────────────────────────────
    // 工具 2：依客戶姓名關鍵字模糊查詢（可回傳多筆）
    // ────────────────────────────────────────────────────────────────────────
    // 回傳型別 IEnumerable<OrderDto>：
    //   MCP SDK 會自動將集合序列化為 JSON 陣列回傳給 Client。
    //   StringComparison.OrdinalIgnoreCase：不區分英文大小寫的比對。
    // ────────────────────────────────────────────────────────────────────────
    [McpServerTool,
     Description("Query order list by customer name keyword")]
    public IEnumerable<OrderDto> SearchOrdersByCustomer(
        [Description("Customer name or keyword to search, e.g. 王小美")] string keyword) =>
        _orders.Where(o =>
            o.Customer.Contains(keyword, StringComparison.OrdinalIgnoreCase));
}
