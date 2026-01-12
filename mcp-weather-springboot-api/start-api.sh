#!/bin/bash

# 啟動 HTTP API Server
echo "Starting MCP Weather API Server (HTTP mode)..."
echo "API will be available at http://localhost:8080"
echo ""
echo "Endpoints:"
echo "  - GET /api/weather/forecast?lat={lat}&lon={lon}"
echo "  - GET /api/weather/alerts?state={state}"
echo "  - GET /api/weather/health"
echo ""

java -jar target/mcp-weather-springboot-api-1.0.0.jar
