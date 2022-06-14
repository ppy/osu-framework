// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A basic implementation of <see cref="ITextPart"/>,
    /// which automatically handles returning correct <see cref="Drawables"/>
    /// and raising <see cref="DrawablePartsRecreated"/>.
    /// </summary>
    public abstract class TextPart : ITextPart
    {
        public IEnumerable<Drawable> Drawables { get; }
        public event Action<IEnumerable<Drawable>> DrawablePartsRecreated;

        private readonly List<Drawable> drawables = new List<Drawable>();

        protected TextPart()
        {
            Drawables = drawables.AsReadOnly();
        }

        public void RecreateDrawablesFor(TextFlowContainer textFlowContainer)
        {
            drawables.Clear();
            drawables.AddRange(CreateDrawablesFor(textFlowContainer));
            DrawablePartsRecreated?.Invoke(drawables);
        }

        /// <summary>
        /// Creates drawables representing the contents of this <see cref="TextPart"/>,
        /// to be appended to the <paramref name="textFlowContainer"/>.
        /// </summary>
        protected abstract IEnumerable<Drawable> CreateDrawablesFor(TextFlowContainer textFlowContainer);
    }
}
