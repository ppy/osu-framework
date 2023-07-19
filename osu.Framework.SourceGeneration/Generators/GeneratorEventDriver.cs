// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace osu.Framework.SourceGeneration.Generators
{
    public class GeneratorEventDriver
    {
        public event Action<IncrementalSyntaxTarget>? SyntaxTargetCreated;
        public event Action<IncrementalSyntaxTarget>? SemanticTargetCreated;
        public event Action<ImmutableArray<IncrementalSyntaxTarget>>? Stage2Entry;
        public event Action<ImmutableArray<IncrementalSyntaxTarget>>? Stage2GenerationIdAssigned;
        public event Action<HashSet<IncrementalSyntaxTarget>>? Stage2Exit;
        public event Action<IncrementalSemanticTarget>? Emit;

        public void OnSyntaxTargetCreated(IncrementalSyntaxTarget target)
        {
            conditionalInvoke(SyntaxTargetCreated, target);
        }

        public void OnSemanticTargetCreated(IncrementalSyntaxTarget target)
        {
            conditionalInvoke(SemanticTargetCreated, target);
        }

        public void OnStage2Entry(ImmutableArray<IncrementalSyntaxTarget> target)
        {
            conditionalInvoke(Stage2Entry, target);
        }

        public void OnStage2GenerationIdAssigned(ImmutableArray<IncrementalSyntaxTarget> target)
        {
            conditionalInvoke(Stage2GenerationIdAssigned, target);
        }

        public void OnStage2Exit(HashSet<IncrementalSyntaxTarget> target)
        {
            conditionalInvoke(Stage2Exit, target);
        }

        public void OnEmit(IncrementalSemanticTarget candidate)
        {
            conditionalInvoke(Emit, candidate);
        }

        [Conditional("DEBUG")]
        private void conditionalInvoke<T>(Action<T>? @event, T arg)
        {
            @event?.Invoke(arg);
        }
    }
}
