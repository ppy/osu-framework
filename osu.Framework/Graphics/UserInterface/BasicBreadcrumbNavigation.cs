// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicBreadcrumbNavigation<T> : BreadcrumbNavigation<T>
    {
        protected override Breadcrumb CreateBreadcrumb(T value)
        {
            return new BasicBreadcrumb(value)
            {
                RelativeSizeAxes = Axes.Y,
            };
        }

        private class BasicBreadcrumb : Breadcrumb
        {
            private readonly SpriteText text;
            private readonly Box background;

            public string Text {
                get => text.Text;
                set => text.Text = value;
            }

            public BasicBreadcrumb(T value) : base(value)
            {
                AutoSizeAxes = Axes.X;
                Current.ValueChanged += args => background.Colour = args.NewValue ? Color4.DarkSlateGray : Color4.Gray;

                AddRangeInternal(new Drawable[] {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Gray
                    },
                    text = new SpriteText
                    {
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft,
                        Margin = new MarginPadding(3),
                        Text = value.ToString(),
                    }
                });
            }
        }
    }
}
