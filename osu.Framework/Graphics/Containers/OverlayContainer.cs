// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.InputQueue;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An element which starts hidden and can be toggled to visible.
    /// </summary>
    public abstract class OverlayContainer : VisibilityContainer
    {
        /// <summary>
        /// Whether we should block any positional input from interacting with things behind us.
        /// </summary>
        protected internal virtual bool BlockPositionalInput => true;

        /// <summary>
        /// Whether we should block any non-positional input from interacting with things behind us.
        /// </summary>
        protected internal virtual bool BlockNonPositionalInput => false;

        public override bool Accept(INonPositionalInputVisitor visitor, bool allowBlocking = true) => visitor.Visit(this, allowBlocking);

        public override bool Accept(IPositionalInputVisitor visitor, Vector2 screenSpacePos) => visitor.Visit(screenSpacePos, this);
    }
}
