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
    public abstract class DirectorySelector : CompositeDrawable
    {
        private FillFlowContainer directoryFlow;

        protected abstract ScrollContainer<Drawable> CreateScrollContainer();
        protected abstract DirectoryListingBreadcumb CreateBreadcrumb();
        protected abstract DirectoryListingDirectory CreateDirectoryItem(DirectoryInfo directory, string displayName = null);
        protected abstract DirectoryListingDirectory CreateParentDirectoryItem(DirectoryInfo directory);

        [Cached]
        public readonly Bindable<DirectoryInfo> CurrentPath = new Bindable<DirectoryInfo>();

        public DirectorySelector(string initialPath = null)
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

        protected virtual IEnumerable<DirectoryListingItem> GetEntriesForPath(DirectoryInfo path)
        {
            foreach (var dir in path.GetDirectories().OrderBy(d => d.Name))
            {
                if ((dir.Attributes & FileAttributes.Hidden) == 0)
                    yield return CreateDirectoryItem(dir);
            }
        }

        protected virtual void NotifySelectionError()
        {
        }
    }
}