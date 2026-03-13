@echo off
setlocal enabledelayedexpansion

:: ============================================================
:: STBWeb — Azure Kudu deployment script
:: Compiles the project with dotnet publish, then syncs the
:: publish output to the deployment target while preserving the
:: umbraco/Data and umbraco/Logs directories that live on the
:: Azure file-system (SQLite DB, log files, etc.).
:: ============================================================

:: Kudu sets these environment variables automatically.
:: DEPLOYMENT_SOURCE  = repo root on the Kudu VM
:: DEPLOYMENT_TARGET  = wwwroot (or the site root Azure deploys to)
:: KUDU_SYNC_CMD      = path to KuduSync.exe

IF NOT DEFINED DEPLOYMENT_SOURCE (
    SET DEPLOYMENT_SOURCE=%~dp0
)

IF NOT DEFINED DEPLOYMENT_TARGET (
    SET DEPLOYMENT_TARGET=%~dp0..\artifacts\wwwroot
)

IF NOT DEFINED KUDU_SYNC_CMD (
    CALL :SelectKuduSync
    IF !ERRORLEVEL! NEQ 0 GOTO KUDU_SYNC_NOT_FOUND
)

IF NOT DEFINED NEXT_MANIFEST_PATH (
    SET NEXT_MANIFEST_PATH=%DEPLOYMENT_TARGET%\.manifest
)

IF NOT DEFINED PREVIOUS_MANIFEST_PATH (
    SET PREVIOUS_MANIFEST_PATH=%DEPLOYMENT_TARGET%\.manifest
)

SET PUBLISH_OUTPUT=%DEPLOYMENT_SOURCE%\publish_output

:: ---------------------------------------------------------------
:: 1. dotnet publish
:: ---------------------------------------------------------------
echo.
echo --- dotnet publish -c Release ---
dotnet publish "%DEPLOYMENT_SOURCE%\STBWeb.csproj" ^
    -c Release ^
    -o "%PUBLISH_OUTPUT%"
IF !ERRORLEVEL! NEQ 0 (
    echo ERROR: dotnet publish failed.
    GOTO FAILED
)

:: ---------------------------------------------------------------
:: 2. KuduSync — copy publish output to deployment target,
::    skipping umbraco/Data (SQLite DB) and umbraco/Logs.
:: ---------------------------------------------------------------
echo.
echo --- KuduSync to %DEPLOYMENT_TARGET% ---
CALL %KUDU_SYNC_CMD% ^
    -v 50 ^
    -f "%PUBLISH_OUTPUT%" ^
    -t "%DEPLOYMENT_TARGET%" ^
    -n "%NEXT_MANIFEST_PATH%" ^
    -p "%PREVIOUS_MANIFEST_PATH%" ^
    -i ".git;.hg;.deployment;deploy.cmd;umbraco\Data;umbraco\Logs"
IF !ERRORLEVEL! NEQ 0 GOTO FAILED

:: ---------------------------------------------------------------
:: 3. Clean up the temporary publish output folder
:: ---------------------------------------------------------------
IF EXIST "%PUBLISH_OUTPUT%" (
    rmdir /s /q "%PUBLISH_OUTPUT%"
)

echo.
echo Deployment successful.
GOTO END

:: ---------------------------------------------------------------
:: Helpers
:: ---------------------------------------------------------------
:SelectKuduSync
IF /I "%IN_WEBSITE_ROOT%"=="true" (
    IF NOT DEFINED KUDU_SYNC_CMD (
        SET KUDU_SYNC_CMD=%APPDATA%\npm\kuduSync.cmd
        GOTO KUDU_SYNC_SELECTED
    )
)
IF EXIST "%APPDATA%\npm\kuduSync.cmd" (
    SET KUDU_SYNC_CMD=%APPDATA%\npm\kuduSync.cmd
    GOTO KUDU_SYNC_SELECTED
)
IF EXIST "%PROGRAMFILES(x86)%\npm\kuduSync.cmd" (
    SET KUDU_SYNC_CMD=%PROGRAMFILES(x86)%\npm\kuduSync.cmd
    GOTO KUDU_SYNC_SELECTED
)
:KUDU_SYNC_NOT_FOUND
echo KuduSync not found. Deployment cannot continue.
EXIT /B 1
:KUDU_SYNC_SELECTED
EXIT /B 0

:FAILED
echo.
echo Deployment FAILED.
EXIT /B 1

:END
EXIT /B 0
