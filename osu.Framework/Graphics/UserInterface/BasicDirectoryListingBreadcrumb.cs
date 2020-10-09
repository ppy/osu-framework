// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDirectoryListingBreadcrumb : DirectoryListingBreadcumb
    {
        protected override DirectoryListingDirectory CreateComputerPiece() => new ComputerPiece();
        protected override DirectoryListingDirectory CreateDirectoryPiece(DirectoryInfo directory, string displayName = null) => new CurrentDisplayPiece(directory, displayName);

        public BasicDirectoryListingBreadcrumb()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        protected class ComputerPiece : CurrentDisplayPiece
        {
            protected override IconUsage? Icon => null;

            public ComputerPiece()
                : base(null, "Computer")
            {
            }
        }

        protected class CurrentDisplayPiece : BasicDirectoryListingDirectory
        {
            protected override IconUsage? Icon => Directory.Name.Contains(Path.DirectorySeparatorChar) ? base.Icon : null;

            public CurrentDisplayPiece(DirectoryInfo directory, string displayName = null)
                : base(directory, displayName)
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