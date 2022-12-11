// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace osu.Framework.SourceGeneration
{
    public partial class DependencyInjectionSourceGenerator
    {
        public static class GeneratorEvent
        {
            public static event Action<SyntaxTarget>? SyntaxTargetCreated;
            public static event Action<SyntaxTarget>? SemanticTargetCreated;
            public static event Action<ImmutableArray<SyntaxTarget>>? Stage2Entry;
            public static event Action<ImmutableArray<SyntaxTarget>>? Stage2GenerationIdAssigned;
            public static event Action<HashSet<SyntaxTarget>>? Stage2Exit;
            public static event Action<GeneratorClassCandidate>? Emit;

            public static void OnSyntaxTargetCreated(SyntaxTarget target)
            {
                conditionalInvoke(SyntaxTargetCreated, target);
            }

            public static void OnSemanticTargetCreated(SyntaxTarget target)
            {
                conditionalInvoke(SemanticTargetCreated, target);
            }

            public static void OnStage2Entry(ImmutableArray<SyntaxTarget> target)
            {
                conditionalInvoke(Stage2Entry, target);
            }

            public static void OnStage2GenerationIdAssigned(ImmutableArray<SyntaxTarget> target)
            {
                conditionalInvoke(Stage2GenerationIdAssigned, target);
            }

            public static void OnStage2Exit(HashSet<SyntaxTarget> target)
            {
                conditionalInvoke(Stage2Exit, target);
            }

            public static void OnEmit(GeneratorClassCandidate candidate)
            {
                conditionalInvoke(Emit, candidate);
            }

            [Conditional("DEBUG")]
            private static void conditionalInvoke<T>(Action<T>? @event, T arg)
            {
                @event?.Invoke(arg);
            }
        }
    }
}
