# Run this script to use a local copy of veldrid rather than fetching it from nuget.
# It expects the veldrid directory to be at the same level as the osu-framework directory
#
# https://github.com/ppy/osu-framework/wiki/Testing-local-framework-checkout-with-other-projects

$FRAMEWORK_CSPROJ="osu.Framework/osu.Framework.csproj"
$SLN="osu-framework.sln"

dotnet remove $FRAMEWORK_CSPROJ reference ppy.Veldrid;

dotnet sln $SLN add ../veldrid/src/Veldrid/Veldrid.csproj `
    ../veldrid/src/Veldrid.MetalBindings/Veldrid.MetalBindings.csproj `
    ../veldrid/src/Veldrid.OpenGLBindings/Veldrid.OpenGLBindings.csproj;

dotnet add $FRAMEWORK_CSPROJ reference ../veldrid/src/Veldrid/Veldrid.csproj;

$TMP=New-TemporaryFile

$SLNF=Get-Content "osu-framework.Desktop.slnf" | ConvertFrom-Json
$SLNF.solution.projects += ("../veldrid/src/Veldrid/Veldrid.csproj")
$SLNF.solution.projects += ("../veldrid/src/Veldrid.OpenGLBindings/Veldrid.OpenGLBindings.csproj")
$SLNF.solution.projects += ("../veldrid/src/Veldrid.MetalBindings/Veldrid.MetalBindings.csproj")
ConvertTo-Json $SLNF | Out-File $TMP -Encoding UTF8
Move-Item -Path $TMP -Destination "osu-framework.Desktop.slnf" -Force

$SLNF=Get-Content "osu-framework.Android.slnf" | ConvertFrom-Json
$SLNF.solution.projects += ("../veldrid/src/Veldrid/Veldrid.csproj")
$SLNF.solution.projects += ("../veldrid/src/Veldrid.OpenGLBindings/Veldrid.OpenGLBindings.csproj")
$SLNF.solution.projects += ("../veldrid/src/Veldrid.MetalBindings/Veldrid.MetalBindings.csproj")
ConvertTo-Json $SLNF | Out-File $TMP -Encoding UTF8
Move-Item -Path $TMP -Destination "osu-framework.Android.slnf" -Force

$SLNF=Get-Content "osu-framework.iOS.slnf" | ConvertFrom-Json
$SLNF.solution.projects += ("../veldrid/src/Veldrid/Veldrid.csproj")
$SLNF.solution.projects += ("../veldrid/src/Veldrid.OpenGLBindings/Veldrid.OpenGLBindings.csproj")
$SLNF.solution.projects += ("../veldrid/src/Veldrid.MetalBindings/Veldrid.MetalBindings.csproj")
ConvertTo-Json $SLNF | Out-File $TMP -Encoding UTF8
Move-Item -Path $TMP -Destination "osu-framework.iOS.slnf" -Force
