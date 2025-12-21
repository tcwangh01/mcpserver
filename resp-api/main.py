# ============================================================
# 匯入必要的函式庫
# ============================================================
import os, asyncio  # os: 用於存取環境變數；asyncio: 用於非同步程式執行
from openai import AsyncOpenAI  # OpenAI 的非同步客戶端，用於呼叫 GPT 模型
from openai.types.responses import ResponseTextDeltaEvent  # 串流回應的事件類型

# ============================================================
# 環境設定：取得 OpenAI API 金鑰並建立客戶端
# ============================================================
# 從環境變數中取得 OpenAI API 金鑰
api_key = os.getenv("OPENAI_API_KEY")

# 檢查 API 金鑰是否存在，若不存在則拋出錯誤
if not api_key:
    raise RuntimeError("OPENAI_API_KEY is not set; please export a valid OpenAI API key.")

# 建立 OpenAI 非同步客戶端實例
client = AsyncOpenAI(api_key=api_key)

# ============================================================
# 工具設定：配置 MCP (Model Context Protocol) 工具
# ============================================================
# 定義可用的工具清單，此處配置 MCP 伺服器連線
tools = [
    {
        "type":"mcp",  # 工具類型：MCP (Model Context Protocol)
        "server_url":"https://savourless-mullishly-vanda.ngrok-free.dev/mcp",  # MCP 伺服器的 URL 位址
        "server_label":"KOKO-Store",  # 伺服器標籤名稱，用於識別此 MCP 服務
        "require_approval": "never"  # 工具呼叫批准設定：never 表示不需要使用者批准即可執行
    }
]

# ============================================================
# 主程式：執行 OpenAI API 呼叫的非同步函數
# ============================================================
async def main():
    # ============================================================
    # 範例 1：非串流式 API 呼叫
    # ============================================================
    # 建立一個完整的回應請求，等待模型完成後一次性返回結果
    response = await client.responses.create(
        model="gpt-4.1",  # 使用的 GPT 模型版本
        input=[  # 輸入訊息陣列
            {
                "role":"user",  # 訊息角色：使用者
                "content":[  # 訊息內容陣列
                    {
                        "type":"input_text",  # 內容類型：文字輸入
                        "text":"茶葉蛋還有多少庫存"  # 查詢文字：詢問茶葉蛋庫存
                    }]
            }
        ],
        tools=tools,  # 提供 MCP 工具供模型使用（可查詢 KOKO-Store 資料）
        temperature=0.3  # 溫度參數：較低的值（0.3）使回應更確定、一致
    )
    # 輸出模型的完整回應文字
    print(response.output_text)

    # ============================================================
    # 範例 2：串流式 API 呼叫
    # ============================================================
    # 建立一個串流回應請求，模型會逐步產生並即時返回內容
    stream = await client.responses.create(
        model="gpt-4.1",  # 使用的 GPT 模型版本
        input=[  # 輸入訊息陣列
            {
                "role":"user",  # 訊息角色：使用者
                "content":[  # 訊息內容陣列
                    {
                        "type":"input_text",  # 內容類型：文字輸入
                        "text":"咖啡還有嗎"  # 查詢文字：詢問咖啡庫存
                    }]
            }
        ],
        tools=tools,  # 提供 MCP 工具供模型使用
        stream=True  # 啟用串流模式：逐步返回回應內容
    )

    # 逐一處理串流中的每個資料塊
    async for chunk in stream:
        # 檢查資料塊是否為文字增量事件（ResponseTextDeltaEvent）
        if isinstance(chunk, ResponseTextDeltaEvent):
            # 即時輸出文字增量，不換行（end=""）且立即刷新緩衝區（flush=True）
            print(chunk.delta, end="", flush=True)

# ============================================================
# 程式執行入口
# ============================================================
# 使用 asyncio.run() 執行非同步主程式
# 這會啟動事件迴圈並執行 main() 函數直到完成
asyncio.run(main())