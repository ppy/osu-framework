// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osu.Framework.Extensions.EnumExtensions;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DirectorySelectorDirectory : DirectorySelectorItem
    {
        protected readonly DirectoryInfo Directory;
        protected override bool? ReduceOpacity => isHiddenDirectoryWithReducedOpacity;
        private readonly bool isHiddenDirectoryWithReducedOpacity;
        protected override string FallbackName => Directory.Name;

        [Resolved]
        private Bindable<DirectoryInfo> currentDirectory { get; set; }

        protected DirectorySelectorDirectory(DirectoryInfo directory, string displayName = null, bool reduceHiddenDirectoryOpacity = false)
            : base(displayName)
        {
            Directory = directory;
            isHiddenDirectoryWithReducedOpacity = (directory?.Attributes.HasFlagFast(FileAttributes.Hidden) ?? false) && reduceHiddenDirectoryOpacity;
        }

        protected override bool OnClick(ClickEvent e)
        {
            currentDirectory.Value = Directory;
            return true;
        }
    }
}
