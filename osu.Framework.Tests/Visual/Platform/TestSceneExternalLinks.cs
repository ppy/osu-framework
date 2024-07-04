// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Tests.Visual.Platform
{
    public partial class TestSceneExternalLinks : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        public TestSceneExternalLinks()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(20),
                Padding = new MarginPadding(10),
                Children = new Drawable[]
                {
                    new BasicButton
                    {
                        Action = () => host.OpenUrlExternally("https://osu.ppy.sh"),
                        Size = new Vector2(150, 30),
                        Text = "Open osu! site",
                    },
                    new BasicButton
                    {
                        Action = () => host.OpenUrlExternally("mailto:contact@ppy.sh"),
                        Size = new Vector2(150, 30),
                        Text = "Mail to contact email",
                    },
                    new BasicButton
                    {
                        Action = () => host.OpenUrlExternally("this is a bad link that shouldn't crash the app"),
                        Size = new Vector2(150, 30),
                        Text = "Open bad link",
                    },
                    new BasicButton
                    {
                        Action = () => host.OpenUrlExternally("https://github.com/ppy/osu-framework"),
                        Size = new Vector2(150, 30),
                        Text = "Open github link",
                    },
                    new BasicButton
                    {
                        Action = () => host.OpenUrlExternally("https://twitter.com/osugame"),
                        Size = new Vector2(150, 30),
                        Text = "Open twitter link",
                    },
                }
            };
        }
    }
}
