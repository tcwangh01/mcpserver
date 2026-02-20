import os
import sys
import pytest

# add project root (one level up) to path so we can import api.py directly
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
    # body should mention the error
    assert "Invalid JSON" in response.get_data(as_text=True)
