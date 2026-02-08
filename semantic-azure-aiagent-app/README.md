# Semantic Kernel AzureAIAgent 商務助理系統示範

這是一個展示 **Semantic Kernel Agents** 框架結合 **AzureAIAgent**（Persistent Agent）和 **Plugin** 功能的 .NET 應用程式，實現一個具備客戶帳戶查詢與業務代表聯絡資訊查詢功能的 AI 商務助理。

## 為什麼使用 AzureAIAgent？

### 問題：OpenAIAssistantAgent 的限制

在 `OpenAIAssistantAgent` 中，Agent 部署在 OpenAI 平台上，適合直接使用 OpenAI API 的場景：

```csharp
// OpenAIAssistantAgent - 使用 OpenAI 平台
AssistantClient assistantClient = new(new ApiKeyCredential(apiKey));
Assistant assistant = await assistantClient.CreateAssistantAsync("gpt-4o", ...);
var agent = new OpenAIAssistantAgent(assistant, assistantClient, [plugin]);
```

但在企業場景中，您可能需要：
- 使用 Azure 提供的安全性與合規性保障
- 透過 Azure Entra ID 進行身份驗證（而非 API Key）
- 在 Azure AI Foundry 中統一管理 Agent 資源
- 使用 Azure 部署的模型（資料不離開 Azure 區域）

### 解決方案：AzureAIAgent

AzureAIAgent 利用 Azure AI Foundry 的 Persistent Agent 服務，將 Agent 部署到 Azure 平台：

```csharp
// AzureAIAgent - 使用 Azure AI Foundry 平台
PersistentAgentsClient client = AzureAIAgent.CreateAgentsClient(
    azureEndpoint, new AzureCliCredential());

PersistentAgent definition = await client.Administration.CreateAgentAsync(
    modelId, name: "Business Assistant", instructions: instructions);

AzureAIAgent agent = new(definition, client);
```

## AzureAIAgent 的核心優勢

| 優勢 | 說明 |
|------|------|
| **Azure 平台整合** | Agent 部署在 Azure AI Foundry，統一管理與監控 |
| **Entra ID 驗證** | 使用 Azure Entra ID 身份驗證，無需管理 API Key |
| **持久化 Agent** | Agent 建立後持久存在於 Azure 上，可跨 Session 重用 |
| **伺服器端對話管理** | 對話歷史透過 Thread 儲存在 Azure 伺服器端 |
| **串流支援** | 內建 `InvokeStreamingAsync` 即時回應 |
| **函數呼叫** | 與 Plugin 無縫整合，自動呼叫函數 |
| **資料合規** | 資料不離開 Azure 區域，符合企業合規需求 |

## 與其他 Agent 類型的差異

| 項目 | chatcomplete-agent-app | aiassistant-agent-app | azure-aiagent-app |
|------|------------------------|------------------------|--------------------|
| **Agent 類型** | ChatCompletionAgent | OpenAIAssistantAgent | AzureAIAgent |
| **平台** | 本地 + OpenAI API | OpenAI Assistants API | Azure AI Foundry |
| **對話歷史儲存** | 本地記憶體（ChatHistory） | OpenAI 伺服器端（Thread） | Azure 伺服器端（Thread） |
| **對話執行緒** | ChatHistoryAgentThread | OpenAIAssistantAgentThread | AzureAIAgentThread |
| **驗證方式** | API Key | API Key | Azure Entra ID（AzureCliCredential） |
| **Kernel 需求** | 需要建立 Kernel 物件 | 不需要 Kernel | 需要建立 Kernel（用於 Plugin 執行） |
| **Plugin 註冊** | `kernel.Plugins.AddFromType<T>()` | 建構函式傳入 `KernelPlugin` | `agent.Kernel.Plugins.Add(plugin)` |
| **Agent 持久化** | 無（僅存在於記憶體） | OpenAI 伺服器（需管理 Assistant ID） | Azure AI Foundry（可在入口網站管理） |
| **函數呼叫設定** | 需設定 `FunctionChoiceBehavior.Auto()` | 自動支援 | 自動支援 |
| **進階功能** | 僅聊天完成 | Code Interpreter、File Search | Azure AI 服務整合 |
| **適用場景** | 簡單問答、快速原型 | 需要 OpenAI 進階功能 | 企業級、需要安全合規 |

### 程式碼差異對比

