// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An <see cref="ITextPart"/> which utilises externally-provided drawables.
    /// Will never recreate its contents or raise <see cref="DrawablePartsRecreated"/>.
    /// </summary>
    public class TextPartManual : ITextPart
    {
        public IEnumerable<Drawable> Drawables { get; }

        public event Action<IEnumerable<Drawable>> DrawablePartsRecreated
        {
            add { }
            remove { }
        }

        public TextPartManual(IEnumerable<Drawable> drawables)
        {
            Drawables = drawables.ToImmutableArray();
        }

        public void RecreateDrawablesFor(TextFlowContainer textFlowContainer)
        {
        }
    }
}
