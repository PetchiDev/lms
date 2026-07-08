# Re-export CareTrack database to database/caretrack_data.sql
# Requires PostgreSQL running (docker compose up -d)

param(
    [string]$ConnectionString = "Host=localhost;Port=5432;Database=lms;Username=postgres;Password=Password@1",
    [string]$OutputFile = "$PSScriptRoot\caretrack_data.sql"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$schemaFile = Join-Path $PSScriptRoot "caretrack_schema.sql"
$fullFile = Join-Path $PSScriptRoot "caretrack_full.sql"

Push-Location $repoRoot
try {
    Write-Host "Exporting schema..." -ForegroundColor Cyan
    dotnet ef migrations script --idempotent `
        --project src/CareTrack.Infrastructure/CareTrack.Infrastructure.csproj `
        --startup-project src/CareTrack.Api/CareTrack.Api.csproj `
        --output $schemaFile | Out-Null

    Write-Host "Exporting data..." -ForegroundColor Cyan
    dotnet run --project tools/CareTrack.DbExport/CareTrack.DbExport.csproj -- $OutputFile

    $generated = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $schemaReset = @"
-- Clean slate: drop any existing tables (e.g. from a prior EF migration run)
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

"@
    $schema = [System.IO.File]::ReadAllText($schemaFile)
    if (-not $schema.StartsWith("DROP SCHEMA IF EXISTS public CASCADE")) {
        $schema = $schemaReset + $schema
        [System.IO.File]::WriteAllText($schemaFile, $schema, $utf8NoBom)
    }
    $data = [System.IO.File]::ReadAllText($OutputFile)
    $tableCount = ([regex]::Matches($data, '(?m)^-- Table: ')).Count
    $header = @"
-- =============================================================================
-- CareTrack LMS - FULL DATABASE SCRIPT (schema + data)
-- Generated: $generated
-- Tables: $tableCount (schema + all row data)
--
-- Restore (new database):
--   1. CREATE DATABASE your_db;
--   2. Stop the API (do NOT run dotnet ef database update first)
--   3. psql -U postgres -d your_db -v ON_ERROR_STOP=1 -f caretrack_full.sql
--
-- Or use: .\database\restore_database.ps1 -DatabaseName your_db -UseFullScript
-- =============================================================================

"@
    $combined = $header + $schema.TrimEnd() + "`r`n`r`n`r`n-- =============================================================================`r`n-- DATA`r`n-- =============================================================================`r`n`r`n" + $data.TrimEnd() + "`r`n"
    [System.IO.File]::WriteAllText($fullFile, $combined, $utf8NoBom)

    Write-Host "Data exported to $OutputFile" -ForegroundColor Green
    Write-Host "Full dump updated: $fullFile" -ForegroundColor Green
}
finally {
    Pop-Location
}
