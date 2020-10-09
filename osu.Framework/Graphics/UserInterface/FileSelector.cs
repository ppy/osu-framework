// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class FileSelector : DirectorySelector
    {
        private readonly string[] validFileExtensions;
        protected abstract FilePiece CreateFilePiece(FileInfo file);

        [Cached]
        public readonly Bindable<FileInfo> CurrentFile = new Bindable<FileInfo>();

        public FileSelector(string initialPath = null, string[] validFileExtensions = null)
            : base(initialPath)
        {
            this.validFileExtensions = validFileExtensions ?? Array.Empty<string>();
        }

        protected override IEnumerable<DirectoryDisplayPiece> GetEntriesForPath(DirectoryInfo path)
        {
            foreach (var dir in base.GetEntriesForPath(path))
                yield return dir;

            IEnumerable<FileInfo> files = path.GetFiles();

            if (validFileExtensions.Length > 0)
                files = files.Where(f => validFileExtensions.Contains(f.Extension));

            foreach (var file in files.OrderBy(d => d.Name))
            {
                if ((file.Attributes & FileAttributes.Hidden) == 0)
                    yield return CreateFilePiece(file);
            }
        }

        protected abstract class FilePiece : DirectoryDisplayPiece
        {
            protected readonly FileInfo File;

            [Resolved]
            private Bindable<FileInfo> currentFile { get; set; }

            public FilePiece(FileInfo file)
            {
                File = file;
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