from flask import Flask, request, jsonify

app = Flask(__name__)

@app.route('/', methods=['GET'])
def hello_world():
    return "Hello, World!"

@app.route('/data', methods=['POST'])
def receive_data():
    # Expect JSON data. use silent=True so we can handle bad payloads ourselves
    data = request.get_json(silent=True)
    if data is None:
        return jsonify({'error': 'Invalid JSON'}), 400
    return "Received"

if __name__ == '__main__':
    # Run the Flask development server on localhost:5000
    app.run(host='127.0.0.1', port=5000)