**ChatCompletionAgent 方式：**
```csharp
// 使用 OpenAI API Key + 本地 Kernel
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId: "gpt-4o", apiKey: apiKey)
    .Build();
kernel.Plugins.AddFromType<CustomerSupportService>();

ChatCompletionAgent agent = new()
{
    Name = "SupportAgent",
    Instructions = "系統提示詞...",
    Kernel = kernel,
    Arguments = new(new PromptExecutionSettings {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    })
};

ChatHistoryAgentThread thread = new();
```

**OpenAIAssistantAgent 方式：**
```csharp
// 使用 OpenAI API Key + Assistants API
AssistantClient assistantClient = new(new ApiKeyCredential(apiKey));
Assistant assistant = await assistantClient.CreateAssistantAsync("gpt-4o", ...);
var agent = new OpenAIAssistantAgent(assistant, assistantClient, [plugin]);

OpenAIAssistantAgentThread agentThread = new(assistantClient);
```

**AzureAIAgent 方式（本專案）：**
```csharp
// 使用 Azure Entra ID + Azure AI Foundry
PersistentAgentsClient client = AzureAIAgent.CreateAgentsClient(
    azureEndpoint, new AzureCliCredential());

PersistentAgent definition = await client.Administration.CreateAgentAsync(
    modelId, name: "Business Assistant", instructions: "系統提示詞...");

AzureAIAgent agent = new(definition, client)
{
    Kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(modelId, azureEndpoint, new AzureCliCredential())
        .Build()
};
agent.Kernel.Plugins.Add(plugin);

AzureAIAgentThread agentThread = new(agent.Client);
```

## 功能特性

- **AzureAIAgent**：使用 Azure AI Foundry Persistent Agent 的 Agent 封裝
- **互動式指令**：透過終端機指令管理 Agent 的生命週期（建立 / 刪除）
- **Plugin 自動函數調用**：AI 自動判斷何時需要調用商務助理函數
- **AzureAIAgentThread**：Azure 伺服器端對話歷史管理
- **串流回應**：即時顯示 AI 回應（打字機效果）
- **函數追蹤**：顯示 AI 呼叫的函數名稱（Trace）
- **自動清理**：程式結束時自動刪除 Agent 與 Thread，避免資源殘留
- **商務助理功能**：
  - 客戶帳戶資訊與合約狀態查詢
  - 業務代表聯絡資訊查詢

## 系統架構

```
┌─────────────────────────────────────────────────────────────┐
│                      使用者介面 (Console)                    │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  互動式指令：create agent / delete agent / exit      │   │
│  │  對話模式：直接輸入訊息與 Agent 對話                   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                       AzureAIAgent                           │
│  ┌─────────────────┐  ┌─────────────────────────────────┐  │
│  │ PersistentAgent  │  │    Semantic Kernel               │  │
│  │ (Azure 上的定義)  │  │  ┌───────────────────────────┐  │  │
│  │  • Agent ID      │  │  │ BusinessAssistantService   │  │  │
│  │  • Instructions  │  │  │  • GetCustomerInfo        │  │  │
│  │  • Model         │  │  │  • GetSalespersonContact  │  │  │
│  └─────────────────┘  │  └───────────────────────────┘  │  │
│                        └─────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│                   PersistentAgentsClient                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │            AzureAIAgentThread                        │   │
│  │   (Azure 伺服器端對話歷史 — 透過 Thread ID 追蹤)     │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │            AzureCliCredential                        │   │
│  │   (Azure Entra ID 驗證 — 使用 az login 身分)         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                             ↓↑
┌─────────────────────────────────────────────────────────────┐
│            Azure AI Foundry (GPT-4o-mini)                   │
│  ┌────────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │  Chat / 推論    │  │ Thread 管理   │  │ Function Call │  │
│  └────────────────┘  └──────────────┘  └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## 環境需求

- [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) 或更高版本
- Azure 訂閱帳戶
- Azure AI Foundry 專案（已部署模型）
- Azure CLI（用於 Entra ID 驗證）

## 專案建立步驟

### 1. 建立專案

```bash
dotnet new console -n semantic-azure-aiagent-app
```

### 2. 安裝 NuGet 套件

```bash
dotnet add package Microsoft.SemanticKernel --version 1.70.0
dotnet add package Microsoft.SemanticKernel.Agents.AzureAI --version 1.70.0-preview
dotnet add package Azure.Identity --version 1.17.1
```

### 3. 設定 .csproj 抑制實驗性 API 警告

由於 `AzureAIAgent` 目前為預覽版本，需要在 `.csproj` 中加入 `NoWarn` 抑制 `SKEXP0110` 警告：

```xml
<PropertyGroup>
    <NoWarn>SKEXP0110</NoWarn>
