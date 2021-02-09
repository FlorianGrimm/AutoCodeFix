@SET slnroot=%~dp0
@CD /D %slnroot%
@ECHO slnroot: %slnroot%

@ECHO .
dotnet run --project src\AutoVersion

@ECHO .
dotnet build /restore

@ECHO .
CD /D %slnroot%src\AutoCodeFixer\

@ECHO .
dotnet pack

@ECHO .
@CD /D %slnroot%

@ECHO .
dotnet tool update --global --add-source nupkg AutoCodeFixer

@REM IF ERRORLEVEL 1 dotnet tool install --global --add-source nupkg AutoCodeFixer

GOTO :EOF

REM install the tool to a default location
dotnet tool install --global --add-source nupkg AutoCodeFixer

dotnet tool update --global --add-source nupkg AutoCodeFixer

dotnet tool list --global

dotnet tool uninstall --global AutoCodeFixer

REM  install the tool to a custom location
dotnet tool install --add-source nupkg AutoCodeFixer --tool-path .\tools