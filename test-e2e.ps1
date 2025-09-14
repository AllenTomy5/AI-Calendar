# End-to-End Natural Language Test
Write-Host "=== AI Calendar Natural Language End-to-End Test ===" -ForegroundColor Green

$apiUrl = "http://localhost:5213/api/natural-language/process"
$mcpUrl = "http://localhost:3000"

Write-Host "Testing servers..." -ForegroundColor Yellow

# Test MCP server first
try {
    $mcpHealth = Invoke-WebRequest -Uri "$mcpUrl/health" -UseBasicParsing
    Write-Host "✅ MCP Server: $($mcpHealth.StatusCode) - $($mcpHealth.Content)" -ForegroundColor Green
} catch {
    Write-Host "❌ MCP Server not responding: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test Natural Language Processing
$testPrompts = @(
    "Schedule a meeting with Bob tomorrow at 2 PM in the conference room",
    "Create an appointment for dental checkup next Monday at 9 AM",
    "Book a team standup for today at 10:30 AM"
)

foreach ($prompt in $testPrompts) {
    Write-Host "`n--- Testing: $prompt ---" -ForegroundColor Cyan
    
    $body = @{ prompt = $prompt } | ConvertTo-Json
    
    try {
        $response = Invoke-WebRequest -Uri $apiUrl -Method POST -Body $body -ContentType "application/json" -UseBasicParsing -TimeoutSec 30
        
        Write-Host "✅ Status: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "📝 Response:" -ForegroundColor White
        $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 5
        
    } catch {
        Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            Write-Host "Error Details: $errorBody" -ForegroundColor Red
        }
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green