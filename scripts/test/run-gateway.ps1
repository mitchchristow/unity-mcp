$ErrorActionPreference = "Stop"
$gatewayRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\gateway")
Push-Location $gatewayRoot
try {
    npm test
    if ($LASTEXITCODE -ne 0) { throw "Gateway tests failed." }
    Write-Host "Gateway tests passed."
}
finally {
    Pop-Location
}
