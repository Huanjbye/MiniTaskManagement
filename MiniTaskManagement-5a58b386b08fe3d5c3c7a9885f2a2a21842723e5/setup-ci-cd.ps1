$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

Write-Host "Checking Docker Desktop..."
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is not installed or not available in PATH. Install Docker Desktop first."
    exit 1
}

try {
    docker info | Out-Null
} catch {
    Write-Error "Docker daemon is not running. Start Docker Desktop and try again."
    exit 1
}

Write-Host "Starting SonarQube and PostgreSQL..."
docker compose -f docker-compose.sonarqube.yml up -d

Write-Host "Waiting for SonarQube to become ready..."
Start-Sleep -Seconds 20

try {
    $health = Invoke-RestMethod -Uri "http://localhost:9000/api/system/health" -Method Get
    Write-Host "SonarQube health: $($health.status)"
} catch {
    Write-Warning "SonarQube may still be starting. Open http://localhost:9000 after a few minutes."
}

Write-Host ""
Write-Host "Login: admin / admin"
Write-Host "Open: http://localhost:9000"
Write-Host "Create a project token under My Account -> Security"
