// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.SourceGeneration
{
    public class GeneratorClassCandidateComparer : IEqualityComparer<GeneratorClassCandidate>
    {
        public static readonly GeneratorClassCandidateComparer DEFAULT = new GeneratorClassCandidateComparer();

        public bool Equals(GeneratorClassCandidate x, GeneratorClassCandidate y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;

            return string.Equals(x.FullyQualifiedTypeName, y.FullyQualifiedTypeName);
        }

        public int GetHashCode(GeneratorClassCandidate obj)
        {
            return obj.FullyQualifiedTypeName.GetHashCode();
        }
    }
}
