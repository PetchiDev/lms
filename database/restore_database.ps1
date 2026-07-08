# Restore CareTrack LMS schema + data into a NEW PostgreSQL database
#
# Examples:
#   .\database\restore_database.ps1 -DatabaseName lms_copy
#   .\database\restore_database.ps1 -DatabaseName lms_staging -Host 10.0.0.5 -Password "secret"
#
# With Docker Compose (caretrack-postgres container):
#   docker compose up -d
#   .\database\restore_database.ps1 -DatabaseName lms_copy -UseDockerContainer

param(
    [Parameter(Mandatory = $true)]
    [string]$DatabaseName,

    [string]$Host = "localhost",
    [int]$Port = 5432,
    [string]$User = "postgres",
    [string]$Password = "Password@1",
    [switch]$UseDockerContainer,
    [string]$DockerContainer = "caretrack-postgres",
    [switch]$UseFullScript
)

$ErrorActionPreference = "Stop"
$schemaFile = Join-Path $PSScriptRoot "caretrack_schema.sql"
$dataFile = Join-Path $PSScriptRoot "caretrack_data.sql"
$fullFile = Join-Path $PSScriptRoot "caretrack_full.sql"
$env:PGPASSWORD = $Password

$schemaReset = @"
-- Clean slate: drop any existing tables (e.g. from a prior EF migration run)
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

"@

function Invoke-SqlFile {
    param([string]$Database, [string]$File)

    if ($UseDockerContainer) {
        Get-Content $File -Raw | docker exec -i $DockerContainer psql -v ON_ERROR_STOP=1 -U $User -d $Database
        return
    }

    if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
        throw "psql not found. Install PostgreSQL client or pass -UseDockerContainer with docker compose running."
    }

    & psql -h $Host -p $Port -U $User -d $Database -v ON_ERROR_STOP=1 -f $File
}

function Invoke-SqlCommand {
    param([string]$Database, [string]$Command)

    if ($UseDockerContainer) {
        docker exec -i $DockerContainer psql -v ON_ERROR_STOP=1 -U $User -d $Database -c $Command | Out-Null
        return
    }

    & psql -h $Host -p $Port -U $User -d $Database -v ON_ERROR_STOP=1 -c $Command | Out-Null
}

Write-Host "Target: database '$DatabaseName'" -ForegroundColor Cyan
Write-Host "Stop the CareTrack API before restoring." -ForegroundColor Yellow

try {
    Invoke-SqlCommand -Database "postgres" -Command "CREATE DATABASE `"$DatabaseName`";"
    Write-Host "Created database '$DatabaseName'." -ForegroundColor Green
}
catch {
    Write-Host "Database '$DatabaseName' may already exist — continuing." -ForegroundColor Yellow
}

if ($UseFullScript) {
    if (-not (Test-Path $fullFile)) {
        throw "Full script not found: $fullFile"
    }

    Write-Host "Applying full dump ($fullFile)..." -ForegroundColor Cyan
    Invoke-SqlFile -Database $DatabaseName -File $fullFile
}
else {
    Write-Host "Applying schema ($schemaFile)..." -ForegroundColor Cyan
    Invoke-SqlFile -Database $DatabaseName -File $schemaFile

    Write-Host "Loading data ($dataFile)..." -ForegroundColor Cyan
    Invoke-SqlFile -Database $DatabaseName -File $dataFile
}

Write-Host ""
Write-Host "Restore complete." -ForegroundColor Green
Write-Host "Connection string:"
Write-Host "  Host=$Host;Port=$Port;Database=$DatabaseName;Username=$User;Password=$Password"
