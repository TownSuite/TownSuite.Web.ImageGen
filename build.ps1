#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$CURRENTPATH = $pwd.Path

$VERSION = cat Directory.Build.props | grep "<Version>"  | sed 's/[^0-9.]*//g' 
rm -rf build
mkdir -p build
$GITHASH = "$(git rev-parse --short HEAD)"
$GITHASH_FULL = "$(git rev-parse HEAD)"
Add-Content "$CURRENTPATH/build/parameterproperties.txt" "VERSION=$version"
Add-Content "$CURRENTPATH/build/parameterproperties.txt" "GITHASH=$GITHASH"
Add-Content "$CURRENTPATH/build/parameterproperties.txt" "GITHASH_FULL=$GITHASH_FULL"

$builderName = "townsuite-builder"
docker buildx inspect $builderName 2>$null
if ($LASTEXITCODE -eq 0) {
    docker buildx use $builderName
} else {
    docker buildx create --name $builderName --driver docker-container --use | Out-Null
}
docker buildx inspect $builderName --bootstrap
mkdir -p $CURRENTPATH/build/oci

Write-Host "Building townsuite/imagegen:latest" -ForegroundColor Green
docker buildx build -f $CURRENTPATH/TownSuite.Web.ImageGen/Dockerfile --progress plain --provenance=true --sbom=true --output "type=oci,name=townsuite/imagegen,oci-mediatypes=true,compression=zstd,force-compression=true,tar=false,dest=$CURRENTPATH/build/oci/imagegen" .

tar -cvf $CURRENTPATH/build/oci/imagegen.oci.tar -C $CURRENTPATH/build/oci/imagegen --transform='s|^\./||' .
rm -rf $CURRENTPATH/build/oci/imagegen
Write-Host "Finished imagegen.oci.tar" -ForegroundColor Green
