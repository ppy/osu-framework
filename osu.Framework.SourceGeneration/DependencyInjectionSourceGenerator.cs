// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.CodeAnalysis;
using osu.Framework.SourceGeneration.Emitters;

namespace osu.Framework.SourceGeneration
{
    [Generator]
    public class DependencyInjectionSourceGenerator : ISourceGenerator
    {
        protected virtual bool AddUniqueNameSuffix => true;

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not SyntaxContextReceiver receiver)
                return;

            foreach (var kvp in receiver.CandidateClasses)
            {
                GeneratorClassCandidate classCandidate = kvp.Value;

                string suffix = AddUniqueNameSuffix ? $"_{Guid.NewGuid()}" : string.Empty;
                string filename = $"g_{classCandidate.ClassSyntax.Identifier.ValueText}_Dependencies{suffix}.cs";

                context.AddSource(filename, new DependenciesFileEmitter(context, receiver, classCandidate).Emit());
            }
        }
    }
}
