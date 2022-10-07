// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDirectorySelectorBreadcrumbDisplay : DirectorySelectorBreadcrumbDisplay
    {
        protected override Drawable CreateCaption() => new SpriteText
        {
            Text = "Current Directory:",
            Font = FrameworkFont.Condensed.With(size: 20),
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft
        };

        protected override DirectorySelectorDirectory CreateRootDirectoryItem() => new BreadcrumbDisplayComputer();

        protected override DirectorySelectorDirectory CreateDirectoryItem(DirectoryInfo directory, string displayName = null) => new BreadcrumbDisplayDirectory(directory, displayName);

        public BasicDirectorySelectorBreadcrumbDisplay()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        protected class BreadcrumbDisplayComputer : BreadcrumbDisplayDirectory
        {
            protected override IconUsage? Icon => null;

            public BreadcrumbDisplayComputer()
                : base(null, "Computer")
            {
            }
        }

        protected class BreadcrumbDisplayDirectory : BasicDirectorySelectorDirectory
        {
            protected override IconUsage? Icon => Directory.Name.Contains(Path.DirectorySeparatorChar) ? base.Icon : null;

            public BreadcrumbDisplayDirectory(DirectoryInfo directory, string displayName = null)
                : base(directory, displayName)
            {
            }

            // this method is suppressed to ensure the breadcrumbs of hidden directories are presented the same way as non-hidden directories
            protected sealed override void ApplyHiddenState()
            {
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Flow.Add(new SpriteIcon
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Icon = FontAwesome.Solid.ChevronRight,
                    Size = new Vector2(FONT_SIZE / 2)
                });
            }
        }
    }
}
