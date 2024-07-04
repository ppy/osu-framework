// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// This class holds extension methods for effects.
    /// </summary>
    public static class EffectExtensions
    {
        /// <summary>
        /// Applies the given effect to the given drawable and optionally initializes the created drawable with the given initializationAction.
        /// </summary>
        /// <typeparam name="T">The type of the drawable that results from applying the given effect.</typeparam>
        /// <param name="effect">The effect to apply to the drawable.</param>
        /// <param name="drawable">The drawable to apply the effect to.</param>
        /// <param name="initializationAction">The action that should get called to initialize the created drawable before it is returned.</param>
        /// <returns>The drawable created by applying the given effect to this drawable.</returns>
        public static T ApplyTo<T>(this IEffect<T> effect, Drawable drawable, Action<T> initializationAction = null) where T : Drawable
            => drawable.WithEffect(effect, initializationAction);
    }
}
