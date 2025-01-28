@echo off
set /p migration_name=Enter migration name: 
dotnet ef migrations add %migration_name% --startup-project ../TekkenFrameData.Backend/TekkenFrameData.Service/TekkenFrameData.Service.csproj --project ../TekkenFrameData.Backend/TekkenFrameData.Library/TekkenFrameData.Library.csproj
pause