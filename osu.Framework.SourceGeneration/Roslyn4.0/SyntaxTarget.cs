// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration
{
    public class SyntaxTarget : IEquatable<SyntaxTarget>
    {
        public readonly ClassDeclarationSyntax Syntax;
        public string? SyntaxName { get; set; }
        public long? GenerationId;

        private SemanticModel? semanticModel;
        private GeneratorClassCandidate? semanticTarget;

        public SyntaxTarget(ClassDeclarationSyntax syntax, SemanticModel semanticModel)
        {
            Syntax = syntax;
            this.semanticModel = semanticModel;
        }

        public SyntaxTarget WithName()
        {
            SyntaxName ??= SyntaxHelpers.GetFullyQualifiedSyntaxName(Syntax);
            return this;
        }

        public GeneratorClassCandidate ResolveSemanticTarget()
        {
            semanticTarget ??= new GeneratorClassCandidate(Syntax, semanticModel!);
            semanticModel = null;

            return semanticTarget;
        }

        public bool Equals(SyntaxTarget? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Syntax == other.Syntax;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((SyntaxTarget)obj);
        }

        public override int GetHashCode()
        {
            return Syntax.GetHashCode();
        }
    }
}
