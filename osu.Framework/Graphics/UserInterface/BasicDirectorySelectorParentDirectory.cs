// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDirectorySelectorParentDirectory : BasicDirectorySelectorDirectory
    {
        protected override IconUsage? Icon => FontAwesome.Solid.Folder;

        public BasicDirectorySelectorParentDirectory(DirectoryInfo directory)
            : base(directory, "..")
        {
        }

        protected override void ApplyHiddenState()
        {
        }
    }
}
