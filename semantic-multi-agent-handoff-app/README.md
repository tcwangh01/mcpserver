# Semantic Kernel Multi-Agent Handoff 企業客服系統示範

這是一個展示 **Semantic Kernel Agents** 框架結合 **HandoffOrchestration** 多代理人轉交機制的 .NET 應用程式，實現一個具備問題分流與專責回答功能的企業 AI 客服系統。

## 為什麼使用 HandoffOrchestration？

### 問題：單一 Agent 的局限性

在先前的 `semantic-chatcomplete-agent-app` 中，我們使用單一 `ChatCompletionAgent` 處理所有問題。但在企業場景中，不同類型的問題需要不同的專業知識與處理邏輯：

```csharp
// 單一 Agent - 所有問題都由同一個 Agent 處理，難以維護
ChatCompletionAgent agent = new()
{
    Name = "SupportAgent",
    Instructions = "你要處理人資、IT、合規所有問題...",  // Instructions 越來越長
    Kernel = kernel
};
```

### 解決方案：HandoffOrchestration 多代理人轉交

HandoffOrchestration 讓多個專責代理人協作，透過自動注入的 `transfer` 工具實現智慧分流：

```csharp
// 多 Agent - 各司其職，自動轉交
HandoffOrchestration orchestration =
    new(OrchestrationHandoffs
            .StartWith(triageAgent)          // 所有問題先由分流 Agent 接收
            .Add(triageAgent, hrAgent, "...")      // 人資問題轉交給 HR Agent
            .Add(triageAgent, itAgent, "...")      // IT 問題轉交給 IT Agent
            .Add(triageAgent, complianceAgent, "..."), // 合規問題轉交給 Compliance Agent
                triageAgent, hrAgent, itAgent, complianceAgent);
```

## HandoffOrchestration 的核心優勢

| 優勢 | 說明 |
|------|------|
| **職責分離** | 每個 Agent 專注於特定領域，Instructions 簡潔明確 |
| **自動轉交** | 自動注入 `transfer_to_XXX` 工具，AI 自行判斷何時轉交 |
| **結果回傳** | 自動注入 `end_task_with_summary` 工具，統一收集最終回覆 |
| **可擴展性** | 新增領域只需新增 Agent 與轉交規則，不影響其他 Agent |
| **Plugin 整合** | 每個 Agent 都能使用 Plugin 查詢企業資料 |

## 與 chatcomplete-agent-app 的差異

| 項目 | chatcomplete-agent-app | multi-agent-handoff-app (本專案) |
|------|------------------------|----------------------------------|
| **Agent 數量** | 1 個 | 4 個（1 分流 + 3 專責） |
| **協調機制** | 無（單一 Agent） | HandoffOrchestration 自動轉交 |
| **對話管理** | ChatHistoryAgentThread | InProcessRuntime + OrchestrationResult |
| **回應方式** | InvokeStreamingAsync 串流 | GetValueAsync 等待最終結果 |
| **Plugin 用途** | 客服查詢（訂單、產品） | 企業服務（人資、IT、合規） |
| **擴展性** | 需修改單一 Agent 的 Instructions | 新增 Agent 即可擴展 |

## 功能特性

- **HandoffOrchestration**：多代理人自動轉交協調機制
- **問題分流**：TriageAgent 自動判斷問題類型並轉交
- **專責處理**：三個專責 Agent 各自負責不同領域
- **Plugin 自動函數調用**：AI 自動呼叫企業服務函數查詢資料
- **企業服務功能**：
  - 人資查詢（請假政策、員工福利、考勤規定）
  - IT 支援（VPN 設定、帳號申請、備份政策）
  - 合規諮詢（合約條款、資安政策、公司治理）

## 系統架構

