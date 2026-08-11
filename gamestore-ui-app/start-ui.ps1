Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
try {
    node .\serve-ui.js
}
finally {
    Pop-Location
}
