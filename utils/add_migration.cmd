@echo off
set /p migration_name=Enter migration name: 
dotnet ef migrations add %migration_name% --startup-project ../TekkenFrameData.Backend/TekkenFrameData.Watcher/TekkenFrameData.Watcher.csproj --project ../TekkenFrameData.Backend/TekkenFrameData.Library/TekkenFrameData.Library.csproj
pause