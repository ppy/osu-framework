// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Analysers
{
    public class DiagnosticRules
    {
        // Disable's roslyn analyser release tracking. Todo: Temporary? The analyser doesn't behave well with Rider :/
        // Read more: https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
#pragma warning disable RS2008

        public static readonly DiagnosticDescriptor MAKE_DI_CLASS_PARTIAL = new DiagnosticDescriptor(
            "OFSG001",
            "Class contributes to dependency injection and should be partial",
            "Class contributes to dependency injection and should be partial",
            "Performance",
            DiagnosticSeverity.Warning,
            true,
            "Classes contributing to dependency injection should be made partial to be subject to compile-time optimisations. This includes usages of `DependencyActivator` and `CachedModelDependencyContainer`.");

#pragma warning restore RS2008
    }
}
