@echo off
setlocal

set "ROOT=%~dp0"
set "SRC=%ROOT%src\OneNoteExporter.csproj"
set "DIST=%ROOT%dist"
set "EXPORTER=%ROOT%..\OneNoteMdExporter.v1.6.0"

if "%1"=="launch" goto :launch

echo === OneNoteExporter build ===

if not exist "%EXPORTER%\OneNoteMdExporter.exe" (
    echo [ERR] OneNoteMdExporter.exe not found at: %EXPORTER%
    pause
    exit /b 1
)

echo [1/3] dotnet publish ...
dotnet publish "%SRC%" -c Release -r win-x64 --self-contained false -o "%DIST%" >nul 2>&1
if errorlevel 1 (
    echo [ERR] build failed.
    pause
    exit /b 1
)

echo [2/3] merge files ...
copy /y "%EXPORTER%\OneNoteMdExporter.exe" "%DIST%\" >nul
if not exist "%DIST%\appSettings.json" copy /y "%EXPORTER%\appSettings.json" "%DIST%\" >nul
xcopy "%EXPORTER%\pandoc" "%DIST%\pandoc\" /E /Y /Q >nul
xcopy "%EXPORTER%\Resources" "%DIST%\Resources\" /E /Y /Q >nul
if exist "%EXPORTER%\LICENSE" copy /y "%EXPORTER%\LICENSE" "%DIST%\" >nul
if not exist "%DIST%\Exports" mkdir "%DIST%\Exports"

echo [3/3] done! starting ...
echo.

:launch
start "" "%DIST%\OneNoteExporter.exe"