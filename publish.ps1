#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$CURRENTPATH = $pwd.Path

# Must install powershell:  https://learn.microsoft.com/en-us/powershell/scripting/install/install-ubuntu?view=powershell-7.2
# Must install google crane:  https://github.com/google/go-containerregistry/releases

$GITHASH = cat build/githash.txt

crane push "$CURRENTPATH/build/imagegen.tar" townsuite/imagegen:latest
crane push "$CURRENTPATH/build/imagegen.tar" townsuite/imagegen:${GITHASH}


