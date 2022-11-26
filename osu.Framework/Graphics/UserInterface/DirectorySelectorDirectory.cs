// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osu.Framework.Extensions.EnumExtensions;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class DirectorySelectorDirectory : DirectorySelectorItem
    {
        protected readonly DirectoryInfo Directory;
        protected override string FallbackName => Directory.Name;

        [Resolved]
        private Bindable<DirectoryInfo> currentDirectory { get; set; }

        protected DirectorySelectorDirectory(DirectoryInfo directory, string displayName = null)
            : base(displayName)
        {
            Directory = directory;

            try
            {
                bool isHidden = directory?.Attributes.HasFlagFast(FileAttributes.Hidden) == true;

                // On Windows, system drives are returned with `System | Hidden | Directory` file attributes,
                // but the expectation is that they shouldn't be shown in a hidden state.
                bool isSystemDrive = directory?.Parent == null;

                if (isHidden && !isSystemDrive)
                    ApplyHiddenState();
            }
            catch (IOException)
            {
                // various IO exceptions could occur when attempting to read attributes.
                // one example is when a target directory is a drive which is locked by BitLocker:
                //
                // "Unhandled exception. System.IO.IOException: This drive is locked by BitLocker Drive Encryption. You must unlock this drive from Control Panel. : 'D:\'"
            }
            catch (UnauthorizedAccessException)
            {
                // checking attributes on access-controlled directories will throw an error so we handle it here to prevent a crash
            }
        }

        protected override bool OnClick(ClickEvent e)
        {
            currentDirectory.Value = Directory;
            return true;
        }
    }
}
