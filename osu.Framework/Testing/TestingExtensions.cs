// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Testing
{
    public static class TestingExtensions
    {
        /// <summary>
        /// Find all children recursively of a specific type. As this is expensive and dangerous, it should only be used for testing purposes.
        /// </summary>
        public static IEnumerable<T> ChildrenOfType<T>(this Drawable drawable)
        {
            if (drawable is T match)
                yield return match;

            if (drawable is CompositeDrawable composite)
            {
                for (int i = 0; i < composite.InternalChildren.Count; i++)
                {
                    var child = composite.InternalChildren[i];

                    foreach (var found in child.ChildrenOfType<T>())
                        yield return found;
                }
            }
        }
    }
}
