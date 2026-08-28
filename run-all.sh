#!/bin/bash
# Run both backend and frontend for POS application

echo "=== Starting POS Backend & Frontend ==="
echo ""

# Start backend in background
cd src/PosBackend.Api
dotnet run --urls "http://localhost:5244" &
BACKEND_PID=$!

echo "Backend started (PID: $BACKEND_PID) on http://localhost:5244"
echo ""

# Wait a moment for backend to start
sleep 3

# Start frontend in background
cd /Users/vix/PosBackend/frontend
npm run dev &
FRONTEND_PID=$!

echo "Frontend started (PID: $FRONTEND_PID) on http://localhost:5173"
echo ""
echo "=== Application URLs ==="
echo "  Frontend:  http://localhost:5173"
echo "  Backend API: http://localhost:5244/api"
echo "  Swagger UI: http://localhost:5244/swagger"
echo "  Combined:   http://localhost:5173 (with API proxy to 5244)"
echo ""
echo "Press Ctrl+C to stop both servers..."

# Wait for Ctrl+C
wait $BACKEND_PID $FRONTEND_PID 2>/dev/null

# Cleanup on exit
kill $BACKEND_PID $FRONTEND_PID 2>/dev/null 2>&1
