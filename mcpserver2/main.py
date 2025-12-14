from typing import List, Dict
from mcp.server.fastmcp import FastMCP
from fastapi import FastAPI
import uvicorn, os

# 庫存資料
inventory: Dict[str,int] = {
    "咖啡": 42,
    "茶葉蛋": 18,
    "洋芋片": 30,
    "牛奶":25
}

# MCP Server

mcp = FastMCP("KOKO store")

# 調整內建路徑，避免 mount 時重複 /mcp/mcp 或 /sse/sse
mcp.settings.streamable_http_path = "/"
mcp.settings.sse_path = "/"
# 允許外部（ngrok）Host / Origin；開發階段關閉 DNS rebinding 保護
mcp.settings.transport_security.enable_dns_rebinding_protection = False
mcp.settings.transport_security.allowed_hosts = ["*"]
mcp.settings.transport_security.allowed_origins = ["*"]

@mcp.tool(name="search", description="依關鍵字搜尋產品並提供摘要")
def search(query: str, limit: int=10) -> List[Dict]:
    results = []
    query = query.lower()
    for product, qty in inventory.items():
        if query in product.lower() and len(results) < limit:
            results.append({
                "id": product,
                "title": product,
                "snippet": f"{product} 庫存 {qty} 件"
            })
    return results

@mcp.tool(name="fetch", description="依 ID 取回商品完整庫存資訊")
def fetch(ids: List[str]) -> List[Dict]:
    docs = []
    for _id in ids:
        if _id in inventory:
            docs.append({
                "id": _id,
                "text": f"{_id} 目前庫存 {inventory[_id]} 件"
            })
    return docs

# FastAPI
# 先初始化 MCP HTTP app，session_manager 才會被建立
mcp_http_app = mcp.streamable_http_app()
mcp_sse_app = mcp.sse_app()
app = FastAPI(title="KOKO 便利商店", lifespan=lambda app: mcp.session_manager.run())
app.mount("/mcp", mcp_http_app)  # /mcp
app.mount("/sse", mcp_sse_app)   # /sse 與 /sse/messages/
@app.get("/")
def index():
    return {"message":"歡迎來到 KOKO 便利商店的 MCP 服務!"}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0",port=8000, log_level="info")