</PropertyGroup>
```

### 4. Azure CLI 登入

```bash
# 使用 Entra ID 登入 Azure（指定租戶）
az login --tenant <tenant-id>
```

### 5. 設定端點與模型

在 `Program.cs` 中修改以下設定：

```csharp
// Azure AI Foundry 專案端點（非 Azure OpenAI 端點）
// 格式：https://<resource>.services.ai.azure.com/api/projects/<project>
var azureEndpoint = "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>";

// Azure AI Foundry 中部署的模型名稱
var modelId = "gpt-4o-mini";
```

> **注意**：端點必須是 Azure AI Foundry 專案端點（`services.ai.azure.com`），而非 Azure OpenAI 端點（`cognitiveservices.azure.com`），否則會回傳 404 錯誤。

## 執行專案

```bash
# 編譯專案
dotnet build

# 執行專案
dotnet run
```

## 執行範例

```
=== Azure AI Agent 互動式管理工具 ===

可用指令：
  create agent  - 建立新的 Azure AI Agent（部署至 Azure AI Foundry）
  刪除 agent    - 刪除目前的 Agent（或 delete agent）
  exit          - 離開程式

請先輸入 'create agent' 建立 Agent，建立後即可直接輸入訊息進行對話。

> create agent
[*] 正在建立 Azure AI Agent...
[v] Agent 已建立！
    Agent ID  : asst_abc123def456
    Agent Name: Business Assistant
    Thread ID : thread_xyz789
    您可以在 Azure AI Foundry 中查看此 Agent。

現在可以開始對話，輸入 'delete agent' 可刪除 Agent。

User > 我想查詢大大銀行的帳戶資訊
# trace assistant - *: FUNCTION CALL - BusinessAssistantService-GetCustomerInfo

assistant - Business Assistant > 大大銀行的帳號是 #SINO123，合約狀態為有效（到期日 2025/12/31）。請問還需要查詢其他資訊嗎？

# trace chat thread with agent: Business Assistant - ..., threadId: thread_xyz789

User > Ian 的聯絡方式是什麼？
# trace assistant - *: FUNCTION CALL - BusinessAssistantService-GetSalespersonContact

assistant - Business Assistant > Ian 的聯絡方式如下：
- Email：ian@company.com
- 電話：02-1688-0857

請問還有其他需要幫忙的嗎？

# trace chat thread with agent: Business Assistant - ..., threadId: thread_xyz789

User > delete agent
[*] 正在刪除 Agent (ID: asst_abc123def456)...
[v] Agent (ID: asst_abc123def456) 與對話 Thread 已刪除。

> exit
再見！
```

## 核心程式碼說明

### 1. 建立 PersistentAgentsClient

```csharp
// 建立 Azure AI Agent 服務客戶端
// AzureCliCredential 使用本機 az login 的身分進行 Entra ID 驗證
PersistentAgentsClient client = AzureAIAgent.CreateAgentsClient(
    azureEndpoint, new AzureCliCredential());
```

### 2. 註冊 Plugin

```csharp
// 掃描 BusinessAssistantService 類別，將 [KernelFunction] 方法註冊為可呼叫工具
KernelPlugin plugin = KernelPluginFactory.CreateFromType<BusinessAssistantService>();
```

### 3. 在 Azure AI Foundry 上建立 Agent

```csharp
// 在 Azure 上建立持久化 Agent 定義（會出現在 Azure AI Foundry 入口網站）
PersistentAgent definition = await client.Administration.CreateAgentAsync(
    modelId,
    name: "Business Assistant",
    description: "商務助理 AI",
    instructions: instructions);

// 建立本地 AzureAIAgent 包裝，設定 Kernel 用於 Plugin 執行
AzureAIAgent agent = new(definition, client)
{
    Kernel = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(modelId, azureEndpoint, new AzureCliCredential())
        .Build()
};

// 自訂 Plugin 不會被 Agent 持久化，每次啟動都需要重新掛載
agent.Kernel.Plugins.Add(plugin);
```

### 4. 使用 AzureAIAgentThread 管理對話

```csharp
// 建立 Azure 伺服器端對話執行緒
AzureAIAgentThread agentThread = new(agent.Client);

