// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DirectoryCurrentDisplay : CompositeDrawable
    {
        protected abstract DirectoryPiece CreateDirectoryPiece(DirectoryInfo directory, string displayName = null);
        protected abstract DirectoryPiece CreateComputerPiece();

        [Resolved]
        private Bindable<DirectoryInfo> currentDirectory { get; set; }

        private FillFlowContainer flow;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = flow = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(5),
                Direction = FillDirection.Horizontal,
            };

            currentDirectory.BindValueChanged(updateDisplay, true);
        }

        private void updateDisplay(ValueChangedEvent<DirectoryInfo> dir)
        {
            flow.Clear();

            List<DirectoryPiece> pathPieces = new List<DirectoryPiece>();

            DirectoryInfo ptr = dir.NewValue;

            while (ptr != null)
            {
                pathPieces.Insert(0, CreateDirectoryPiece(ptr));
                ptr = ptr.Parent;
            }

            flow.ChildrenEnumerable = new Drawable[]
            {
                CreateComputerPiece(),
            }.Concat(pathPieces);
        }
    }
}