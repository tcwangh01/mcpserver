// ============================================================================
// OrderDto.cs — 訂單資料傳輸物件（Data Transfer Object）
// ============================================================================
//
// 【Namespace 定義說明】
//
//   namespace semantic_mcp_order_shttp_server.Models;
//
//   使用 C# 10+ 的「檔案範圍命名空間」語法（File-Scoped Namespace），
//   等同傳統寫法：namespace semantic_mcp_order_shttp_server.Models { ... }
//
//   命名空間規則：
//     - 根命名空間 semantic_mcp_order_shttp_server 來自 .csproj 的 <RootNamespace>
//     - 本檔案位於 model/ 資料夾，慣例加上 .Models 子層
//
//   跨檔案引用：
//     tools/OrderTool.cs 使用 `using semantic_mcp_order_shttp_server.Models;` 引入本型別。
//
// ============================================================================
//
// 【MCP 工具回傳型別與序列化說明】
//
//   當 MCP Tool 方法（如 GetOrderById）回傳 OrderDto 時：
//     1. MCP SDK 自動將 OrderDto 的所有公開屬性序列化為 JSON 字串
//     2. JSON 透過 HTTP 回應（Streamable HTTP）傳送給 Client
//     3. Client 端（Console Agent）解析 JSON 字串並呈現給 AI Agent
//     4. AI Agent 再將資訊整理成自然語言回覆給使用者
//
//   Client 不需直接引用 OrderDto 型別，只需處理收到的 JSON 字串。
//
//   序列化後 JSON 範例：
//     {
//       "id": 1001,
//       "customer": "王小美",
//       "orderDate": "2025-06-01T00:00:00",
//       "total": 1299,
//       "status": "已出貨"
//     }
//
// ============================================================================

namespace semantic_mcp_order_shttp_server.Models;

// ============================================================================
// OrderDto — 訂單資料記錄（C# Record）
// ============================================================================
//
// C# record（C# 9+）是不可變（immutable）的資料類別，特性：
//   - 使用 Primary Constructor 語法，自動產生對應的屬性（get-only）
//   - 編譯器自動產生 Equals / GetHashCode / ToString 實作
//   - 適合純資料傳遞，不包含業務邏輯
//   - 可用 `with` 語法建立修改後的新實例（非破壞性更新）
//
// ============================================================================
public record OrderDto(
    int Id,              // 訂單編號，例如 1001
    string Customer,     // 客戶姓名，例如 "王小美"
    DateTime OrderDate,  // 下單日期
    int Total,           // 訂單金額（元）
    string Status        // 訂單狀態，例如 "已出貨"、"處理中"、"已取消"
);
