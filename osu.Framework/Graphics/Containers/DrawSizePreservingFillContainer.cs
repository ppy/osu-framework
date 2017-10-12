// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A <see cref="Container"/> filling its parent while preserving a given target
    /// <see cref="Drawable.DrawSize"/> according to a <see cref="DrawSizePreservationStrategy"/>.
    /// This is useful, for example, to automatically scale the user interface according to
    /// the window resolution, or to provide automatic HiDPI display support.
    /// </summary>
    public class DrawSizePreservingFillContainer : Container
    {
        /// <summary>
        /// The target <see cref="DrawSize"/> to be enforced according to <see cref="Strategy"/>.
        /// </summary>
        public Vector2 TargetDrawSize = new Vector2(1024, 768);

        /// <summary>
        /// The strategy to be used for enforcing <see cref="TargetDrawSize"/>. The default strategy
        /// is Minimum, which preserves the aspect ratio of all children while ensuring one of the
        /// two axes matches <see cref="TargetDrawSize"/> while the other is always larger.
        /// </summary>
        public DrawSizePreservationStrategy Strategy;

        public DrawSizePreservingFillContainer()
        {
            RelativeSizeAxes = Axes.Both;
        }

        protected override void Update()
        {
            base.Update();

            Vector2 drawSizeRatio = Vector2.Divide(Parent.DrawSize, TargetDrawSize);

            switch (Strategy)
            {
                case DrawSizePreservationStrategy.Minimum:
                    Scale = new Vector2(Math.Min(drawSizeRatio.X, drawSizeRatio.Y));
                    break;

                case DrawSizePreservationStrategy.Maximum:
                    Scale = new Vector2(Math.Min(drawSizeRatio.X, drawSizeRatio.Y));
                    break;

                case DrawSizePreservationStrategy.Average:
                    Scale = new Vector2(0.5f * (drawSizeRatio.X + drawSizeRatio.Y));
                    break;

                case DrawSizePreservationStrategy.Separate:
                    Scale = drawSizeRatio;
                    break;
            }


            Size = Vector2.Divide(Vector2.One, Scale);
        }
    }

    /// <summary>
    /// Strategies used by <see cref="DrawSizePreservingFillContainer"/> to enforce its
    /// <see cref="DrawSizePreservingFillContainer.TargetDrawSize"/>.
    /// </summary>
    public enum DrawSizePreservationStrategy
    {
        /// <summary>
        /// Preserves the aspect ratio of all children while ensuring one of the
        /// two axes matches <see cref="TargetDrawSize"/> while the other is always larger.
        /// </summary>
        Minimum,
        /// <summary>
        /// Preserves the aspect ratio of all children while ensuring one of the
        /// two axes matches <see cref="TargetDrawSize"/> while the other is always smaller.
        /// </summary>
        Maximum,
        /// <summary>
        /// Preserves the aspect ratio of all children while one axis is always larger and
        /// the other always smaller than <see cref="TargetDrawSize"/>, achieving a good compromise.
        /// </summary>
        Average,
        /// <summary>
        /// Ensures <see cref="TargetDrawSize"/> is perfectly matched, while aspect ratio of children
        /// is disregarded.
        /// </summary>
        Separate,
    }
}
