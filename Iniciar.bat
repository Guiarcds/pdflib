@echo off
echo ==========================================
echo  Organizador de Documentos Financeiros
echo ==========================================
echo.

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
