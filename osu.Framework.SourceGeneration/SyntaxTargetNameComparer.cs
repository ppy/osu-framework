// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.SourceGeneration
{
    public class SyntaxTargetNameComparer : IEqualityComparer<SyntaxTarget>
    {
        public static readonly SyntaxTargetNameComparer DEFAULT = new SyntaxTargetNameComparer();

        public bool Equals(SyntaxTarget? x, SyntaxTarget? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;

            return x.SyntaxName == y.SyntaxName;
        }

        public int GetHashCode(SyntaxTarget obj)
        {
            return obj.SyntaxName!.GetHashCode();
        }
    }
}
