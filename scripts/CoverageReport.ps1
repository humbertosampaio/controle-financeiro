<#
.SYNOPSIS
    Executa os testes da solution com cobertura (Coverlet) e gera relatório HTML.
.DESCRIPTION
    Roda dotnet test com coleta de cobertura e usa ReportGenerator para gerar
    um relatório HTML na pasta coveragereport, abrindo-o no navegador.
.EXAMPLE
    .\CoverageReport.ps1
    .\CoverageReport.ps1 -OpenReport
#>

[CmdletBinding()]
param(
    [switch]$OpenReport = $true
)

$ErrorActionPreference = "Stop"

# Diretório raiz do repositório (onde está a solution)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$solutionPath = Join-Path $rootDir "ControleFinanceiro.slnx"
$runsettingsPath = Join-Path $rootDir "coverlet.runsettings"
$testResultsDir = Join-Path $rootDir "TestResults"
$reportDir = Join-Path $rootDir "coveragereport"

Push-Location $rootDir
try {
    if (Test-Path $testResultsDir) {
        Write-Host "Limpando diretório de resultados anteriores em $testResultsDir ..." -ForegroundColor DarkGray
        Remove-Item -Recurse -Force -Path $testResultsDir
    }

    Write-Host "Executando testes com cobertura de código (excluindo projetos de teste)..." -ForegroundColor Cyan
    dotnet test $solutionPath `
        --collect:"XPlat Code Coverage" `
        --results-directory $testResultsDir `
        --settings $runsettingsPath

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha na execução dos testes. Código de saída: $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    $coverageXml = Get-ChildItem -Path $testResultsDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $coverageXml) {
        Write-Warning "Nenhum arquivo coverage.cobertura.xml encontrado em $testResultsDir"
        Write-Host "Tentando com padrão opencover..."
        $coverageXml = Get-ChildItem -Path $testResultsDir -Recurse -Filter "coverage.opencover.xml" -ErrorAction SilentlyContinue | Select-Object -First 1
    }
    if (-not $coverageXml) {
        $anyCoverage = Get-ChildItem -Path $testResultsDir -Recurse -Filter "*.xml" -ErrorAction SilentlyContinue
        if ($anyCoverage) {
            $coverageXml = $anyCoverage | Select-Object -First 1
            Write-Host "Usando: $($coverageXml.FullName)"
        }
    }

    if (-not $coverageXml) {
        Write-Error "Nenhum arquivo de cobertura encontrado em $testResultsDir"
        exit 1
    }

    Write-Host "Arquivo de cobertura: $($coverageXml.FullName)" -ForegroundColor Gray

    # ReportGenerator: verifica se está instalado
    $reportGenerator = Get-Command reportgenerator -ErrorAction SilentlyContinue
    if (-not $reportGenerator) {
        Write-Host "Instalando dotnet-reportgenerator-globaltool..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-reportgenerator-globaltool
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Falha ao instalar ReportGenerator. Instale manualmente: dotnet tool install -g dotnet-reportgenerator-globaltool"
            exit 1
        }
    }

    Write-Host "Gerando relatório HTML em $reportDir ..." -ForegroundColor Cyan
    $coveragePattern = Join-Path $testResultsDir "**\coverage.cobertura.xml"
    reportgenerator "-reports:$coveragePattern" "-targetdir:$reportDir" "-reporttypes:Html" "-verbosity:Warning"

    if (-not (Test-Path (Join-Path $reportDir "index.html"))) {
        Write-Host "Gerando relatório a partir do arquivo encontrado..." -ForegroundColor Gray
        reportgenerator "-reports:$($coverageXml.FullName)" "-targetdir:$reportDir" "-reporttypes:Html" "-verbosity:Warning"
    }

    $indexHtml = Join-Path $reportDir "index.html"
    if (Test-Path $indexHtml) {
        Write-Host "Relatório gerado: $indexHtml" -ForegroundColor Green
        if ($OpenReport) {
            Start-Process $indexHtml
        }
    }
    else {
        Write-Warning "index.html não encontrado em $reportDir"
    }
}
finally {
    Pop-Location
}
