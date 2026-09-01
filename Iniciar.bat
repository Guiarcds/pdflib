@echo off
echo ==========================================
echo  Organizador de Documentos Financeiros
echo ==========================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo .NET SDK nao encontrado. Instalando...
    echo.
    if exist "%~dp0dotnet-sdk-10.0.400-win-x64.exe" (
        start /wait "" "%~dp0dotnet-sdk-10.0.400-win-x64.exe" /quiet /norestart
    ) else (
        echo ERRO: Instalador do SDK nao encontrado no diretorio do projeto!
        pause
        exit /b 1
    )

    where dotnet >nul 2>nul
    if %ERRORLEVEL% NEQ 0 (
        echo ERRO: Apos a instalacao o dotnet ainda nao foi detectado.
        echo Feche e abra este script novamente.
        pause
        exit /b 1
    )
)

if not exist "bin\OrganizadorDocumentos.UI.exe" (
    echo ERRO: Executavel nao encontrado!
    echo.
    echo Compile o projeto primeiro com:
    echo   dotnet build src\OrganizadorDocumentos.slnx
    echo.
    pause
    exit /b 1
)

echo Iniciando o aplicativo...
echo.
start "" "bin\OrganizadorDocumentos.UI.exe"