```
┌─────────────────────────────────────────────────────────────┐
│                      使用者介面 (Console)                    │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                   HandoffOrchestration                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              TriageAgent（分流代理人）                │   │
│  │  根據問題類型呼叫 transfer 工具自動轉交              │   │
│  └──────────┬──────────────┬──────────────┬─────────────┘   │
│             ↓              ↓              ↓                 │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐    │
│  │ HRPolicyAgent│ │ITSupportAgent│ │ ComplianceAgent   │    │
│  │  (人資專員)   │ │  (IT 支援)   │ │  (法遵顧問)      │    │
│  └──────┬───────┘ └──────┬───────┘ └────────┬─────────┘    │
│         ↓                ↓                  ↓               │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐    │
│  │HRPolicyServ. │ │ITSupportServ.│ │ComplianceService  │    │
│  │-GetLeavePol. │ │-GetVpnSetup  │ │-GetContractPol.   │    │
│  │-GetBenefitP. │ │-GetAccountP. │ │-GetDataSecurityP. │    │
│  │-GetAttendanc.│ │-GetBackupPol.│ │-GetGovernancePol. │    │
│  └──────────────┘ └──────────────┘ └──────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│              InProcessRuntime（代理人執行環境）               │
│              訊息傳遞與排程、記憶體內佇列通訊                │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                    OpenAI GPT-4o API                        │
└─────────────────────────────────────────────────────────────┘
```

## 執行流程

```
使用者輸入：「公司特休假怎麼算？」
        │
        ▼
┌───────────────────────────────────────────┐
│  TriageAgent 接收問題                      │
│  判斷：這是人資相關問題                     │
│  呼叫：transfer_to_HRPolicyAgent          │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  HRPolicyAgent 接收轉交                   │
│  呼叫：HRPolicyService.GetLeavePolicy()  │
│  取得請假政策資料                          │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  HRPolicyAgent 整理回覆                   │
│  呼叫：end_task_with_summary              │
│  將完整回覆回傳給 orchestration            │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  主程式透過 GetValueAsync() 取得結果       │
│  顯示給使用者                              │
└───────────────────────────────────────────┘
```

## HandoffOrchestration 自動注入的工具

HandoffOrchestration 會自動為每個代理人注入 `HandoffPlugin`，包含以下工具：

| 工具名稱 | 注入對象 | 說明 |
|---------|---------|------|
| `transfer_to_HRPolicyAgent` | TriageAgent | 將對話轉交給人資代理人 |
| `transfer_to_ITSupportAgent` | TriageAgent | 將對話轉交給 IT 代理人 |
| `transfer_to_ComplianceAgent` | TriageAgent | 將對話轉交給合規代理人 |
| `end_task_with_summary` | 所有 Agent | 結束任務並回傳最終結果 |

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- OpenAI API Key

## 專案結構

```
semantic-multi-agent-handoff-app/
├── Program.cs                                          # 主程式（Agent 定義、Orchestration 設定、互動迴圈）
├── plugins/
│   └── EnterpriseAssistantService.cs                   # 企業服務 Plugin（HR、IT、合規）
├── semantic-multi-agent-handoff-app.csproj              # 專案設定
└── README.md                                           # 本文件
```

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-multi-agent-handoff-app
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel --version 1.70.0
dotnet add package Microsoft.SemanticKernel.Agents.Core --version 1.70.0
dotnet add package Microsoft.SemanticKernel.Agents.Orchestration --version 1.70.0-preview
dotnet add package Microsoft.SemanticKernel.Agents.Runtime.Core --version 1.70.0-preview
dotnet add package Microsoft.SemanticKernel.Agents.Runtime.InProcess --version 1.70.0-preview
```

### 3. 設定環境變數

**macOS/Linux：**
```bash
export OPENAI_API_KEY="your-api-key-here"
```

**Windows PowerShell：**
```powershell
$env:OPENAI_API_KEY="your-api-key-here"
```

### 4. 抑制實驗性 API 警告

由於 `HandoffOrchestration` 目前標記為實驗性 API（`SKEXP0110`），需在 `.csproj` 中加入：

```xml
<PropertyGroup>
    <NoWarn>$(NoWarn);SKEXP0110</NoWarn>
