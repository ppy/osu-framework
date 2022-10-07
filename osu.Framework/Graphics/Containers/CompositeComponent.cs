// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// An updateable component that can be inserted into the draw hierarchy and has self-managed child components.
    /// This is currently used as a marker for cases where nothing more than load, update, lifetime support and hierarchy presence are required.
    /// Eventually this will be fleshed out (and the inheritance will be reversed to Drawable : Component).
    /// </summary>
    public abstract class CompositeComponent : CompositeDrawable
    {
        protected override bool RequiresChildrenUpdate => true;
    }
}
