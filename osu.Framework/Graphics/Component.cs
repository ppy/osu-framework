// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics
{
    /// <summary>
    /// An updateable component that can be inserted into the draw hierarchy.
    /// This is currently used as a marker for cases where nothing more than load, update, lifetime support and hierarchy presence are required.
    /// Eventually this will be fleshed out (and the inheritance will be reversed to Drawable : Component).
    /// </summary>
    public abstract class Component : Drawable
    {
    }
}
