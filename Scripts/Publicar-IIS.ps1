[CmdletBinding()]
param(
    [string]$Destino = 'E:\inetpub\Nutrie\NutrieGestao\App',
    [string]$Staging = 'E:\NutrieGestao\PublishTemp'
)

$ErrorActionPreference = 'Stop'
$projeto = Join-Path $PSScriptRoot '..\NutrieGestao.Web\NutrieGestao.Web.csproj'
$destinoCompleto = [IO.Path]::GetFullPath($Destino)
$raizProducao = Split-Path -Parent $destinoCompleto

if ((Split-Path -Leaf $destinoCompleto) -ne 'App') {
    throw "Por segurança, o destino deve ser a pasta App. Recebido: $destinoCompleto"
}

foreach ($pasta in @($destinoCompleto, (Join-Path $raizProducao 'Data'), (Join-Path $raizProducao 'Logs'))) {
    if (-not (Test-Path -LiteralPath $pasta)) {
        New-Item -ItemType Directory -Path $pasta -Force | Out-Null
    }
}

if (Test-Path -LiteralPath $Staging) {
    Remove-Item -LiteralPath $Staging -Recurse -Force
}

dotnet publish $projeto -c Release -o $Staging --nologo
if ($LASTEXITCODE -ne 0) { throw 'A publicação não foi concluída.' }

$offline = Join-Path $destinoCompleto 'app_offline.htm'
Set-Content -LiteralPath $offline -Value 'Atualização em andamento.' -Encoding utf8
try {
    Start-Sleep -Seconds 2
    robocopy $Staging $destinoCompleto /E /R:2 /W:1 `
        /XF appsettings.json appsettings.Development.json appsettings.Production.json web.config app_offline.htm `
        /XD Data Keys Logs Backups
    if ($LASTEXITCODE -gt 7) { throw "Falha ao copiar a publicação. Código: $LASTEXITCODE" }
}
finally {
    if (Test-Path -LiteralPath $offline) { Remove-Item -LiteralPath $offline -Force }
}

Write-Host "Publicação concluída em $destinoCompleto"
Write-Host "Dados preservados em $(Join-Path $raizProducao 'Data')"