</PropertyGroup>
```

## 執行專案

```bash
# 編譯專案
dotnet build

# 執行專案
dotnet run
```

## 執行範例

```
=== Semantic Kernel + Multi-Agent 示範 ===
這個應用程式會依據使用者提問類型，分派給不同的Agent負責回覆
這個應用程式可查詢公司內部的人資問題．法規問題與 IT 問題
試試看問「公司的特休怎麼算？」，「VPN 怎麼設？」以及問問「我剛問了哪些問題？」
輸入 'exit' 或 'quit' 離開程式
================================================

您: 公司特休假怎麼算？
AI > 根據公司的請假政策，特休假依年資計算：
1. 滿一年提供7日
2. 滿兩年提供10日
此外，病假每年提供30日，未使用部分得轉為特休；事假每年最多7日，不扣薪但需主管核准。


您: VPN 怎麼設定？
AI > VPN 設定步驟如下：
1. 下載公司專用 VPN 軟體
2. 安裝後輸入員工帳號密碼
3. 連線至內部網段 vpn.company.com


您: 公司的保密條款是什麼？
AI > 根據公司合約規定，所有員工必須簽署 NDA（保密協議），嚴禁洩漏公司資料予第三方。
```

## 核心程式碼說明

### 1. 建立 Kernel 與註冊 Plugin

```csharp
// 建立 Kernel
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId: "gpt-4o", apiKey: apiKey)
    .Build();

// 註冊三個企業服務 Plugin
kernel.Plugins.AddFromType<HRPolicyService>();
kernel.Plugins.AddFromType<ITSupportService>();
kernel.Plugins.AddFromType<ComplianceService>();
```

### 2. 建立多個 ChatCompletionAgent

```csharp
// 分流代理人 - 負責判斷問題類型並轉交
var triageAgent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "TriageAgent",
    Description = "問題分流助理",
    Instructions = @"對使用者的提問進行分類..."
};

// 專責代理人 - 負責回答特定領域問題
var hrAgent = new ChatCompletionAgent
{
    Kernel = kernel,
    Name = "HRPolicyAgent",
    Description = "負責回應與人資制度相關的問題",
    Instructions = """
        你是公司的人資專員...
        回答完畢後，務必呼叫 end_task_with_summary 工具，將你的完整回覆作為摘要傳回。
    """
};
```

### 3. 設定 HandoffOrchestration 轉交規則

```csharp
// 定義轉交規則：TriageAgent 可以轉交給三個專責 Agent
HandoffOrchestration orchestration =
    new(OrchestrationHandoffs
            .StartWith(triageAgent)
            .Add(triageAgent, hrAgent, "Transfer to this agent if the issue is about leave, salary, HR policy, or attendance.")
            .Add(triageAgent, itAgent, "Transfer to this agent if the issue is about IT, account, VPN, password, or device.")
            .Add(triageAgent, complianceAgent, "Transfer to this agent if the issue is about compliance, security, or internal company policy."),
                triageAgent, hrAgent, itAgent, complianceAgent);
```

### 4. 執行 Orchestration 並取得結果

```csharp
// 啟動 Runtime
await runtime.StartAsync();

// 將使用者輸入送入 Orchestration
var result = await orchestration.InvokeAsync(input, runtime);

// 等待最終結果（必須使用 await）
var response = await result.GetValueAsync(TimeSpan.FromSeconds(300));
Console.Write(response);

// 等待 Runtime 完成所有待處理訊息
await runtime.RunUntilIdleAsync();
```

### 5. Plugin 函數定義

```csharp
public class HRPolicyService
{
    [KernelFunction, Description("Get detailed leave policy information")]
    public string GetLeavePolicy([Description("Type of leave")] string leaveType)
    {
        return @"請假政策說明：...";
    }
}

