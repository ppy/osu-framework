// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDirectoryParentPiece : BasicDirectoryPiece
    {
        protected override IconUsage? Icon => FontAwesome.Solid.Folder;

        public BasicDirectoryParentPiece(DirectoryInfo directory)
            : base(directory, "..")
        {
        }
    }
}