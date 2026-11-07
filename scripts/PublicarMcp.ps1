<#
.SYNOPSIS
    Publica o servidor MCP em artifacts/mcp, de onde os clientes MCP o executam.
.DESCRIPTION
    O cliente MCP executa uma cópia publicada, e não a saída de bin/: no Windows o
    processo do servidor trava a DLL carregada, e um dotnet build com uma sessão
    aberta falharia. O dotnet run também não serve, porque escreve a saída do build
    no stdout, que é do protocolo.

    Rode de novo depois de mudar o servidor e reinicie o servidor no cliente
    (/mcp no Claude Code). Com uma sessão usando o servidor, a publicação falha
    pelo mesmo motivo: feche-a antes.
.EXAMPLE
    .\PublicarMcp.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$projectPath = Join-Path $rootDir "src/ControleFinanceiro.Mcp/ControleFinanceiro.Mcp.csproj"
$outputDir = Join-Path $rootDir "artifacts/mcp"

Write-Host "Publicando o servidor MCP em $outputDir ..." -ForegroundColor Cyan
dotnet publish $projectPath --configuration Release --output $outputDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na publicação do servidor MCP. Código de saída: $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Servidor publicado: $(Join-Path $outputDir 'ControleFinanceiro.Mcp.dll')" -ForegroundColor Green