// 串流呼叫 Agent
ChatMessageContent message = new(AuthorRole.User, input);
await foreach (StreamingChatMessageContent response in
    agent.InvokeStreamingAsync(message, agentThread))
{
    Console.Write(response.Content);
}
```

### 5. 刪除 Agent 與 Thread

```csharp
// 先刪除對話 Thread
await agentThread.DeleteAsync();

// 再刪除 Azure 上的 Agent 資源
await client.Administration.DeleteAgentAsync(agent.Id);
```

### 6. Plugin 函數定義

```csharp
public class BusinessAssistantService
{
    [KernelFunction]
    [Description("Query the customer's account and contract status")]
    public static string GetCustomerInfo(
        [Description("customer name")] string customerName)
    {
        return customerName switch
        {
            "大大銀行" => "大大銀行的帳號是 #SINO123，合約狀態為有效（到期日 2025/12/31）",
            "錢多多金控" => "錢多多金控的帳號是 #TAISHIN001，合約狀態為審核中",
            _ => $"找不到名為 {customerName} 的客戶資料。"
        };
    }

    [KernelFunction]
    [Description("Query the salesperson's contact information")]
    public static string GetSalespersonContact(
        [Description("salesperson name")] string name)
    {
        return name switch
        {
            "Ian" => "Ian 的聯絡方式：ian@company.com，電話 02-1688-0857",
            "Cheryl" => "Cheryl 的聯絡方式：cheryl@company.com，電話 02-9571-1688",
            _ => $"找不到 {name} 的聯絡資訊。"
        };
    }
}
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 1.70.0 | 核心框架（KernelPlugin、Kernel 等） |
| Microsoft.SemanticKernel.Agents.AzureAI | 1.70.0-preview | Azure AI Agent 支援 |
| Azure.Identity | 1.17.1 | Azure Entra ID 驗證（AzureCliCredential） |

## 互動式指令說明

| 指令 | 說明 |
|------|------|
| `create agent` | 在 Azure AI Foundry 上建立新的持久化 Agent |
| `delete agent` / `刪除 agent` | 刪除目前的 Agent 與對話 Thread |
| `exit` / `quit` | 離開程式（若 Agent 尚未刪除，會自動清理） |
| 其他任意文字 | 與 Agent 進行對話 |

## 常見問與答

### Q: AzureAIAgent、OpenAIAssistantAgent、ChatCompletionAgent 該怎麼選？

**A: 取決於你的平台需求與使用場景。**

| 需求 | 推薦方案 |
|------|---------|
| 簡單問答、快速原型 | ChatCompletionAgent |
| 需要 Code Interpreter / File Search | OpenAIAssistantAgent |
| 直接使用 OpenAI 平台 | OpenAIAssistantAgent |
| 企業級、需要 Azure 安全合規 | AzureAIAgent |
| 使用 Azure Entra ID 驗證 | AzureAIAgent |
| 在 Azure AI Foundry 統一管理 | AzureAIAgent |
| 資料不離開 Azure 區域 | AzureAIAgent |
| 需要最低延遲 | ChatCompletionAgent |
| 離線或私有部署 | ChatCompletionAgent |

### Q: Agent 建立後會持久存在嗎？

**A: 是的！** `PersistentAgent` 建立後會持久存在於 Azure AI Foundry 上，即使程式結束也不會消失。您可以在 Azure AI Foundry 入口網站中查看和管理所有已建立的 Agent。

本範例程式在以下時機會自動刪除 Agent：
- 使用者輸入 `delete agent` / `刪除 agent`
- 使用者輸入 `exit` 時，若 Agent 尚未刪除，會自動清理

正式環境建議儲存 Agent ID，並在後續執行中重用：

```csharp
// 重用已建立的 Agent（正式環境建議做法）
PersistentAgent definition = await client.Administration.GetAgentAsync("asst_abc123");
AzureAIAgent agent = new(definition, client);
agent.Kernel.Plugins.Add(plugin); // 自訂 Plugin 需每次重新掛載
```

### Q: 為什麼自訂 Plugin 需要每次重新掛載？

**A: 因為自訂 Plugin（如 BusinessAssistantService）不會隨 Agent 持久化到 Azure。** Azure AI Foundry 只儲存 Agent 的定義（模型、指令、名稱等），而 Plugin 中的函數邏輯是在本地端執行的，所以每次啟動程式時都需要重新將 Plugin 掛載到 Agent 的 Kernel 上。

### Q: 端點（Endpoint）用哪一個？

**A: 必須使用 Azure AI Foundry 專案端點。**

