// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using osu.Framework.SourceGeneration.Emitters;

namespace osu.Framework.SourceGeneration
{
    [Generator]
    public class DependencyInjectionSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new CustomSyntaxContextReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not CustomSyntaxContextReceiver receiver)
                return;

            foreach (var candidate in receiver.Candidates.Distinct())
                new DependenciesFileEmitter(candidate).Emit(context.AddSource);
        }

        private class CustomSyntaxContextReceiver : ISyntaxContextReceiver
        {
            public readonly List<GeneratorClassCandidate> Candidates = new List<GeneratorClassCandidate>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (!GeneratorClassCandidate.IsCandidate(context.Node))
                    return;

                GeneratorClassCandidate? candidate = GeneratorClassCandidate.TryCreate(context.Node, context.SemanticModel);

                if (candidate != null)
                    Candidates.Add(candidate);
            }
        }
    }
}
