#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$CURRENTPATH=$pwd.Path

# Must install powershell:  https://learn.microsoft.com/en-us/powershell/scripting/install/install-ubuntu?view=powershell-7.2

if (Test-Path "libheif-build") {
    Remove-Item "libheif-build" -Recurse -Force
}
else {
    New-Item "libheif-build" -ItemType Directory
}

git clone -q --depth 1 "https://github.com/microsoft/vcpkg" libheif-build

Start-Process -FilePath ".\libheif-build\bootstrap-vcpkg.bat" -ArgumentList "-disableMetrics" -Wait -NoNewWindow

Start-Process -FilePath ".\libheif-build\vcpkg.exe" -ArgumentList "install aom:x64-windows libde265:x64-windows x265:x64-windows libheif:x64-windows" -Wait -NoNewWindow 

Rename-Item -Path ".\libheif-build\installed\x64-windows\bin\heif.dll" -NewName "libheif.dll"

Copy-Item -Path ".\libheif-build\installed\x64-windows\bin\*.dll" -Destination ".\TownSuite.Web.ImageGen\bin\Debug\net6.0"

Copy-Item -Path ".\libheif-build\installed\x64-windows\bin\*.dll" -Destination ".\TownSuite.Web.ImageGen.Tests\bin\Debug\net6.0"

Remove-Item "libheif-build" -Recurse -Force