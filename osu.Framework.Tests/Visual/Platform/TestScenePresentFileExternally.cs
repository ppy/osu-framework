// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Tests.Visual.Platform
{
    public class TestScenePresentFileExternally : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private Storage storage { get; set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var logStorage = storage.GetStorageForDirectory(@"logs");

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(20),
                Padding = new MarginPadding(10),
                Children = new Drawable[]
                {
                    new ButtonWithDescription
                    (
                        () => host.PresentFileExternally(@"C:/Windows/system32/winver.exe"),
                        @"C:/Windows/system32/winver.exe",
                        @"Opens: 'C:/Windows/system32'   Selected: 'winver.exe'"
                    ),
                    new ButtonWithDescription
                    (
                        () => host.PresentFileExternally(@"C:\Windows\system32"),
                        @"C:\Windows\system32",
                        @"Opens: 'C:\Windows'   Selected: 'System32'"
                    ),
                    new ButtonWithDescription
                    (
                        () => host.PresentFileExternally(@"C:\Windows\system32\"),
                        @"C:\Windows\system32\",
                        @"Opens: 'C:\Windows'   Selected: 'System32'"
                    ),
                    new ButtonWithDescription
                    (
                        () => host.OpenFileExternally(@"C:\Windows\system32"),
                        @"Open C:\Windows\system32",
                        @"Opens: 'C:\Windows\System32'   Selected: nothing"
                    ),
                    new ButtonWithDescription
                    (
                        () => host.OpenFileExternally(@"C:\Windows\system32\"),
                        @"Open C:\Windows\system32\",
                        @"Opens: 'C:\Windows\System32'   Selected: nothing"
                    ),
                    new ButtonWithDescription
                    (
                        () => logStorage.PresentExternally(),
                        @"open logs folder",
                        @"Opens: 'logs'   Selected: nothing"
                    ),
                    new ButtonWithDescription
                    (
                        () => logStorage.PresentFileExternally(string.Empty),
                        @"show logs folder",
                        @"Opens: 'visual-tests'   Selected: 'logs'"
                    ),
                    new ButtonWithDescription
                    (
                        () => logStorage.PresentFileExternally(@"runtime.log"),
                        @"show runtime.log",
                        @"Opens: 'logs'   Selected: 'runtime.log'"
                    ),
                    new ButtonWithDescription
                    (
                        () => logStorage.PresentFileExternally(@"file that doesn't exist"),
                        @"show non-existent file (in logs folder)",
                        @"Opens: 'logs'   Selected: nothing"
                    )
                }
            };
        }

        private class ButtonWithDescription : FillFlowContainer
        {
            public ButtonWithDescription(Action action, string text, string description)
            {
                AutoSizeAxes = Axes.Both;
                Direction = FillDirection.Vertical;
                Spacing = new Vector2(3);
                Children = new Drawable[]
                {
                    new BasicButton
                    {
                        Anchor = Anchor.TopLeft,
                        Action = action,
                        Size = new Vector2(430, 30),
                        Text = text,
                    },
                    new Container
                    {
                        Size = new Vector2(430, 25),
                        Child = new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = description
                        }
                    }
                };
            }
        }
    }
}
