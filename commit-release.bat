@echo off
rem Tensho Assistant: verify, commit and release in one go (double-click or run from a terminal).
rem   commit-release.bat          commit then release
rem   commit-release.bat dry      same, commands shown only (-DryRun)
rem   commit-release.bat commit   commit only
rem Exit code: 0 done, 1 error, 2 expected stop (already released, nothing new...).
setlocal
cd /d "%~dp0"

set "MODE=%~1"
set "DRY="
if /i "%MODE%"=="dry" set "DRY=-DryRun"

rem PowerShell 7 when installed, else Windows PowerShell 5.1 (the scripts support both).
set "PS=powershell.exe"
where pwsh.exe >nul 2>nul && set "PS=pwsh.exe"
set "RUN=%PS% -NoProfile -ExecutionPolicy Bypass -File scripts\Verify.ps1"

echo === Verify + commit ===
%RUN% -Commit %DRY%
set "CODE=%ERRORLEVEL%"
if not "%CODE%"=="0" goto :stopped
if /i "%MODE%"=="commit" goto :done

echo.
echo === Release ===
%RUN% -Release %DRY%
set "CODE=%ERRORLEVEL%"
if not "%CODE%"=="0" goto :stopped

:done
echo.
echo Finished.
goto :end

:stopped
echo.
if "%CODE%"=="2" (echo Stopped: see the yellow message above ^(nothing was published^).) else (echo FAILED with code %CODE%: see the messages above.)

:end
rem Keep the window open when started by double-click.
echo %CMDCMDLINE% | find /i "/c" >nul && pause
endlocal & exit /b %CODE%
