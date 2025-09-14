# Test script for the Natural Language API endpoint

$uri = "http://localhost:5213/api/natural-language/process"
$headers = @{
    "Content-Type" = "application/json"
}

$body = @{
    prompt = "Schedule a meeting with Bob tomorrow at 2 PM in the conference room"
} | ConvertTo-Json

Write-Host "Testing Natural Language API..." -ForegroundColor Green
Write-Host "URI: $uri" -ForegroundColor Yellow
Write-Host "Request Body: $body" -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri $uri -Method POST -Body $body -Headers $headers -UseBasicParsing
    Write-Host "Response Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response Content:" -ForegroundColor Green
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Response Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
}