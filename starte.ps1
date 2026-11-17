# Startet ein Beispiel mit den Zugangsdaten aus .env (Windows, PowerShell).
#   .\starte.ps1 kapitel10_erste_schritte\01_verbinden.cs [Argumente]
#   .\starte.ps1 kapitel17_dashboard [Argumente]          (Projektordner)
param([Parameter(Mandatory)][string]$Ziel, [Parameter(ValueFromRemainingArguments)]$Rest)
Set-Location $PSScriptRoot
if (Test-Path .env) {
    Get-Content .env | Where-Object { $_ -match '^\s*[A-Z_]+=' } | ForEach-Object {
        $name, $wert = $_ -split '=', 2
        Set-Item -Path "Env:$($name.Trim())" -Value $wert.Trim()
    }
}
if (Test-Path $Ziel -PathType Container) { dotnet run --project $Ziel -- @Rest }
else { dotnet run $Ziel -- @Rest }
