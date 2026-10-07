@echo off
chcp 65001 >nul
set "SRC=%~dp0dist\EventSL"
set "DST=%USERPROFILE%\AppData\LocalLow\Shuxi\LONESTAR\Mods\Dev\EventSL"

echo [1/2] Building EventSL...
dotnet build "%~dp0EventSL.csproj" -c Release
if %errorlevel% neq 0 (
  echo [ERROR] Build failed!
  pause
  exit /b %errorlevel%
)

echo.
echo [2/2] Preparing dist folder...
if not exist "%SRC%" mkdir "%SRC%"
copy /Y "%~dp0bin\Release\EventSL.dll" "%SRC%\" >nul
copy /Y "%~dp0mod.json" "%SRC%\" >nul

echo.
echo Installing EventSL to Dev mods...
echo   from: %SRC%
echo   to  : %DST%
echo.

xcopy "%SRC%" "%DST%" /E /I /Y

if %errorlevel%==0 (
  echo.
  echo [OK] EventSL installed successfully!
  echo Dev mods are always enabled - just launch the game.
  echo If the game is currently running, please restart it.
) else (
  echo.
  echo [FAILED] Could not copy files. Please close the game and try again.
)
echo.
pause
