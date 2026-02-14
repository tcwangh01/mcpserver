using System.ComponentModel;
using Microsoft.SemanticKernel;

// ============================================================================
// 企業服務 Plugin 定義
// ============================================================================
// 本檔案定義三個 Plugin 類別，提供企業內部資料的查詢功能。
// 每個類別中的方法都標記了 [KernelFunction] 與 [Description] 屬性：
//
// - [KernelFunction]：將方法註冊為 Semantic Kernel 可呼叫的函數，
//   Semantic Kernel 會自動將其轉換為 OpenAI Function Calling 的工具定義
// - [Description]：提供函數的自然語言描述，AI 模型會根據此描述
//   判斷何時該呼叫此函數（類似 OpenAI tool 的 description 欄位）
//
// 實際應用中，這些方法可以改為查詢資料庫、呼叫外部 API 或讀取設定檔，
// 本範例使用硬編碼的字串回傳值作為示範。
// ============================================================================

// ============================================================================
// HRPolicyService — 人資政策查詢服務
// ============================================================================
// 提供三個查詢功能，供 HRPolicyAgent 透過 Function Calling 呼叫：
// - GetLeavePolicy：查詢請假政策（病假、特休、事假）
// - GetBenefitPolicy：查詢員工福利（旅遊補助、健檢、午餐補助）
// - GetAttendancePolicy：查詢考勤規定（上班時間、彈性時間）
public class HRPolicyService
{
    /// <summary>
    /// 查詢請假政策，包含病假、特休、事假的天數與規定
    /// </summary>
    /// <param name="leaveType">請假類型（如：病假、特休、事假）</param>
    /// <returns>請假政策的詳細說明</returns>
    [KernelFunction, Description("Get detailed leave policy information")]
    public string GetLeavePolicy([Description("Type of leave")] string leaveType)
    {
        return @"請假政策說明：\n" +
               "1. 病假：每年提供30日，未使用部分得轉為特休。\n" +
               "2. 特休：依年資計算，滿一年提供7日，滿兩年提供10日。\n" +
               "3. 事假：每年最多7日，不扣薪但需主管核准。";
    }

    /// <summary>
    /// 查詢員工福利政策，包含旅遊補助、健康檢查、午餐補助
    /// </summary>
    /// <param name="benefitName">福利名稱（如：旅遊補助、健檢）</param>
    /// <returns>福利政策的詳細說明</returns>
    [KernelFunction, Description("Query specific benefit policy")]
    public string GetBenefitPolicy([Description("Benefit name")] string benefitName)
    {
        return @"福利政策說明：\n" +
               "1. 旅遊補助：每位員工每年可申請一次旅遊補助上限 NT$5,000。\n" +
               "2. 健康檢查：公司每兩年提供一次免費員工健康檢查。\n" +
               "3. 午餐補助：每日午餐補助 NT$100，自動匯入薪資帳戶。";
    }

    /// <summary>
    /// 查詢考勤規定，包含上班時間與彈性工時
    /// </summary>
    /// <returns>考勤規定的說明</returns>
    [KernelFunction, Description("Query attendance policy description")]
    public string GetAttendancePolicy()
    {
        return "上班時間為上午9:00至下午6:00，彈性時間為8:00至10:00，需每日完成8小時工作。";
    }
}

// ============================================================================
// ITSupportService — IT 支援服務
// ============================================================================
// 提供三個查詢功能，供 ITSupportAgent 透過 Function Calling 呼叫：
// - GetVpnSetup：查詢 VPN 設定步驟
// - GetAccountPolicy：查詢系統帳號申請流程
// - GetBackupPolicy：查詢資料備份政策
public class ITSupportService
{
    /// <summary>
    /// 查詢 VPN 設定教學，提供完整的安裝與連線步驟
    /// </summary>
    /// <returns>VPN 設定步驟說明</returns>
    [KernelFunction, Description("Get VPN setup tutorial")]
    public string GetVpnSetup()
    {
        return "VPN 設定步驟：1. 下載公司專用 VPN 軟體；2. 安裝後輸入員工帳號密碼；3. 連線至內部網段 vpn.company.com。";
    }

    /// <summary>
    /// 查詢系統帳號開通流程，根據指定的系統名稱回傳申請說明
    /// </summary>
    /// <param name="systemName">要申請帳號的系統名稱</param>
    /// <returns>帳號申請流程說明</returns>
    [KernelFunction, Description("Query system account activation process")]
    public string GetAccountPolicy([Description("System name")] string systemName)
    {
        return $"系統「{systemName}」的帳號申請請透過 IT 申請單，由主管核准後開通，預計作業時間為1-2工作天。";
    }

    /// <summary>
    /// 查詢資料備份政策，包含備份頻率與保留期限
    /// </summary>
    /// <returns>備份政策說明</returns>
    [KernelFunction, Description("Query backup policy")]
    public string GetBackupPolicy()
    {
        return "所有桌機與筆電會自動每日備份至雲端，保留期限為30日。如需還原請聯繫 IT 支援。";
    }
}

// ============================================================================
// ComplianceService — 合規與資安查詢服務
// ============================================================================
// 提供三個查詢功能，供 ComplianceAgent 透過 Function Calling 呼叫：
// - GetContractPolicy：查詢合約條款（支援依條款名稱查詢）
// - GetDataSecurityPolicy：查詢資訊安全政策
// - GetGovernancePolicy：查詢公司治理政策
public class ComplianceService
{
    /// <summary>
    /// 查詢合約條款內容，根據條款名稱回傳對應的合規規定
    /// 支援的條款：保密條款、競業禁止
    /// </summary>
    /// <param name="clauseName">合約條款名稱（如：保密條款、競業禁止）</param>
    /// <returns>該條款的詳細說明，若找不到則回傳提示訊息</returns>
    [KernelFunction, Description("Query specific contract compliance clause")]
    public string GetContractPolicy([Description("Clause name")] string clauseName)
    {
        // 使用 switch expression 根據條款名稱回傳對應內容
        return clauseName.ToLower() switch
        {
            "保密條款" => "所有員工必須簽署 NDA，嚴禁洩漏公司資料予第三方。",
            "競業禁止" => "離職後六個月內不得任職於競爭公司，除非經公司書面同意。",
            _ => "找不到指定的合約條款。"
        };
    }

    /// <summary>
    /// 查詢公司資訊安全政策，包含資料儲存、傳輸加密與權限稽核規定
    /// </summary>
    /// <returns>資安政策說明</returns>
    [KernelFunction, Description("Query company data security policy")]
    public string GetDataSecurityPolicy()
    {
        return "公司所有機密文件必須儲存在加密磁碟區，傳輸時使用 TLS 加密，並定期進行權限稽核。";
    }

    /// <summary>
    /// 查詢公司治理政策，包含董事會運作與內部稽核機制
    /// </summary>
    /// <returns>公司治理政策說明</returns>
    [KernelFunction, Description("Query company governance policy")]
    public string GetGovernancePolicy()
    {
        return "公司治理依據董事會決議與公司章程執行，設有內部稽核與合規部門監督運作。";
    }
}
