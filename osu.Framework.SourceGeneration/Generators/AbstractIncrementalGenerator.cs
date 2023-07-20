// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators
{
    public abstract class AbstractIncrementalGenerator : IIncrementalGenerator
    {
        public readonly GeneratorEventDriver EventDriver = new GeneratorEventDriver();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Stage 1: Create SyntaxTarget objects for all classes.
            IncrementalValuesProvider<IncrementalSyntaxTarget> syntaxTargets =
                context.SyntaxProvider.CreateSyntaxProvider(
                           (n, _) => isSyntaxTarget(n),
                           (ctx, _) => returnWithEvent(new IncrementalSyntaxTarget((ClassDeclarationSyntax)ctx.Node, ctx.SemanticModel), EventDriver.OnSyntaxTargetCreated))
                       .Select((t, _) => t.WithName())
                       .Combine(context.CompilationProvider)
                       .Where(c => c.Right.Options.OptimizationLevel == OptimizationLevel.Release)
                       .Select((t, _) => t.Item1)
                       .Select((t, _) => returnWithEvent(t.WithSemanticTarget(CreateSemanticTarget), EventDriver.OnSemanticTargetCreated));

            // Stage 2: Separate out the old and new syntax targets for the same class object.
            // At this point, there are a bunch of old and new syntax targets that may refer to the same class object.
            // Find a distinct syntax target for any one class object, preferring the most-recent target.
            // Example: Multi-partial definitions where one file is updated. We need to find the definition that was newly-updated.
            // Example: Multi-partial definitions where an unrelated file is updated. Need to find the definition that was used for the last generation.
            // Bug: Due to an internal bug in Roslyn, this may also occur for non-multi-partial files.
            IncrementalValuesProvider<IncrementalSyntaxTarget> distinctSyntaxTargets =
                syntaxTargets
                    .Collect()
                    .SelectMany((targets, _) =>
                    {
                        EventDriver.OnStage2Entry(targets);

                        // Ensure all targets have a generation ID. This is over-engineered as two loops to:
                        // 1. Increment the generation ID locally for deterministic test output.
                        // 2. Remain performant across many thousands of objects.
                        Dictionary<IncrementalSyntaxTarget, long> maxGenerationIds = new Dictionary<IncrementalSyntaxTarget, long>(IncrementalSyntaxTarget.SyntaxNameComparer.DEFAULT);

                        foreach (var target in targets)
                        {
                            maxGenerationIds.TryGetValue(target, out long existingValue);
                            maxGenerationIds[target] = Math.Max(existingValue, target.GenerationId ?? 0);
                        }

                        foreach (var target in targets)
                            target.GenerationId ??= maxGenerationIds[target] + 1;

                        EventDriver.OnStage2GenerationIdAssigned(targets);

                        HashSet<IncrementalSyntaxTarget> result = new HashSet<IncrementalSyntaxTarget>(IncrementalSyntaxTarget.SyntaxNameComparer.DEFAULT);

                        // Filter out the targets, preferring the most recent at all times.
                        foreach (IncrementalSyntaxTarget t in targets.OrderByDescending(t => t.GenerationId))
                            result.Add(t);

                        EventDriver.OnStage2Exit(result);
                        return result;
                    });

            context.RegisterImplementationSourceOutput(distinctSyntaxTargets.Select((t, _) => t.SemanticTarget!), emit);
        }

        protected abstract IncrementalSemanticTarget CreateSemanticTarget(ClassDeclarationSyntax node, SemanticModel semanticModel);

        protected abstract IncrementalSourceEmitter CreateSourceEmitter(IncrementalSemanticTarget target);

        private static bool isSyntaxTarget(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax classSyntax)
                return false;

            if (classSyntax.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Any(c => !c.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return false;

            return true;
        }

        private void emit(SourceProductionContext context, IncrementalSemanticTarget target)
        {
            EventDriver.OnEmit(target);
            CreateSourceEmitter(target).Emit(context.AddSource);
        }

        private static T returnWithEvent<T>(T arg, Action<T> @event)
        {
            @event(arg);
            return arg;
        }
    }
}
