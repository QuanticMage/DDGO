@echo off
setlocal

dotnet publish -c Release

REM ===== CONFIGURE THESE PATHS =====
set SOURCE=bin\Release\net8.0\publish\wwwroot
set TARGET=..\DDGOPublish

REM ===== SAFETY CHECK =====
if "%TARGET%"=="" (
    echo ERROR: TARGET path is empty
    exit /b 1
)

REM ===== DELETE TARGET IF IT EXISTS =====
if exist "%TARGET%" (
    echo Deleting existing directory: %TARGET%
    rmdir /s /q "%TARGET%"
)

REM ===== RECREATE TARGET =====
echo Creating directory: %TARGET%
mkdir "%TARGET%"

REM ===== COPY FILES =====
echo Copying files from %SOURCE% to %TARGET%
xcopy "%SOURCE%\*" "%TARGET%\" /E /I /Y /H

echo.
echo Dep

