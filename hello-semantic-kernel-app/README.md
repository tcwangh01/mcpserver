# Semantic Kernel 多 AI 服務整合範例

這是一個使用 Microsoft Semantic Kernel 框架整合多個 AI 服務的示範專案，展示如何在同一個應用程式中使用 OpenAI 和 Google Gemini 兩個不同的 AI 模型。

## 功能特性

- ✅ 整合 OpenAI GPT-4 模型
- ✅ 整合 Google Gemini 2.0 Flash 模型
- ✅ 使用環境變數管理 API Keys
- ✅ 展示 Semantic Kernel 的基本使用方式
- ✅ 示範多 AI 服務在同一應用中的協同使用

## 環境需求

- .NET 10.0 或更高版本
- OpenAI API Key
- Google Gemini API Key

## 專案建立步驟

### 1. 建立 .NET C# Console 應用程式

開始開發前要先完成 .NET C#  的開發環境環境．建立方式可參考這篇 [https://github.com/tcwangh01/mcpserver/tree/master/hello-semantic-kernel-app](在 MacBook 上建立 .NET/C# 開發環境)． 
C# 開發環境建立後，開啟終端機，導航至您想要建立專案的目錄：

```bash
# 建立新的控制台應用程式
dotnet new console -n hello-semantic-kernel-app

# 進入專案目錄
cd ello-semantic-kernel-app
```

這會建立一個包含以下檔案的專案：
- `Program.cs` - 主程式檔案
- `ello-semantic-kernel-app.csproj` - 專案設定檔
- `obj/` - 編譯暫存目錄

### 2. 安裝相關套件

```bash
# 安裝 Semantic Kernel 核心套件
dotnet add package Microsoft.SemanticKernel

# 安裝 Google Gemini 連接器 (預覽版)
dotnet add package Microsoft.SemanticKernel.Connectors.Google --prerelease
```

### 3. 設定環境變數

在 `~/.zshrc` (Mac/Linux) 或系統環境變數中加入：

```bash
export OPENAI_API_KEY="your-openai-api-key-here"
export GEMINI_API_KEY="your-gemini-api-key-here"
```

重新載入環境變數：

```bash
source ~/.zshrc
```

### 4. 驗證環境變數

```bash
echo $OPENAI_API_KEY
echo $GEMINI_API_KEY
```

## 取得 API Keys

### OpenAI API Key
1. 前往 [OpenAI Platform](https://platform.openai.com/api-keys)
2. 登入或註冊帳號
3. 在 API Keys 頁面創建新的 API Key

### Google Gemini API Key
1. 前往 [Google AI Studio](https://aistudio.google.com/apikey)
2. 使用 Google 帳號登入
3. 創建 API Key

## 執行專案

```bash
# 編譯專案
dotnet build

# 執行專案
dotnet run
```

## 專案結構

```
hello-semantic-kernel-app/
├── Program.cs              # 主程式
├── README.md              # 專案說明文件
└── hello-semantic-kernel-app.csproj  # 專案設定檔
```

## 程式說明

這個範例程式會依序執行以下操作：

1. **OpenAI 查詢**：向 GPT-4 詢問「什麼是 Semantic Kernel？」
2. **Gemini 查詢**：向 Gemini 2.0 Flash 詢問「你認識台灣嗎」

### 輸出範例

```
=== OpenAI 回應 ===
Semantic Kernel 是一個...

=== Gemini 回應 ===
是的，我認識台灣...
```

## 使用的套件

| 套件名稱 | 版本 | 用途 |
|---------|------|------|
| Microsoft.SemanticKernel | 最新穩定版 | Semantic Kernel 核心框架 |
| Microsoft.SemanticKernel.Connectors.Google | 1.68.0-alpha | Google Gemini 連接器 |

## 支援的模型

### OpenAI 模型
- `gpt-4`
- `gpt-4-turbo`
- `gpt-3.5-turbo`

### Google Gemini 模型
- `gemini-2.0-flash` (目前使用)
- `gemini-2.5-flash`
- `gemini-2.5-pro`

## 注意事項

1. **API Key 安全**：絕對不要將 API Keys 硬編碼在程式碼中或提交到版本控制系統
2. **實驗性功能**：Google Connector 目前是實驗性功能 (SKEXP0070)
3. **API 費用**：使用 OpenAI 和 Gemini API 會產生費用，請注意使用量
4. **環境變數**：確保在執行前已正確設定環境變數

## 疑難排解

### 錯誤：請設定 OPENAI_API_KEY 環境變數
確認環境變數已正確設定：
```bash
echo $OPENAI_API_KEY
```

### 錯誤：404 Not Found
檢查模型 ID 是否正確，並確認 API Key 有效

### 套件版本問題
如果遇到套件相容性問題，請確認使用最新版本：
```bash
dotnet list package
dotnet add package Microsoft.SemanticKernel --prerelease
```
## 參考資源

- [Semantic Kernel 官方文檔](https://learn.microsoft.com/en-us/semantic-kernel/)
- [OpenAI API 文檔](https://platform.openai.com/docs)
- [Google Gemini API 文檔](https://ai.google.dev/gemini-api/docs)
- [Semantic Kernel GitHub](https://github.com/microsoft/semantic-kernel)

