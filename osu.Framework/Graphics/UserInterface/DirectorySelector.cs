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
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A component which allows a user to select a directory.
    /// </summary>
    public abstract class DirectorySelector : CompositeDrawable
    {
        private FillFlowContainer directoryFlow;

        protected readonly BindableBool ShowHiddenItems = new BindableBool();

        protected abstract ScrollContainer<Drawable> CreateScrollContainer();

        /// <summary>
        /// Create the breadcrumb part of the control.
        /// </summary>
        protected abstract DirectorySelectorBreadcrumbDisplay CreateBreadcrumb();

        /// <summary>
        /// Create a button that toggles the display of hidden items.
        /// </summary>
        /// <remarks>
        /// Unless overridden, a toggle button will not be added.
        /// </remarks>
        protected virtual Drawable CreateHiddenToggleButton() => Empty();

        protected abstract DirectorySelectorDirectory CreateDirectoryItem(DirectoryInfo directory, string displayName = null);

        /// <summary>
        /// Create the directory item that resolves the parent directory.
        /// </summary>
        protected abstract DirectorySelectorDirectory CreateParentDirectoryItem(DirectoryInfo directory);

        [Cached]
        public readonly Bindable<DirectoryInfo> CurrentPath = new Bindable<DirectoryInfo>();

        private string initialPath;

        protected DirectorySelector(string initialPath = null)
        {
            this.initialPath = initialPath;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            CurrentPath.Value = new DirectoryInfo(initialPath);
        }

        [BackgroundDependencyLoader]
        private void load(GameHost gameHost)
        {
            initialPath ??= gameHost.InitialFileSelectorPath;

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
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            ColumnDimensions = new[]
                            {
                                new Dimension(),
                                new Dimension(GridSizeMode.AutoSize),
                            },
                            RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                            Content = new[]
                            {
                                new[]
                                {
                                    CreateBreadcrumb(),
                                    CreateHiddenToggleButton()
                                }
                            }
                        }
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

            ShowHiddenItems.ValueChanged += _ => updateDisplay();
            CurrentPath.BindValueChanged(_ => updateDisplay(), true);
        }

        /// <summary>
        /// Because <see cref="CurrentPath"/> changes may not necessarily lead to directories that exist/are accessible,
        /// <see cref="updateDisplay"/> may need to change <see cref="CurrentPath"/> again to lead to a directory that is actually accessible.
        /// This flag intends to prevent recursive <see cref="updateDisplay"/> calls from taking place during the process of finding an accessible directory.
        /// </summary>
        private bool directoryChanging;

        private void updateDisplay()
        {
            if (directoryChanging)
                return;

            try
            {
                directoryChanging = true;

                directoryFlow.Clear();

                var newDirectory = CurrentPath.Value;
                bool notifyError = false;
                ICollection<DirectorySelectorItem> items = Array.Empty<DirectorySelectorItem>();

                while (newDirectory != null)
                {
                    newDirectory.Refresh();

                    if (TryGetEntriesForPath(newDirectory, out items))
                        break;

                    notifyError = true;
                    newDirectory = newDirectory.Parent;
                }

                if (notifyError)
                    NotifySelectionError();

                if (newDirectory == null)
                {
                    var drives = DriveInfo.GetDrives();

                    foreach (var drive in drives)
                        directoryFlow.Add(CreateDirectoryItem(drive.RootDirectory));

                    return;
                }

                CurrentPath.Value = newDirectory;

                directoryFlow.Add(CreateParentDirectoryItem(newDirectory.Parent));
                directoryFlow.AddRange(items);
            }
            finally
            {
                directoryChanging = false;
            }
        }

        /// <summary>
        /// Attempts to create entries to display for the given <paramref name="path"/>.
        /// A return value of <see langword="false"/> is used to indicate a non-specific I/O failure, signaling to the selector that it should attempt
        /// to find another directory to display (since <paramref name="path"/> is inaccessible).
        /// </summary>
        /// <param name="path">The directory to create entries for.</param>
        /// <param name="items">
        /// The created <see cref="DirectorySelectorItem"/>s, provided that the <paramref name="path"/> could be entered.
        /// Not valid for reading if the return value of the method is <see langword="false"/>.
        /// </param>
        protected virtual bool TryGetEntriesForPath(DirectoryInfo path, out ICollection<DirectorySelectorItem> items)
        {
            items = new List<DirectorySelectorItem>();

            try
            {
                foreach (var dir in path.GetDirectories().OrderBy(d => d.Name))
                {
                    if (ShowHiddenItems.Value || !dir.Attributes.HasFlagFast(FileAttributes.Hidden))
                        items.Add(CreateDirectoryItem(dir));
                }

                return true;
            }
            catch
            {
                return false;
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
