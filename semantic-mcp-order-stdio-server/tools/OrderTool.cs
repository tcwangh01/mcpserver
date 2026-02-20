// ============================================================================
// OrderTool.cs — MCP 訂單查詢工具
// ============================================================================
//
// 【using 引用說明】
//
//   using McpOrderServer.Models;
//     ↑ 引用同專案內 model/ 資料夾的 OrderDto。
//       命名空間定義於 model/OrderDto.cs 的 `namespace McpOrderServer.Models;`。
//       沒有加這行，編譯器會找不到 OrderDto 型別（error CS0246）。
//
//   using ModelContextProtocol.Server;
//     ↑ 引用 MCP Server SDK（NuGet 套件），
//       提供 [McpServerToolType]、[McpServerTool] 等 Attribute，
//       讓本類別的方法能被 MCP 框架自動掃描並對外暴露為工具。
//
//   using System.ComponentModel;
//     ↑ 提供 [Description(...)] Attribute，
//       用來描述工具與參數的用途，AI Agent 會讀取這些說明來決定要呼叫哪個工具。
//
// ============================================================================
//
// 【Namespace 定義說明】
//
//   namespace McpOrderServer.Tools;
//
//   - 根命名空間 McpOrderServer 來自 .csproj 的 <RootNamespace>
//   - 本檔案位於 tools/ 資料夾，加上 .Tools 子層做區隔
//   - 同一個命名空間內的類別不需要 using，跨命名空間才需要 using
//
// ============================================================================
//
// 【外部 Console Client Agent 如何使用這些工具？】
//
//   Console Client 並不是直接引用本專案的 .dll 或 Namespace，
//   而是透過 MCP 協定（JSON-RPC over Stdio）與本 Server 溝通：
//
//   1. Client 啟動本 Server 作為子程序：
//        dotnet run --project ../semantic-mcp-order-server
//
//   2. Client 呼叫 mcpClient.ListToolsAsync() 取得工具清單，
//      Server 會回傳所有標記了 [McpServerTool] 的方法名稱與 Description。
//
//   3. AI Agent 根據 Description 判斷要呼叫哪個工具，
//      並透過 MCP 協定傳入參數、取得 JSON 格式的回傳結果。
//
//   4. 因此 Client 端不需要 using McpOrderServer.Tools，
//      工具的呼叫與回傳完全透過 MCP 協定的 JSON 訊息進行。
//
// ============================================================================

using McpOrderServer.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

// 命名空間宣告：本檔案所有類別都屬於 McpOrderServer.Tools
namespace McpOrderServer.Tools;

// ============================================================================
// [McpServerToolType] — 標記本類別為 MCP 工具容器
// ============================================================================
// Program.cs 呼叫 WithToolsFromAssembly() 時，
// MCP SDK 會掃描整個 Assembly，找出所有標記了此 Attribute 的類別，
// 並將其中標記了 [McpServerTool] 的方法自動註冊為可呼叫的工具。
// ============================================================================
[McpServerToolType]
public class OrderTool
{
    // ========================================================================
    // 模擬訂單資料（實務上應替換為資料庫查詢）
    // ========================================================================
    // OrderDto 型別來自 McpOrderServer.Models 命名空間（model/OrderDto.cs），
    // 透過頂端的 `using McpOrderServer.Models;` 引入。
    // ========================================================================
    private static readonly List<OrderDto> _orders =
    [
        new(1001, "王小美",  new(2025, 6, 1), 1299, "已出貨"),
        new(1002, "陳錢錢",  new(2025, 6, 3), 2599, "處理中"),
        new(1003, "阿土伯",  new(2025, 6, 5),  499, "已取消")
    ];

    // ========================================================================
    // [McpServerTool] — 標記此方法為 MCP 工具（對外暴露）
    // ========================================================================
    // Description("...") 的文字會透過 MCP 協定傳給 Client，
    // AI Agent 讀取此說明後，判斷在什麼情況下應該呼叫本工具。
    // ★ Description 建議用英文，以提升 AI 模型的理解準確度。
    // ========================================================================

    // 依訂單編號查詢單筆訂單
    [McpServerTool,
     Description("Query order data by order ID")]
    public OrderDto? GetOrderById(
        [Description("The order ID to query, e.g. 1001")] int orderId) =>
        _orders.FirstOrDefault(o => o.Id == orderId);

    // 依客戶姓名關鍵字模糊查詢（可查到多筆）
    [McpServerTool,
     Description("Query order list by customer name keyword")]
    public IEnumerable<OrderDto> SearchOrdersByCustomer(
        [Description("Customer name or keyword to search, e.g. 王小美")] string keyword) =>
        _orders.Where(o =>
            o.Customer.Contains(keyword, StringComparison.OrdinalIgnoreCase));
}
