#!/bin/sh

# Run this script to use a local copy of OpenTabletDriver rather than fetching it from nuget.
# It expects the OpenTabletDriver directory to be at the same level as the osu-framework directory
#
# https://github.com/ppy/osu-framework/wiki/Testing-local-framework-checkout-with-other-projects

FRAMEWORK_CSPROJ="osu.Framework/osu.Framework.csproj"
SLN="osu-framework.sln"

dotnet remove $FRAMEWORK_CSPROJ reference OpenTabletDriver

dotnet sln $SLN add ../OpenTabletDriver/OpenTabletDriver/OpenTabletDriver.csproj

dotnet add $FRAMEWORK_CSPROJ reference ../OpenTabletDriver/OpenTabletDriver/OpenTabletDriver.csproj

tmp=$(mktemp)

jq '.solution.projects += ["../OpenTabletDriver/OpenTabletDriver/OpenTabletDriver.csproj"]' osu-framework.Desktop.slnf > $tmp
mv -f $tmp osu-framework.Desktop.slnf
