@echo off
echo ==============================================
echo AI Calendar - LLM Client Type Test
echo ==============================================

echo Starting API server...
start "API Server" cmd /k "cd /d D:\NET\Project && dotnet run --project AiCalendar.Api"

echo Waiting for server to start...
timeout /t 10

echo.
echo Testing client type...
curl -s http://localhost:5213/api/Diagnostics/client-type

echo.
echo.
echo Testing LLM response...
curl -s -X POST http://localhost:5213/api/Diagnostics/test-llm -H "Content-Type: application/json" -d "{\"prompt\": \"Hello\"}"

echo.
echo.
echo Press any key to continue...
pause