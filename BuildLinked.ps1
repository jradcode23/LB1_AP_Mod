# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/LB1_AP_Mod/*" -Force -Recurse
dotnet publish "./LB1_AP_Mod.csproj" -c Release -o "$env:RELOADEDIIMODS/LB1_AP_Mod" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location