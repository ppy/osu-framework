// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using osu.Framework.SourceGeneration.Generators.Dependencies;

namespace osu.Framework.SourceGeneration.Generators
{
    public class GeneratorEventDriver
    {
        public event Action<SyntaxTarget>? SyntaxTargetCreated;
        public event Action<SyntaxTarget>? SemanticTargetCreated;
        public event Action<ImmutableArray<SyntaxTarget>>? Stage2Entry;
        public event Action<ImmutableArray<SyntaxTarget>>? Stage2GenerationIdAssigned;
        public event Action<HashSet<SyntaxTarget>>? Stage2Exit;
        public event Action<GeneratorClassCandidate>? Emit;

        public void OnSyntaxTargetCreated(SyntaxTarget target)
        {
            conditionalInvoke(SyntaxTargetCreated, target);
        }

        public void OnSemanticTargetCreated(SyntaxTarget target)
        {
            conditionalInvoke(SemanticTargetCreated, target);
        }

        public void OnStage2Entry(ImmutableArray<SyntaxTarget> target)
        {
            conditionalInvoke(Stage2Entry, target);
        }

        public void OnStage2GenerationIdAssigned(ImmutableArray<SyntaxTarget> target)
        {
            conditionalInvoke(Stage2GenerationIdAssigned, target);
        }

        public void OnStage2Exit(HashSet<SyntaxTarget> target)
        {
            conditionalInvoke(Stage2Exit, target);
        }

        public void OnEmit(GeneratorClassCandidate candidate)
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
