// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Generators.Dependencies.Emitters;

namespace osu.Framework.SourceGeneration.Generators.Dependencies
{
    [Generator]
    public class DependencyInjectionSourceGenerator : IIncrementalGenerator, IGeneratorWithEvents
    {
        public GeneratorEventDriver EventDriver { get; } = new GeneratorEventDriver();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Stage 1: Create SyntaxTarget objects for all classes.
            IncrementalValuesProvider<SyntaxTarget> syntaxTargets =
                context.SyntaxProvider.CreateSyntaxProvider(
                           (n, _) => GeneratorClassCandidate.IsSyntaxTarget(n),
                           (ctx, _) => returnWithEvent(new SyntaxTarget((ClassDeclarationSyntax)ctx.Node, ctx.SemanticModel), EventDriver.OnSyntaxTargetCreated))
                       .Select((t, _) => t.WithName())
                       .Select((t, _) => returnWithEvent(t.WithSemanticTarget(), EventDriver.OnSemanticTargetCreated));

            // Stage 2: Separate out the old and new syntax targets for the same class object.
            // At this point, there are a bunch of old and new syntax targets that may refer to the same class object.
            // Find a distinct syntax target for any one class object, preferring the most-recent target.
            // Example: Multi-partial definitions where one file is updated. We need to find the definition that was newly-updated.
            // Example: Multi-partial definitions where an unrelated file is updated. Need to find the definition that was used for the last generation.
            // Bug: Due to an internal bug in Roslyn, this may also occur for non-multi-partial files.
            IncrementalValuesProvider<SyntaxTarget> distinctSyntaxTargets =
                syntaxTargets
                    .Collect()
                    .SelectMany((targets, _) =>
                    {
                        EventDriver.OnStage2Entry(targets);

                        // Ensure all targets have a generation ID. This is over-engineered as two loops to:
                        // 1. Increment the generation ID locally for deterministic test output.
                        // 2. Remain performant across many thousands of objects.
                        Dictionary<SyntaxTarget, long> maxGenerationIds = new Dictionary<SyntaxTarget, long>(SyntaxTargetNameComparer.DEFAULT);

                        foreach (var target in targets)
                        {
                            maxGenerationIds.TryGetValue(target, out long existingValue);
                            maxGenerationIds[target] = Math.Max(existingValue, target.GenerationId ?? 0);
                        }

                        foreach (var target in targets)
                            target.GenerationId ??= maxGenerationIds[target] + 1;

                        EventDriver.OnStage2GenerationIdAssigned(targets);

                        HashSet<SyntaxTarget> result = new HashSet<SyntaxTarget>(SyntaxTargetNameComparer.DEFAULT);

                        // Filter out the targets, preferring the most recent at all times.
                        foreach (SyntaxTarget t in targets.OrderByDescending(t => t.GenerationId))
                            result.Add(t);

                        EventDriver.OnStage2Exit(result);
                        return result;
                    });

            context.RegisterImplementationSourceOutput(distinctSyntaxTargets.Select((t, _) => t.SemanticTarget!), emit);
        }

        private void emit(SourceProductionContext context, GeneratorClassCandidate candidate)
        {
            EventDriver.OnEmit(candidate);
            new DependenciesFileEmitter(candidate).Emit(context.AddSource);
        }

        private static T returnWithEvent<T>(T arg, Action<T> @event)
        {
            @event(arg);
            return arg;
        }
    }
}
