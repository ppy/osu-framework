// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A component which allows a user to select a directory.
    /// </summary>
    public abstract class DirectorySelector : CompositeDrawable
    {
        private FillFlowContainer directoryFlow;

        protected abstract ScrollContainer<Drawable> CreateScrollContainer();

        /// <summary>
        /// Create the breadcrumb part of the control.
        /// </summary>
        protected abstract DirectorySelectorBreadcrumbDisplay CreateBreadcrumb();

        protected abstract DirectorySelectorDirectory CreateDirectoryItem(DirectoryInfo directory, string displayName = null);

        /// <summary>
        /// Create the directory item that resolves the parent directory.
        /// </summary>
        protected abstract DirectorySelectorDirectory CreateParentDirectoryItem(DirectoryInfo directory);

        [Cached]
        public readonly Bindable<DirectoryInfo> CurrentPath = new Bindable<DirectoryInfo>();

        protected DirectorySelector(string initialPath = null)
        {
            CurrentPath.Value = new DirectoryInfo(initialPath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        CreateBreadcrumb()
                    },
                    new Drawable[]
                    {
                        CreateScrollContainer().With(d =>
                        {
                            d.RelativeSizeAxes = Axes.Both;
                            d.Child = directoryFlow = new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Y,
                                RelativeSizeAxes = Axes.X,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(2),
                            };
                        })
                    }
                }
            };

            CurrentPath.BindValueChanged(updateDisplay, true);
        }

        private void updateDisplay(ValueChangedEvent<DirectoryInfo> directory)
        {
            directoryFlow.Clear();

            try
            {
                if (directory.NewValue == null)
                {
                    var drives = DriveInfo.GetDrives();

                    foreach (var drive in drives)
                        directoryFlow.Add(CreateDirectoryItem(drive.RootDirectory));
                }
                else
                {
                    directoryFlow.Add(CreateParentDirectoryItem(CurrentPath.Value.Parent));

                    directoryFlow.AddRange(GetEntriesForPath(CurrentPath.Value));
                }
            }
            catch (Exception)
            {
                CurrentPath.Value = directory.OldValue;
                NotifySelectionError();
            }
        }

        /// <summary>
        /// Creates entries for a given directory.
        /// </summary>
        protected virtual IEnumerable<DirectorySelectorItem> GetEntriesForPath(DirectoryInfo path)
        {
            foreach (var dir in path.GetDirectories().OrderBy(d => d.Name))
            {
                if ((dir.Attributes & FileAttributes.Hidden) == 0)
                    yield return CreateDirectoryItem(dir);
            }
        }

        /// <summary>
        /// Called when an error has occured. Usually happens when trying to access protected directories.
        /// </summary>
        protected virtual void NotifySelectionError()
        {
        }
    }
}
