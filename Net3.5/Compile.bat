@echo off
:: Markus Scholtes, 2019
:: Compile GetAllEvents in .Net 3.5x environment
setlocal

set COMPILER=%WINDIR%\Microsoft.NET\Framework\v3.5\csc.exe
if NOT EXIST "%COMPILER%" echo C# compiler not found&goto :READY

"%COMPILER%" /target:exe "%~dp0GetAllEvents.cs" /win32icon:"%~dp0MScholtes.ico"

:READY
:: was batch started in Windows Explorer? Yes, then pause
echo "%CMDCMDLINE%" | find /i "/c" > nul
if %ERRORLEVEL%==0 pause
