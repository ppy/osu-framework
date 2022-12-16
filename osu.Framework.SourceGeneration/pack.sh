#!/bin/bash

dotnet build ./Roslyn3.11/osu.Framework.SourceGeneration.Roslyn3.11.csproj --no-incremental $@
dotnet build ./Roslyn4.0/osu.Framework.SourceGeneration.Roslyn4.0.csproj --no-incremental $@

dotnet pack ./Roslyn3.11/osu.Framework.SourceGeneration.Roslyn3.11.csproj --no-build $@
dotnet pack ./Roslyn4.0/osu.Framework.SourceGeneration.Roslyn4.0.csproj --no-build $@
