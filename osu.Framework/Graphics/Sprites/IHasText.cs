// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Interface for <see cref="IDrawable"/> components that support reading and writing text.
    /// </summary>
    public interface IHasText : IDrawable
    {
        string Text { get; set; }
    }
}
