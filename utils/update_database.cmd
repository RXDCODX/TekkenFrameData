@echo off
dotnet ef database update --startup-project ../TekkenFrameData.Backend/TekkenFrameData.Service/TekkenFrameData.Service.csproj --project ../TekkenFrameData.Backend/TekkenFrameData.Library/TekkenFrameData.Library.csproj
pause