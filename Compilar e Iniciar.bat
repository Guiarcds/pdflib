@echo off
echo ==========================================
echo  Organizador de Documentos Financeiros
echo  Compilando o projeto...
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

dotnet build src\OrganizadorDocumentos.slnx --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERRO na compilacao!
    pause
    exit /b 1
)

echo.
echo Compilacao concluida com sucesso!
echo.
echo Iniciando o aplicativo...
echo.
start "" "bin\OrganizadorDocumentos.UI.exe"