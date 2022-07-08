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

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A component which allows a user to select a file.
    /// </summary>
    public abstract class FileSelector : DirectorySelector
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
            items = new List<DirectorySelectorItem>();

            if (!base.TryGetEntriesForPath(path, out var directories))
                return false;

            items = directories;

            try
            {
                IEnumerable<FileInfo> files = path.GetFiles();

                if (validFileExtensions.Length > 0)
                    files = files.Where(f => validFileExtensions.Contains(f.Extension));

                foreach (var file in files.OrderBy(d => d.Name))
                {
                    if (ShowHiddenItems.Value || !file.Attributes.HasFlagFast(FileAttributes.Hidden))
                        items.Add(CreateFileItem(file));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        protected abstract class DirectoryListingFile : DirectorySelectorItem
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