| 端點類型 | 格式 | 是否可用 |
|---------|------|---------|
| Azure AI Foundry 專案端點 | `https://<resource>.services.ai.azure.com/api/projects/<project>` | ✅ 正確 |
| Azure OpenAI 端點 | `https://<resource>.cognitiveservices.azure.com/` | ❌ 會回傳 404 |

可在 Azure AI Foundry 入口網站 > 您的專案 > Overview 頁面找到正確的端點。

### Q: 解析使用者問題的是 AzureAIAgent 還是 Semantic Kernel？

**A: 都不是！真正解析問題的是 Azure AI Foundry 上部署的 GPT-4o-mini 模型。**

```
┌─────────────────────────────────────────────────────────────────┐
│                  您的輸入: "查詢大大銀行的帳戶"                    │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  AzureAIAgent                                                    │
│  職責：                                                          │
│  • 包裝 PersistentAgent 定義 + PersistentAgentsClient            │
│  • 管理對話執行緒 (AzureAIAgentThread)                           │
│  • 提供統一的呼叫介面 (InvokeStreamingAsync)                     │
│  • 掛載與執行本地端的 Plugin 函式                                 │
│  • ❌ 不解析問題                                                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  Azure AI Foundry + GPT-4o-mini    ← ✅ 真正解析問題的地方       │
│  職責：                                                          │
│  • 管理 Thread（伺服器端對話歷史）                                │
│  • 理解使用者意圖（自然語言理解 NLU）                              │
│  • 根據 Instructions 決定如何回應                                 │
│  • 判斷是否需要呼叫函數（Function Calling）                       │
│  • 生成回應文字                                                  │
└─────────────────────────────────────────────────────────────────┘
```

#### 實際流程範例

當您輸入「查詢大大銀行的帳戶」：

| 步驟 | 元件 | 動作 |
|-----|------|-----|
| 1 | AzureAIAgent | 將訊息加入 Thread |
| 2 | PersistentAgentsClient | 透過 Azure AI Agent API 發送請求 |
| 3 | **GPT-4o-mini** | **解析問題，判斷意圖是「查詢客戶帳戶」** |
| 4 | **GPT-4o-mini** | **決定呼叫 `GetCustomerInfo("大大銀行")`** |
| 5 | Semantic Kernel | 在本地執行 `BusinessAssistantService.GetCustomerInfo("大大銀行")` |
| 6 | PersistentAgentsClient | 將函數結果回傳給 Azure AI Agent API |
| 7 | **GPT-4o-mini** | **根據結果生成友善的回應文字** |
| 8 | AzureAIAgent | 串流回傳結果給使用者 |

---

## 注意事項

1. **Azure 驗證**：執行前請確認已透過 `az login --tenant <tenant-id>` 登入
2. **Agent 管理**：本範例每次 `create agent` 都會在 Azure 上建立新 Agent，正式環境應重用已建立的 Agent
3. **成本考量**：Azure AI Foundry 的 API 調用與 Agent 資源可能產生費用
4. **Preview 套件**：`Microsoft.SemanticKernel.Agents.AzureAI` 目前為預覽版本，API 未來可能變更
5. **SKEXP0110 警告**：AzureAIAgent 標記為實驗性，需在 `.csproj` 中以 `<NoWarn>SKEXP0110</NoWarn>` 抑制
6. **資源清理**：若程式異常終止，可能在 Azure AI Foundry 上留下未刪除的 Agent，請至入口網站手動清理

## 學習路徑建議

1. **hello-semantic-kernel-app** - 基礎 Semantic Kernel 使用
2. **hello-semantic-kernel-plugin-app** - 學習 Plugin 架構
3. **semantic-kernel-plugin-history-app** - 學習 ChatHistory
4. **semantic-chatcomplete-agent-app** - 學習 ChatCompletionAgent 架構
5. **semantic-aiassistant-agent-app** - 學習 OpenAIAssistantAgent 架構
6. **semantic-azure-aiagent-app** - 學習 AzureAIAgent 架構（本專案）

## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/semantic-kernel/)
- [Semantic Kernel Agents 文檔](https://learn.microsoft.com/semantic-kernel/agents/)
- [Azure AI Foundry 文檔](https://learn.microsoft.com/azure/ai-studio/)
- [Azure AI Agent Service 文檔](https://learn.microsoft.com/azure/ai-services/agents/)
- [Azure.Identity 文檔](https://learn.microsoft.com/dotnet/api/azure.identity)
