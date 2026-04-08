param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "build", "test", "lint", "format", "clean")]
    [string]$Task
)

$ErrorActionPreference = "Stop"
$solution = "ProFootball.slnx"

switch ($Task) {
    "dev" {
        dotnet run --project .\src\ProFootball.Presentation\ProFootball.Presentation.csproj
    }
    "build" {
        dotnet build $solution --configuration Debug
    }
    "test" {
        dotnet test $solution --configuration Debug
    }
    "lint" {
        dotnet format --verify-no-changes $solution
    }
    "format" {
        dotnet format $solution
    }
    "clean" {
        dotnet clean $solution
        if (Test-Path .\tests\ProFootball.Application.Tests\TestResults) {
            Remove-Item -Recurse -Force .\tests\ProFootball.Application.Tests\TestResults
        }
    }
}
