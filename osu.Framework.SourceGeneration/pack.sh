#!/bin/bash

dotnet pack ./Roslyn3.11/osu.Framework.SourceGeneration.Roslyn3.11.csproj $@
dotnet pack ./Roslyn4.0/osu.Framework.SourceGeneration.Roslyn4.0.csproj $@
