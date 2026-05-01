param(
    [Parameter(Mandatory = $true)]
    [string]$SqlitePath,
    [string]$Profile = "realistic",
    [string]$Mode = "regenerate",
    [int]$BatchSize = 2000
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$infrastructureProject = Join-Path $root "src/ProFootball.Infrastructure/ProFootball.Infrastructure.csproj"
$dataLoaderProject = Join-Path $root "src/ProFootball.DataLoader/ProFootball.DataLoader.csproj"

Write-Host "[1/3] Drop database..."
dotnet ef database drop --force --project $infrastructureProject --startup-project $dataLoaderProject

Write-Host "[2/3] Apply migrations..."
dotnet ef database update --project $infrastructureProject --startup-project $dataLoaderProject

Write-Host "[3/3] Import and generate data..."
dotnet run --project $dataLoaderProject -- --sqlite "$SqlitePath" --batch-size $BatchSize --profile $Profile --mode $Mode

Write-Host "Done."