public class ITSupportService
{
    [KernelFunction, Description("Get VPN setup tutorial")]
    public string GetVpnSetup()
    {
        return "VPN 設定步驟：...";
    }
}
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.70.0 | 核心框架 |
| Microsoft.SemanticKernel.Agents.Core | 1.70.0 | Agent 框架 |
| Microsoft.SemanticKernel.Agents.Orchestration | 1.70.0-preview | Orchestration 協調機制 |
| Microsoft.SemanticKernel.Agents.Runtime.Core | 1.70.0-preview | Agent Runtime 核心 |
| Microsoft.SemanticKernel.Agents.Runtime.InProcess | 1.70.0-preview | 行程內 Runtime 實作 |

## 關鍵概念說明

### OrchestrationHandoffs

定義代理人之間的轉交規則，`Add(source, target, description)` 中的 `description` 會被注入到 `transfer_to_XXX` 工具的說明中，引導 AI 判斷何時該轉交。

### InProcessRuntime

提供代理人之間的訊息傳遞與排程機制，所有代理人在同一個行程內運行，透過記憶體中的訊息佇列通訊。

### end_task_with_summary

由 HandoffOrchestration 自動注入的工具。專責 Agent 回答完畢後**必須**呼叫此工具，將最終回覆回傳給 orchestration，否則 `GetValueAsync()` 將無法取得結果。

### SKEXP0110 警告

HandoffOrchestration 相關的 API 目前標記為實驗性（Experimental），需在 `.csproj` 中加入 `<NoWarn>$(NoWarn);SKEXP0110</NoWarn>` 來抑制編譯警告。

## 常見問題

### Q: 為什麼 AI 沒有回應（AI > 後面是空白）？

**A: 可能有兩個原因：**

1. **缺少 `await`**：`GetValueAsync()` 是非同步方法，必須使用 `await`，否則 `response` 是 `Task<string>` 物件而非字串。
2. **Agent 未呼叫 `end_task_with_summary`**：專責 Agent 的 Instructions 中需要明確指示「回答完畢後呼叫 end_task_with_summary 工具」，否則結果無法回傳。

### Q: TriageAgent 沒有進行轉交怎麼辦？

**A: 確認 `OrchestrationHandoffs.Add()` 的 description 足夠清晰。** 若使用不帶 description 的 `.Add(source, targets...)` 重載，AI 不知道何時該轉交。建議使用帶 description 的版本，明確描述每個轉交的條件。

### Q: 可以讓專責 Agent 之間互相轉交嗎？

**A: 可以。** 只需在 `OrchestrationHandoffs` 中新增對應的轉交規則，例如：

```csharp
.Add(hrAgent, itAgent, "Transfer if the question involves IT systems.")
```

## 注意事項

1. **API Key 安全**：請勿將 API Key 提交到版本控制系統
2. **Token 限制**：每次 Orchestration 呼叫涉及多個 Agent，Token 消耗較高
3. **成本考量**：GPT-4o 的 API 調用有費用，多 Agent 模式會產生多次 API 呼叫
4. **Preview 套件**：Orchestration 相關套件目前為預覽版本，API 可能會變更

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** - 學習 ChatHistory
4. **semantic-kernel-multi-function-app** - 多 Plugin + Filter
5. **semantic-chatcomplete-agent-app** - 學習 ChatCompletionAgent
6. **semantic-aiassistant-agent-app** - 學習 OpenAIAssistantAgent
7. **semantic-azure-aiagent-app** - 學習 AzureAIAgent
8. **semantic-multi-agent-handoff-app** - Multi-Agent Handoff 協作（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [Agent Orchestration 文檔](https://learn.microsoft.com/semantic-kernel/agents/agent-orchestration)
- [HandoffOrchestration 範例](https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithAgents)
- [OpenAI API 文檔](https://platform.openai.com/docs/)
