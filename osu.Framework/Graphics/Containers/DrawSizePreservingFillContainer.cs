// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

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
        private readonly Container content;

        protected override Container<Drawable> Content => content;

        /// <summary>
        /// The target <see cref="Drawable.DrawSize"/> to be enforced according to <see cref="Strategy"/>.
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
            AddInternal(content = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            RelativeSizeAxes = Axes.Both;
        }

        protected override void Update()
        {
            base.Update();

            Vector2 drawSizeRatio = Vector2.Divide(Parent.ChildSize, TargetDrawSize);

            switch (Strategy)
            {
                case DrawSizePreservationStrategy.Minimum:
                    content.Scale = new Vector2(Math.Min(drawSizeRatio.X, drawSizeRatio.Y));
                    break;

                case DrawSizePreservationStrategy.Maximum:
                    content.Scale = new Vector2(Math.Max(drawSizeRatio.X, drawSizeRatio.Y));
                    break;

                case DrawSizePreservationStrategy.Average:
                    content.Scale = new Vector2(0.5f * (drawSizeRatio.X + drawSizeRatio.Y));
                    break;

                case DrawSizePreservationStrategy.Separate:
                    content.Scale = drawSizeRatio;
                    break;
            }

            content.Size = Vector2.Divide(Vector2.One, content.Scale);
        }

        #region Size modification guards

        public override Axes RelativeSizeAxes
        {
            get => base.RelativeSizeAxes;
            set
            {
                if (RelativeSizeAxes == value) return;

                // Allow filling-to-parent values.
                if (value == Axes.Both)
                {
                    base.RelativeSizeAxes = value;
                    return;
                }

                throw new MustFillToParentException();
            }
        }

        public override Axes AutoSizeAxes
        {
            get => base.AutoSizeAxes;
            set
            {
                if (AutoSizeAxes == value) return;

                // Allow filling-to-parent values.
                if (value == Axes.None)
                {
                    base.AutoSizeAxes = value;
                    return;
                }

                throw new MustFillToParentException();
            }
        }

        public override Vector2 Size
        {
            get => base.Size;
            set
            {
                if (Size == value) return;

                // Allow filling-to-parent values.
                if (value == Vector2.One)
                {
                    base.Size = value;
                    return;
                }

                throw new MustFillToParentException();
            }
        }

        public override float Width
        {
            get => base.Width;
            set
            {
                if (Width == value) return;

                if (value == 1f)
                {
                    base.Width = value;
                    return;
                }

                throw new MustFillToParentException();
            }
        }

        public override float Height
        {
            get => base.Height;
            set
            {
                if (Height == value) return;

                if (value == 1f)
                {
                    base.Height = value;
                    return;
                }

                throw new MustFillToParentException();
            }
        }

        #endregion
    }

    /// <summary>
    /// Strategies used by <see cref="DrawSizePreservingFillContainer"/> to enforce its
    /// <see cref="DrawSizePreservingFillContainer.TargetDrawSize"/>.
    /// </summary>
    public enum DrawSizePreservationStrategy
    {
        /// <summary>
        /// Preserves the aspect ratio of all children while ensuring one of the
        /// two axes matches <see cref="DrawSizePreservingFillContainer.TargetDrawSize"/>
        /// while the other is always larger.
        /// </summary>
        Minimum,

        /// <summary>
        /// Preserves the aspect ratio of all children while ensuring one of the
        /// two axes matches <see cref="DrawSizePreservingFillContainer.TargetDrawSize"/>
        /// while the other is always smaller.
        /// </summary>
        Maximum,

        /// <summary>
        /// Preserves the aspect ratio of all children while one axis is always larger and
        /// the other always smaller than <see cref="DrawSizePreservingFillContainer.TargetDrawSize"/>,
        /// achieving a good compromise.
        /// </summary>
        Average,

        /// <summary>
        /// Ensures <see cref="DrawSizePreservingFillContainer.TargetDrawSize"/> is perfectly
        /// matched while aspect ratio of children is disregarded.
        /// </summary>
        Separate,
    }

    public class MustFillToParentException : NotSupportedException
    {
        public MustFillToParentException()
            : base($"A {nameof(DrawSizePreservingFillContainer)} will automatically fill up to its parent's determined {nameof(CompositeDrawable.ChildSize)}). "
                   + "Modifying its dimensions is not supported.")
        {
        }
    }
}
