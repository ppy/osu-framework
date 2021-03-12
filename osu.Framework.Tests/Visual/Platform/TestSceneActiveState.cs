// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestSceneActiveState : FrameworkTestScene
    {
        private IBindable<bool> isActive;
        private IBindable<bool> cursorInWindow;

        private Box isActiveBox;
        private Box cursorInWindowBox;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            isActive = host.IsActive.GetBoundCopy();
            cursorInWindow = host.Window?.CursorInWindow.GetBoundCopy();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Children = new Drawable[]
            {
                isActiveBox = new Box
                {
                    Colour = Color4.Black,
                    Width = 0.5f,
                    RelativeSizeAxes = Axes.Both,
                },
                cursorInWindowBox = new Box
                {
                    Colour = Color4.Black,
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                },
            };

            isActive.BindValueChanged(active => isActiveBox.Colour = active.NewValue ? Color4.Green : Color4.Red, true);
            cursorInWindow?.BindValueChanged(active => cursorInWindowBox.Colour = active.NewValue ? Color4.Green : Color4.Red, true);
        }
    }
}
