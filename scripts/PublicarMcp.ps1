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
    pelo mesmo motivo: feche-a antes, ou use -EncerrarServidores.
.PARAMETER EncerrarServidores
    Antes de publicar, encerra os processos do servidor que executam esta cópia
    publicada. As sessões que os iniciaram continuam abertas e só perdem as
    ferramentas do servidor até reconectar. Uma chamada de ferramenta em andamento
    é interrompida.
.EXAMPLE
    .\PublicarMcp.ps1
.EXAMPLE
    .\PublicarMcp.ps1 -EncerrarServidores
#>

[CmdletBinding()]
param(
    [switch]$EncerrarServidores
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$projectPath = Join-Path $rootDir "src/ControleFinanceiro.Mcp/ControleFinanceiro.Mcp.csproj"
$outputDir = Join-Path $rootDir "artifacts/mcp"
$dllPublicada = [System.IO.Path]::GetFullPath((Join-Path $outputDir "ControleFinanceiro.Mcp.dll"))

# O cliente pode passar a DLL por caminho relativo (o .mcp.json passa), então a linha de comando não
# identifica a cópia. O módulo carregado no processo traz o caminho absoluto.
function Get-ServidoresDaCopia {
    Get-Process -Name dotnet -ErrorAction SilentlyContinue | Where-Object {
        try {
            $_.Modules | Where-Object { [string]::Equals($_.FileName, $dllPublicada, [StringComparison]::OrdinalIgnoreCase) }
        }
        catch {
            # Processo de outro usuário ou elevado, ou que terminou no meio da leitura: não é desta cópia.
            $false
        }
    }
}

if ($EncerrarServidores) {
    $servidores = @(Get-ServidoresDaCopia)

    if ($servidores.Count -eq 0) {
        Write-Host "Nenhum servidor MCP executando $dllPublicada." -ForegroundColor Cyan
    }

    foreach ($servidor in $servidores) {
        Write-Host "Encerrando o servidor MCP (PID $($servidor.Id), iniciado em $($servidor.StartTime))." -ForegroundColor Yellow
        # Pelo objeto já conferido, e não pelo PID, que o Windows pode dar a outro processo se este sair antes.
        if (-not $servidor.HasExited) {
            try { $servidor.Kill() } catch [System.InvalidOperationException] { }
        }
        $servidor.WaitForExit()
    }

    if ($servidores.Count -gt 0) {
        Write-Host "Reconecte o servidor nas sessões: /mcp no Claude Code; no Claude Desktop, reinicie o app pela bandeja." -ForegroundColor Yellow
    }
}

Write-Host "Publicando o servidor MCP em $outputDir ..." -ForegroundColor Cyan
dotnet publish $projectPath --configuration Release --output $outputDir

if ($LASTEXITCODE -ne 0) {
    if (-not $EncerrarServidores -and @(Get-ServidoresDaCopia).Count -gt 0) {
        Write-Host "Há sessões usando o servidor e travando os arquivos. Feche-as ou rode de novo com -EncerrarServidores." -ForegroundColor Yellow
    }

    Write-Error "Falha na publicação do servidor MCP. Código de saída: $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "Servidor publicado: $dllPublicada" -ForegroundColor Green
