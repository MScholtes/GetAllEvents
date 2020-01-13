@echo off
:: Markus Scholtes, 2020
:: Compile GetAllEvents in .Net 4.x environment
setlocal

set COMPILER=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if NOT EXIST "%COMPILER%" echo C# compiler not found&goto :READY

"%COMPILER%" /target:exe "%~dp0GetAllEvents.cs" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\presentationframework.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\windowsbase.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\presentationcore.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll" /win32icon:"%~dp0MScholtes.ico"
"%COMPILER%" /target:exe "%~dp0GetAllEventsCLI.cs" /win32icon:"%~dp0MScholtes.ico"

:READY
:: was batch started in Windows Explorer? Yes, then pause
echo "%CMDCMDLINE%" | find /i "/c" > nul
if %ERRORLEVEL%==0 pause
