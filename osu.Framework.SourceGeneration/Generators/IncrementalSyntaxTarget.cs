// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators
{
    public class IncrementalSyntaxTarget : IEquatable<IncrementalSyntaxTarget>
    {
        public readonly ClassDeclarationSyntax Syntax;
        public string? SyntaxName { get; set; }
        public long? GenerationId;
        public IncrementalSemanticTarget? SemanticTarget { get; private set; }

        private SemanticModel? semanticModel;

        public IncrementalSyntaxTarget(ClassDeclarationSyntax syntax, SemanticModel semanticModel)
        {
            Syntax = syntax;
            this.semanticModel = semanticModel;
        }

        public IncrementalSyntaxTarget WithName()
        {
            SyntaxName ??= SyntaxHelpers.GetFullyQualifiedSyntaxName(Syntax);
            return this;
        }

        public IncrementalSyntaxTarget WithSemanticTarget(Func<ClassDeclarationSyntax, SemanticModel, IncrementalSemanticTarget> createTarget)
        {
            SemanticTarget ??= createTarget(Syntax, semanticModel!);
            semanticModel = null;
            return this;
        }

        public bool Equals(IncrementalSyntaxTarget? other)
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

            return Equals((IncrementalSyntaxTarget)obj);
        }

        public override int GetHashCode()
        {
            return Syntax.GetHashCode();
        }

        public class SyntaxNameComparer : IEqualityComparer<IncrementalSyntaxTarget>
        {
            public static readonly SyntaxNameComparer DEFAULT = new SyntaxNameComparer();

            public bool Equals(IncrementalSyntaxTarget? x, IncrementalSyntaxTarget? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;

                return x.SyntaxName == y.SyntaxName;
            }

            public int GetHashCode(IncrementalSyntaxTarget obj)
            {
                return obj.SyntaxName!.GetHashCode();
            }
        }
    }
}
