// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Timing;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Exposes various properties that are part of the public interface of <see cref="Drawable"/>.
    /// This interface should generally NOT be implemented by other classes than <see cref="Drawable"/>, but only used to
    /// specify that an object is of type <see cref="Drawable"/>.
    /// It is mostly useful in cases where you need to specify additional constraints on a <see cref="Drawable"/>, but also do not want to force inheriting from
    /// any particular subclass of <see cref="Drawable"/>.
    /// </summary>
    public interface IDrawable : ITransformable
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
        /// Contains the colour and blending information of this <see cref="Drawable"/> that are used during draw.
        /// </summary>
        DrawColourInfo DrawColourInfo { get; }

        /// <summary>
        /// The screen-space quad this drawable occupies.
        /// </summary>
        Quad ScreenSpaceDrawQuad { get; }

        /// <summary>
        /// The parent of this drawable in the scene graph.
        /// </summary>
        CompositeDrawable? Parent { get; }

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
        BlendingParameters Blending { get; }

        /// <summary>
        /// Whether this Drawable is currently hovered over.
        /// </summary>
        bool IsHovered { get; }

        /// <summary>
        /// Whether this Drawable is currently dragged.
        /// </summary>
        bool IsDragged { get; }

        /// <summary>
        /// Multiplicative alpha factor applied on top of <see cref="Colour.ColourInfo"/> and its existing
        /// alpha channel(s).
        /// </summary>
        float Alpha { get; }

        /// <summary>
        /// Show sprite instantly.
        /// </summary>
        void Show();

        /// <summary>
        /// Hide sprite instantly.
        /// </summary>
        void Hide();

        /// <summary>
        /// The current invalidation ID of this <see cref="Drawable"/>.
        /// Incremented every time the <see cref="DrawNode"/> should be re-validated.
        /// </summary>
        long InvalidationID { get; }
    }
}
