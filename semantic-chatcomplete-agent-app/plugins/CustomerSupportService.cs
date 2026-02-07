/*
 * ============================================================================
 * CustomerSupportService - 客服支援 Plugin
 * ============================================================================
 *
 * 這是一個 Semantic Kernel Plugin 類別，提供客服相關的業務邏輯函數。
 * 當 AI Agent 需要查詢訂單或產品資訊時，會自動呼叫這些函數。
 *
 * Plugin 運作原理：
 * 1. 使用 [KernelFunction] 屬性標記可被 AI 呼叫的方法
 * 2. 使用 [Description] 屬性描述函數用途，幫助 AI 理解何時該呼叫
 * 3. 參數也使用 [Description] 描述，讓 AI 知道需要提供什麼資訊
 *
 * 函數呼叫流程：
 * ┌─────────────┐     ┌─────────────────┐     ┌──────────────────┐
 * │   使用者    │ --> │  ChatCompletion │ --> │ CustomerSupport  │
 * │ "查詢訂單"  │     │     Agent       │     │    Service       │
 * └─────────────┘     └─────────────────┘     └──────────────────┘
 *                            │                        │
 *                            │  1. AI 分析使用者意圖   │
 *                            │  2. 決定呼叫 GetOrder  │
 *                            │     Status 函數        │
 *                            │  3. 傳入訂單編號參數    │
 *                            │ ---------------------->│
 *                            │                        │
 *                            │  4. 函數返回訂單狀態    │
 *                            │ <----------------------│
 *                            │                        │
 *                            │  5. AI 整合結果回覆    │
 *                            v                        │
 *                     ┌─────────────┐                 │
 *                     │   使用者    │                 │
 *                     │  收到回覆   │                 │
 *                     └─────────────┘                 │
 *
 * ============================================================================
 */

using System.ComponentModel;
using Microsoft.SemanticKernel;

/// <summary>
/// 客服支援服務 Plugin
/// 提供訂單查詢和產品資訊查詢功能
/// </summary>
public class CustomerSupportService
{
    /// <summary>
    /// 查詢訂單狀態
    /// </summary>
    /// <param name="orderId">訂單編號</param>
    /// <returns>訂單狀態描述</returns>
    /// <remarks>
    /// 這個函數會被 AI Agent 在使用者詢問訂單狀態時自動呼叫。
    ///
    /// AI 呼叫時機：
    /// - 使用者說「我想查訂單」
    /// - 使用者說「訂單 12345 的狀態是什麼？」
    /// - 使用者說「我的訂單寄出了嗎？」
    ///
    /// 實際應用中，這裡應該：
    /// 1. 連接資料庫查詢訂單
    /// 2. 呼叫訂單管理 API
    /// 3. 查詢物流追蹤系統
    /// </remarks>
    [KernelFunction]  // 標記此方法可被 AI 呼叫
    [Description("Get the status of an order.")]  // 描述函數用途，幫助 AI 決定何時呼叫
    public static string GetOrderStatus(
        [Description("Order ID")] string orderId)  // 描述參數，讓 AI 知道需要提供訂單編號
    {
        // 模擬訂單查詢
        // 實際情況下，這裡可能會：
        // - 查詢資料庫：SELECT status FROM orders WHERE order_id = @orderId
        // - 呼叫外部 API：await httpClient.GetAsync($"/api/orders/{orderId}")
        // - 查詢快取：cache.Get<Order>($"order:{orderId}")
        return $"您查詢的訂單 {orderId} 的狀態為 : 已出貨";
    }

    /// <summary>
    /// 查詢產品資訊
    /// </summary>
    /// <param name="productName">產品名稱</param>
    /// <returns>產品詳細資訊</returns>
    /// <remarks>
    /// 這個函數會被 AI Agent 在使用者詢問產品資訊時自動呼叫。
    ///
    /// AI 呼叫時機：
    /// - 使用者說「這個產品多少錢？」
    /// - 使用者說「有沒有 iPhone 的資訊？」
    /// - 使用者說「我想了解某某產品」
    ///
    /// 實際應用中，這裡應該：
    /// 1. 查詢產品資料庫
    /// 2. 呼叫產品目錄 API
    /// 3. 整合價格、庫存、規格等資訊
    /// </remarks>
    [KernelFunction]  // 標記此方法可被 AI 呼叫
    [Description("Get the product information")]  // 描述函數用途
    public static string GetProductInfo(
        [Description("Product Name")] string productName)  // 描述參數
    {
        // 模擬產品查詢
        // 實際情況下，這裡可能會：
        // - 查詢資料庫：SELECT * FROM products WHERE name LIKE @productName
        // - 呼叫產品 API：await httpClient.GetAsync($"/api/products?name={productName}")
        // - 整合多個來源的資訊（價格、庫存、評價等）
        return $"{productName} 是我們長年熱銷商品，價格為 NT $599";
    }
}

/*
 * ============================================================================
 * 擴展建議
 * ============================================================================
 *
 * 如需擴展此 Plugin，可以考慮加入以下函數：
 *
 * 1. 取消訂單
 *    [KernelFunction]
 *    [Description("Cancel an order")]
 *    public static string CancelOrder(string orderId) { ... }
 *
 * 2. 查詢物流
 *    [KernelFunction]
 *    [Description("Track shipping status")]
 *    public static string TrackShipment(string trackingNumber) { ... }
 *
 * 3. 查詢庫存
 *    [KernelFunction]
 *    [Description("Check product availability")]
 *    public static string CheckStock(string productId) { ... }
 *
 * 4. 提交退款申請
 *    [KernelFunction]
 *    [Description("Request a refund")]
 *    public static string RequestRefund(string orderId, string reason) { ... }
 *
 * ============================================================================
 */
