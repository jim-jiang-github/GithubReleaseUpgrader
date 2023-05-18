@echo off
set "originalFolder=%1"
set "targetFolder=%2"
set "executablePath=%3"
set "needRestart=%4"
set "cpResult=1"

:loop
timeout 1 > nul
xcopy /E /I /Y "%originalFolder%\*" "%targetFolder%" > error.log 2>&1
echo "xcopy %originalFolder% to %targetFolder%"
if errorlevel 1 (
    set "cpResult=1"
) else (
    set "cpResult=0"
)

if %cpResult% equ 1 goto loop

echo "remove %originalFolder%"
rd /S /Q "%originalFolder%"

if "%needRestart%"=="True" (
    echo "ready for start"
    start "" "%executablePath%"
    timeout /T 1 > nul
) else (
    echo "No restart needed"
)

echo "ready for exit"

del "%~f0"
exit