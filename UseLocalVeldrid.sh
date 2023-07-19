#!/bin/sh

# Run this script to use a local copy of veldrid rather than fetching it from nuget.
# It expects the veldrid directory to be at the same level as the osu-framework directory
#
# https://github.com/ppy/osu-framework/wiki/Testing-local-framework-checkout-with-other-projects

FRAMEWORK_CSPROJ="osu.Framework/osu.Framework.csproj"
SLN="osu-framework.sln"

dotnet remove $FRAMEWORK_CSPROJ reference ppy.Veldrid

dotnet sln $SLN add ../veldrid/src/Veldrid/Veldrid.csproj \
    ../veldrid/src/Veldrid.MetalBindings/Veldrid.MetalBindings.csproj \
    ../veldrid/src/Veldrid.OpenGLBindings/Veldrid.OpenGLBindings.csproj

dotnet add $FRAMEWORK_CSPROJ reference ../veldrid/src/Veldrid/Veldrid.csproj

tmp=$(mktemp)

jq '.solution.projects += ["../veldrid/src/Veldrid/Veldrid.csproj", "../veldrid/src/Veldrid.MetalBindings/Veldrid.MetalBindings.csproj", "../veldrid/src/Veldrid.OpenGLBindings/Veldrid.OpenGLBindings.csproj"]' osu-framework.Desktop.slnf > $tmp
mv -f $tmp osu-framework.Desktop.slnf

jq '.solution.projects += ["../veldrid/src/Veldrid/Veldrid.csproj", "../veldrid/src/Veldrid.MetalBindings/Veldrid.MetalBindings.csproj", "../veldrid/src/Veldrid.OpenGLBindings/Veldrid.OpenGLBindings.csproj"]' osu-framework.Android.slnf > $tmp
mv -f $tmp osu-framework.Android.slnf

jq '.solution.projects += ["../veldrid/src/Veldrid/Veldrid.csproj", "../veldrid/src/Veldrid.MetalBindings/Veldrid.MetalBindings.csproj", "../veldrid/src/Veldrid.OpenGLBindings/Veldrid.OpenGLBindings.csproj"]' osu-framework.iOS.slnf > $tmp
mv -f $tmp osu-framework.iOS.slnf
