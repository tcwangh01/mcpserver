# copilot-rest-api

本專案由 GitHub Copilot 協助建立，是一個使用 Python 與 Flask 的簡單 RESTful API 範例。以下說明即為從與 Copilot 的對話中產生的需求：

> 我要建立一個簡單的 RESTful API 服務，程式語言使用 Python 與 Flask 框架．
> - 請幫我建立專案與安裝必要套件
> - 專案資料夾為copilot-rest-api
> - 使用Flask 建立一個簡單的 RESTful API，包括以下功能:
> - 一個 GET 路由 ('/'), 返回 “Hello, World!"
> - 一個 POST 路由 ('/data'),接受 JSON 數據並回傳 "Received"
> - 啟動服務 localhost:5000
> - 加入單元測試
> - 執行單元測試

專案功能摘要：

- GET 路由 `/` 回傳 "Hello, World!"。
- POST 路由 `/data` 接收 JSON，回傳 "Received"。
- 使用 `pytest` 撰寫並執行單元測試。
## 專案建立流程

以下步驟是依照與 Copilot 對話的內容，由助理自動產生專案檔案：

1. **在工作區中建立目錄**
   ```bash
   mkdir -p /Users/timmacpro/PyProjects/mcpserver/copilot-rest-api
   cd /Users/timmacpro/PyProjects/mcpserver/copilot-rest-api
   ```

2. **建立應用程式主檔案 `api.py`**
   Copilot 生成的程式碼如下：
   ```python
   from flask import Flask, request, jsonify

   app = Flask(__name__)

   @app.route('/', methods=['GET'])
   def hello_world():
       return "Hello, World!"

   @app.route('/data', methods=['POST'])
   def receive_data():
       data = request.get_json(silent=True)
       if data is None:
           return jsonify({'error': 'Invalid JSON'}), 400
       return "Received"

   if __name__ == '__main__':
       app.run(host='127.0.0.1', port=5000)
   ```

3. **建立需求檔**
   ```text
   Flask>=2.0
   pytest>=7.0
   ```

4. **建立測試程式**
   Copilot 自動產生 `tests/test_app.py`：
   ```python
   import os
   import sys
   import pytest

   sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))
   import api

   @pytest.fixture
   def client():
       with api.app.test_client() as client:
           yield client

   def test_hello_world(client):
       rv = client.get('/')
       assert rv.status_code == 200
       assert rv.get_data(as_text=True) == "Hello, World!"

   def test_receive_data_valid_json(client):
       response = client.post('/data', json={'key': 'value'})
       assert response.status_code == 200
       assert response.get_data(as_text=True) == "Received"

   def test_receive_data_invalid_json(client):
       response = client.post('/data', data="notjson", content_type='application/json')
       assert response.status_code == 400
       assert "Invalid JSON" in response.get_data(as_text=True)
   ```

## Installation and Environment

```bash
cd /Users/timmacpro/PyProjects/mcpserver/copilot-rest-api
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

## Running the server

With the virtual environment active:

```bash
python api.py
```

The service listens on `http://127.0.0.1:5000`.

### Testing with curl

```bash
# GET
curl http://127.0.0.1:5000/
# POST valid JSON
curl --json '{"foo":"bar"}' http://127.0.0.1:5000/data
# POST invalid JSON
curl -X POST http://127.0.0.1:5000/data -H "Content-Type: application/json" -d 'not a json'
```

Responses:
- `Hello, World!` for the GET
- `Received` for valid POST
- HTTP 400 and an error message for invalid JSON

## Running tests

```bash
source .venv/bin/activate
pytest -q
```

You should see `3 passed`.

---

All files and commands were created/generated with assistance from GitHub Copilot conversation.