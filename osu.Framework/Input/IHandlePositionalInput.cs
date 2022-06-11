// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Input
{
    /// <summary>
    /// Marker interface for interfaces which depend on positional input.
    /// All drawables which implement this interface will have <see cref="Graphics.Drawable.HandlePositionalInput"/> set to <see langword="true"/> by default.
    /// </summary>
    public interface IHandlePositionalInput
    {
    }
}
