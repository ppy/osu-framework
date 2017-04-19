// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Lists;
using osu.Framework.Timing;
using OpenTK;
using osu.Framework.Graphics.Transforms;
using System;

namespace osu.Framework.Graphics
{
    public interface IDrawable : IHasLifetime
    {
        /// <summary>
        /// Absolute size of this Drawable in the <see cref="Parent"/>'s coordinate system.
        /// </summary>
        Vector2 DrawSize { get; }

        /// <summary>
        /// Contains a linear transformation, colour information, and blending information
        /// of this drawable.
        /// </summary>
        DrawInfo DrawInfo { get; }

        /// <summary>
        /// The parent of this drawable in the scene graph.
        /// </summary>
        IContainer Parent { get; }

        /// <summary>
        /// Whether this drawable is present for any sort of user-interaction.
        /// If this is false, then this drawable will not be drawn, it will not handle input,
        /// and it will not affect layouting (e.g. autosizing and flow).
        /// </summary>
        bool IsPresent { get; }

        /// <summary>
        /// The clock of this drawable. Used for keeping track of time across frames.
        /// </summary>
        IFrameBasedClock Clock { get; }

        /// <summary>
        /// Accepts a vector in local coordinates and converts it to coordinates in another Drawable's space.
        /// </summary>
        /// <param name="input">A vector in local coordinates.</param>
        /// <param name="other">The drawable in which space we want to transform the vector to.</param>
        /// <returns>The vector in other's coordinates.</returns>
        Vector2 ToSpaceOfOtherDrawable(Vector2 input, IDrawable other);

        /// <summary>
        /// Convert a position to the local coordinate system from either native or local to another drawable.
        /// This is *not* the same space as the Position member variable (use Parent.GetLocalPosition() in this case).
        /// </summary>
        /// <param name="screenSpacePos">The input position.</param>
        /// <returns>The output position.</returns>
        Vector2 ToLocalSpace(Vector2 screenSpacePos);

        /// <summary>
        /// Determines how this Drawable is blended with other already drawn Drawables.
        /// </summary>
        BlendingMode BlendingMode { get; }

        /// <summary>
        /// Applies a transform to this drawable object.
        /// </summary>
        /// <typeparam name="TValue">The value type upon which the transform acts.</typeparam>
        /// <param name="currentValue">A function to get the current value to transform from.</param>
        /// <param name="newValue">The value to transform to.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing.</param>
        /// <param name="transform">The transform to use.</param>
        void TransformTo<TValue>(Func<TValue> currentValue, TValue newValue, double duration, EasingTypes easing, Transform<TValue> transform) where TValue : struct, IEquatable<TValue>;
    }
}
