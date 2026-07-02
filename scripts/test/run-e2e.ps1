param(
    [int]$TimeoutSeconds = 60,
    [string]$RpcUrl = "http://localhost:17890/mcp/rpc"
)

$ErrorActionPreference = "Stop"
$gatewayRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\gateway")

function Test-UnityRpcPort {
    param([string]$Url)

    try {
        $uri = [Uri]$Url
        $client = New-Object System.Net.Sockets.TcpClient
        $async = $client.BeginConnect($uri.Host, $uri.Port, $null, $null)
        $connected = $async.AsyncWaitHandle.WaitOne(1000, $false)
        if ($connected) {
            $client.EndConnect($async)
            $client.Close()
            return $true
        }
        $client.Close()
        return $false
    }
    catch {
        return $false
    }
}

Write-Host "Waiting for Unity MCP HTTP server at $RpcUrl (up to ${TimeoutSeconds}s)..."
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$ready = $false
while ((Get-Date) -lt $deadline) {
    if (Test-UnityRpcPort -Url $RpcUrl) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 2
}

if (-not $ready) {
    throw @"
Unity MCP server not reachable on port 17890.

1. Open this project in the Unity Editor (repo root or a version worktree).
2. Wait for the console message: [MCP] Initializing Unity MCP Server...
3. Re-run: .\scripts\test\run-e2e.ps1
"@
}

Write-Host "Unity MCP server is up. Running HTTP E2E smoke tests..."

$env:UNITY_E2E = "1"
$env:UNITY_RPC_URL = $RpcUrl

Push-Location $gatewayRoot
try {
    npm run test:e2e
    if ($LASTEXITCODE -ne 0) { throw "E2E tests failed." }
    Write-Host "E2E smoke tests passed."
}
finally {
    Pop-Location
}
