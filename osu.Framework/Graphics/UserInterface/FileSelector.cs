// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input.Events;
using osu.Framework.Logging;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A component which allows a user to select a file.
    /// </summary>
    public abstract partial class FileSelector : DirectorySelector
    {
        private readonly string[] validFileExtensions;
        protected abstract DirectoryListingFile CreateFileItem(FileInfo file);

        [Cached]
        public readonly Bindable<FileInfo> CurrentFile = new Bindable<FileInfo>();

        protected FileSelector(string initialPath = null, string[] validFileExtensions = null)
            : base(initialPath)
        {
            this.validFileExtensions = validFileExtensions ?? Array.Empty<string>();
        }

        protected override bool TryGetEntriesForPath(DirectoryInfo path, out ICollection<DirectorySelectorItem> items)
        {
            bool gotAllEntries = true;
            items = new List<DirectorySelectorItem>();

            if (!base.TryGetEntriesForPath(path, out var directories))
                gotAllEntries = false;

            items = directories;

            try
            {
                IEnumerable<string> filenames = Directory.GetFiles(path.FullName).OrderBy(f => f);

                foreach (string filename in filenames)
                {
                    try
                    {
                        FileInfo file = new FileInfo(filename);

                        if (validFileExtensions.Length > 0 && !validFileExtensions.Contains(file.Extension))
                            continue;

                        if (ShowHiddenItems.Value || !file.Attributes.HasFlagFast(FileAttributes.Hidden))
                            items.Add(CreateFileItem(file));
                    }
                    catch
                    {
                        Logger.Log($"File {filename} is inaccessible", LoggingTarget.Information, LogLevel.Debug);
                        gotAllEntries = false;
                    }
                }

                return items.Count > 0 || gotAllEntries;
            }
            catch
            {
                return false;
            }
        }

        protected abstract partial class DirectoryListingFile : DirectorySelectorItem
        {
            protected readonly FileInfo File;

            [Resolved]
            private Bindable<FileInfo> currentFile { get; set; }

            protected DirectoryListingFile(FileInfo file)
            {
                File = file;

                try
                {
                    if (File?.Attributes.HasFlagFast(FileAttributes.Hidden) == true)
                        ApplyHiddenState();
                }
                catch (UnauthorizedAccessException)
                {
                    // checking attributes on access-controlled files will throw an error so we handle it here to prevent a crash
                }
            }

            protected override bool OnClick(ClickEvent e)
            {
                currentFile.Value = File;
                return true;
            }

            protected override string FallbackName => File.Name;
        }
    }
}
