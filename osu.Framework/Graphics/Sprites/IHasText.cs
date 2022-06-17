// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Interface for <see cref="IDrawable"/> components that support reading and writing text.
    /// </summary>
    public interface IHasText : IDrawable
    {
        LocalisableString Text { get; set; }
    }
}
