@echo off
echo ==========================================
echo  Organizador de Documentos Financeiros
echo  Compilando o projeto...
echo ==========================================
echo.

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
