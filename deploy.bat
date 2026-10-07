@echo off
set "DEV_DIR=%USERPROFILE%\AppData\LocalLow\Shuxi\LONESTAR\Mods\Dev\EventSL"
set "DIST_DIR=d:\AI project\EventSL\dist\EventSL"

if not exist "%DEV_DIR%" mkdir "%DEV_DIR%"
if not exist "%DIST_DIR%" mkdir "%DIST_DIR%"

copy /y "d:\AI project\EventSL\bin\Release\EventSL.dll" "%DEV_DIR%\"
copy /y "d:\AI project\EventSL\mod.json" "%DEV_DIR%\"

copy /y "d:\AI project\EventSL\bin\Release\EventSL.dll" "%DIST_DIR%\"
copy /y "d:\AI project\EventSL\mod.json" "%DIST_DIR%\"

echo Successfully copied files!
dir "%DEV_DIR%"
dir "%DIST_DIR%"
