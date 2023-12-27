#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$CURRENTPATH = $pwd.Path

# Must install powershell:  https://learn.microsoft.com/en-us/powershell/scripting/install/install-ubuntu?view=powershell-7.2

$VERSION = cat TownSuite.Web.ImageGen/TownSuite.Web.ImageGen.csproj | grep "<Version>"  | sed 's/[^0-9.]*//g' 
rm -rf build
mkdir -p build
$GITHASH = "$(git rev-parse --short HEAD)"
echo "$GITHASH" >> build/githash.txt

Write-Host "Building townsuite/imagegen:latest" -ForegroundColor Green
docker build -f "$CURRENTPATH/TownSuite.Web.ImageGen/Dockerfile" -t townsuite/imagegen$GITHASH -m 4GB .


Write-Host "Building imagegen.tar" -ForegroundColor Green
docker save townsuite/imagegen$GITHASH -o "$CURRENTPATH/build/imagegen.tar"
docker rmi "townsuite/imagegen$GITHASH"
Write-Host "Finished imagegen.tar" -ForegroundColor Green

