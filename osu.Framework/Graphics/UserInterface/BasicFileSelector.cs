// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicFileSelector : FileSelector
    {
        protected override DirectoryCurrentDisplay CreateDirectoryCurrentDisplay() => new BasicDirectoryCurrentDisplay();

        protected override DirectoryPiece CreateDirectoryPiece(DirectoryInfo directory, string displayName = null) => new BasicDirectoryPiece(directory, displayName);

        protected override DirectoryPiece CreateParentDirectoryPiece(DirectoryInfo directory) => new BasicDirectoryParentPiece(directory);

        protected override ScrollContainer<Drawable> CreateScrollContainer() => new BasicScrollContainer();

        protected override FilePiece CreateFilePiece(FileInfo file) => new BasicFilePiece(file);

        private class BasicFilePiece : FilePiece
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
        }
    }
}