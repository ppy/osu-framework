// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Graphics.Animations
{
    /// <summary>
    /// An animation that switches the displayed drawable when a new frame is displayed.
    /// </summary>
    public class DrawableAnimation : Animation<Drawable>
    {
        protected override void DisplayFrame(Drawable content)
        {
            ClearInternal(false);
            AddInternal(content);
        }

        protected override Vector2 GetFrameSize(Drawable content) => new Vector2(content.DrawWidth, content.DrawHeight);
    }
}
