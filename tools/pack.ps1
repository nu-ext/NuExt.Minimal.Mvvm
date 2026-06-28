$outDir = $PSScriptRoot

$tasks = @(
    @{ Project = "..\src\NuExt.Minimal.Mvvm.csproj";        Config = "Release" },
    @{ Project = "..\src\NuExt.Minimal.Mvvm.csproj";        Config = "Sources" },
    @{ Project = "..\src\NuExt.Minimal.Mvvm.Legacy.csproj"; Config = "Release" }
)

foreach ($t in $tasks) {
    $proj = Join-Path $PSScriptRoot $t.Project
    $cfg  = $t.Config

    Write-Host "==> $proj ($cfg)"

    dotnet clean $proj -c $cfg
    dotnet pack  $proj -c $cfg -o $outDir
}