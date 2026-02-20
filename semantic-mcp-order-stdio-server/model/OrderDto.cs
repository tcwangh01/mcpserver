// ============================================================================
// OrderDto.cs — 訂單資料傳輸物件（Data Transfer Object）
// ============================================================================
//
// 【Namespace 定義說明】
// 使用 C# 10+ 的「檔案範圍命名空間」語法（File-Scoped Namespace）：
//
//   namespace McpOrderServer.Models;
//
// 等同於傳統寫法：
//   namespace McpOrderServer.Models { ... }
//
// 命名空間規則：
//   - 根命名空間來自 .csproj 的 <RootNamespace>McpOrderServer</RootNamespace>
//   - 本檔案位於 model/ 資料夾，慣例上加上 .Models 子層
//   - 完整命名空間 = McpOrderServer.Models
//
// 【跨檔案引用方式】
// 其他 .cs 檔案若要使用 OrderDto，需加上：
//   using McpOrderServer.Models;
//
// 範例：tools/OrderTool.cs 頂端有：
//   using McpOrderServer.Models;   ← 引用本命名空間
// ============================================================================

namespace McpOrderServer.Models;

// ============================================================================
// OrderDto — 訂單資料記錄（Record）
// ============================================================================
// C# record 是不可變（immutable）的資料類別，適合用來傳遞資料。
// 使用 Primary Constructor 語法，編譯器自動產生屬性與相等比較邏輯。
//
// 【MCP 工具回傳型別】
// 當 MCP Tool 回傳 OrderDto 時，SDK 會自動將其序列化為 JSON 回傳給 Client，
// 因此 Client 端（Console Agent）不需要直接引用此型別，
// 只需解析收到的 JSON 字串即可。
// ============================================================================
public record OrderDto(
    int Id,              // 訂單編號，例如 1001
    string Customer,     // 客戶姓名，例如 "王小美"
    DateTime OrderDate,  // 下單日期
    int Total,           // 訂單金額（元）
    string Status        // 訂單狀態，例如 "已出貨"、"處理中"、"已取消"
);
