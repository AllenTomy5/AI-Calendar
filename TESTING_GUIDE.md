# How to Test: Ollama vs Mock ChatClient

## 🔍 Quick Test Methods

### Method 1: Client Type Diagnostics Endpoint

1. **Start the API:**
   ```powershell
   cd "D:\NET\Project"
   dotnet run --project AiCalendar.Api
   ```

2. **Check which client is being used:**
   ```powershell
   curl http://localhost:5213/api/Diagnostics/client-type
   ```

   **Expected Results:**
   - **If using Ollama:** `"IsOllama": true, "ClientType": "OllamaApiClient"`
   - **If using Mock:** `"IsOllama": false, "ClientType": "MockChatClient"`

### Method 2: Direct LLM Test

3. **Test the actual LLM response:**
   ```powershell
   curl -X POST http://localhost:5213/api/Diagnostics/test-llm -H "Content-Type: application/json" -d '{"prompt": "Hello"}'
   ```

   **Expected Results:**
   - **If using Ollama:** Real LLM response (varies each time)
   - **If using Mock:** `"MOCK_CHAT_CLIENT_RESPONSE - This is clearly from the MockChatClient fallback, not Ollama"`

## 🧪 Comprehensive Testing Steps

### Step 1: Without Ollama Server (Expected: Mock Fallback)

1. **Ensure Ollama is NOT running** (no server at localhost:11434)

2. **Start API and check logs:**
   ```powershell
   dotnet run --project AiCalendar.Api
   ```
   Look for log message: `"Failed to configure Ollama client"` or `"Using MockChatClient as fallback"`

3. **Test client type:**
   ```powershell
   curl http://localhost:5213/api/Diagnostics/client-type
   ```
   Should show: `"IsOllama": false`

4. **Test LLM response:**
   ```powershell
   curl -X POST http://localhost:5213/api/Diagnostics/test-llm -H "Content-Type: application/json" -d '{"prompt": "Hello"}'
   ```
   Should return: `"MOCK_CHAT_CLIENT_RESPONSE"`

### Step 2: With Ollama Server (Expected: Real Ollama)

1. **Install and start Ollama:**
   ```bash
   # Install Ollama from https://ollama.ai
   ollama serve
   ollama pull mistral
   ```

2. **Start API and check logs:**
   ```powershell
   dotnet run --project AiCalendar.Api  
   ```
   Look for log message: `"Successfully configured OllamaSharp ChatClient with model mistral"`

3. **Test client type:**
   ```powershell
   curl http://localhost:5213/api/Diagnostics/client-type
   ```
   Should show: `"IsOllama": true, "ClientType": "OllamaApiClient"`

4. **Test LLM response:**
   ```powershell
   curl -X POST http://localhost:5213/api/Diagnostics/test-llm -H "Content-Type: application/json" -d '{"prompt": "Hello"}'
   ```
   Should return: Real LLM-generated response (different each time)

## 🔎 Log Analysis

### Look for these log patterns:

**Ollama Success:**
```
Successfully configured OllamaSharp ChatClient with model mistral at http://localhost:11434
```

**Mock Fallback:**
```
Failed to configure Ollama client at http://localhost:11434. Using MockChatClient as fallback
```

**Runtime Test Logs:**
```
Chat client diagnosis: Type=OllamaApiClient, Assembly=OllamaSharp, IsOllama=True
# OR
Chat client diagnosis: Type=MockChatClient, Assembly=AiCalendar.Api, IsOllama=False
```

## 📊 Verification Checklist

- [ ] **Build Success**: Project compiles with both OllamaSharp and MockChatClient
- [ ] **Service Registration**: Logs show which client type was configured
- [ ] **Runtime Detection**: `/api/Diagnostics/client-type` correctly identifies the client
- [ ] **Response Differences**: Mock returns obvious "MOCK" text, Ollama returns varied responses
- [ ] **Graceful Fallback**: System works whether Ollama is available or not

## 🚨 Current Status Without Ollama Server

Since you don't have Ollama running locally, the system will:

1. **Try to create OllamaApiClient** → This succeeds (just configuration)
2. **On first request** → Connection fails to localhost:11434  
3. **Fallback behavior** → Depends on error handling in LlmService

The system is currently configured to use **OllamaApiClient** but will encounter connection errors when making actual requests, which our **fallback logic in LlmService** handles gracefully.

## 🎯 Bottom Line

**To definitively know which client you're using:**
1. Hit `/api/Diagnostics/client-type` - shows the configured client
2. Hit `/api/Diagnostics/test-llm` - shows the actual behavior
3. Check startup logs for configuration messages

The key difference: **OllamaApiClient** is configured but fails at runtime, **MockChatClient** works immediately with obvious mock responses.