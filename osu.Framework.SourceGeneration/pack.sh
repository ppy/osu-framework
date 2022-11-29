#!/bin/bash

dotnet pack osu.Framework.SourceGeneration.Roslyn3.11.csproj $@
dotnet pack osu.Framework.SourceGeneration.Roslyn4.0.csproj $@
