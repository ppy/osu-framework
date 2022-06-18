// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicFileSelector : FileSelector
    {
        protected override DirectorySelectorBreadcrumbDisplay CreateBreadcrumb() => new BasicDirectorySelectorBreadcrumbDisplay();

        protected override Drawable CreateHiddenToggleButton() => new BasicButton
        {
            Size = new Vector2(200, 25),
            Text = "Toggle hidden items",
            Action = ShowHiddenItems.Toggle,
        };

        protected override DirectorySelectorDirectory CreateDirectoryItem(DirectoryInfo directory, string displayName = null) => new BasicDirectorySelectorDirectory(directory, displayName);

        protected override DirectorySelectorDirectory CreateParentDirectoryItem(DirectoryInfo directory) => new BasicDirectorySelectorParentDirectory(directory);

        protected override ScrollContainer<Drawable> CreateScrollContainer() => new BasicScrollContainer();

        protected override DirectoryListingFile CreateFileItem(FileInfo file) => new BasicFilePiece(file);

        protected override void NotifySelectionError()
        {
            this.FlashColour(Colour4.Red, 300);
        }

        private class BasicFilePiece : DirectoryListingFile
        {
            public BasicFilePiece(FileInfo file)
                : base(file)
            {
            }

            protected override IconUsage? Icon
            {
                get
                {
                    switch (File.Extension)
                    {
                        case ".ogg":
                        case ".mp3":
                        case ".wav":
                            return FontAwesome.Regular.FileAudio;

                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                            return FontAwesome.Regular.FileImage;

                        case ".mp4":
                        case ".avi":
                        case ".mov":
                        case ".flv":
                            return FontAwesome.Regular.FileVideo;

                        default:
                            return FontAwesome.Regular.File;
                    }
                }
            }

            protected override SpriteText CreateSpriteText() => new SpriteText
            {
                Font = FrameworkFont.Regular.With(size: FONT_SIZE)
            };
        }
    }
}
