// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDirectorySelectorDirectory : DirectorySelectorDirectory
    {
        protected override IconUsage? Icon => Directory.Name.Contains(Path.DirectorySeparatorChar)
            ? FontAwesome.Solid.Database
            : FontAwesome.Regular.Folder;

        protected override SpriteText CreateSpriteText() => new SpriteText
        {
            Font = FrameworkFont.Regular.With(size: FONT_SIZE)
        };

        public BasicDirectorySelectorDirectory(DirectoryInfo directory, string displayName = null)
            : base(directory, displayName)
        {
        }
    }
}
