// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicDirectorySelector : DirectorySelector
    {
        protected override DirectorySelectorBreadcrumbDisplay CreateBreadcrumb() => new BasicDirectorySelectorBreadcrumbDisplay();

        protected override DirectorySelectorDirectory CreateDirectoryItem(DirectoryInfo directory, string displayName = null) => new BasicDirectorySelectorDirectory(directory, displayName);

        protected override DirectorySelectorDirectory CreateParentDirectoryItem(DirectoryInfo directory) => new BasicDirectorySelectorParentDirectory(directory);

        protected override ScrollContainer<Drawable> CreateScrollContainer() => new BasicScrollContainer();

        protected override void NotifySelectionError()
        {
            this.FlashColour(Colour4.Red, 300);
        }
    }
}
