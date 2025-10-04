$ErrorActionPreference = "Stop"
Write-Host "Building WPF app..."
dotnet build ..\..\src\Control.Wpf\Control.Wpf.csproj -c Debug
Write-Host "Running WPF UI tests (headed)..."
dotnet test .\Control.UiTests.Wpf.csproj -c Debug
