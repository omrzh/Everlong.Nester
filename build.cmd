@echo off
set "SCRIPT_ROOT=%~dp0."
if "%~1"=="" (
  dotnet run --project "%SCRIPT_ROOT%\_build\_build.csproj" -- --root "%SCRIPT_ROOT%"
) else (
  set "FIRST_ARG=%~1"
  if "%FIRST_ARG:~0,1%"=="-" (
    dotnet run --project "%SCRIPT_ROOT%\_build\_build.csproj" -- --root "%SCRIPT_ROOT%" %*
  ) else (
    dotnet run --project "%SCRIPT_ROOT%\_build\_build.csproj" -- --root "%SCRIPT_ROOT%" --target %*
  )
)
