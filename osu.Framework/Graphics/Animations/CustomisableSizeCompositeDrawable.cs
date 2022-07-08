// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// A drawable which handles sizing in a roughly expected way when wrapping a single direct child.
    /// </summary>
    public abstract class CustomisableSizeCompositeDrawable : CompositeDrawable
    {
        private bool hasCustomWidth;

        public override float Width
        {
            set
            {
                base.Width = value;
                hasCustomWidth = true;
            }
        }

        private bool hasCustomHeight;

        public override float Height
        {
            set
            {
                base.Height = value;
                hasCustomHeight = true;
            }
        }

        public override Vector2 Size
        {
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        /// <summary>
        /// Retrieves the size of the target display content.
        /// </summary>
        /// <returns>The size of current content.</returns>
        protected abstract Vector2 GetCurrentDisplaySize();

        protected abstract float GetFillAspectRatio();

        protected virtual void UpdateSizing()
        {
            FillAspectRatio = GetFillAspectRatio();

            if (RelativeSizeAxes == Axes.Both) return;

            var frameSize = GetCurrentDisplaySize();

            if ((RelativeSizeAxes & Axes.X) == 0 && !hasCustomWidth)
                base.Width = frameSize.X;

            if ((RelativeSizeAxes & Axes.Y) == 0 && !hasCustomHeight)
                base.Height = frameSize.Y;
        }
    }
}
