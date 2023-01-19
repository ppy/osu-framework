// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// Represents an effect that can be applied to a drawable.
    /// </summary>
    /// <typeparam name="T">The type of the drawable that is created as a result of applying the effect to a drawable.</typeparam>
    public interface IEffect<out T> where T : Drawable
    {
        /// <summary>
        /// Applies this effect to the given drawable.
        /// </summary>
        /// <param name="drawable">The drawable to apply this effect to.</param>
        /// <returns>A new drawable derived from the given drawable with the effect applied.</returns>
        T ApplyTo(Drawable drawable);
    }
}
