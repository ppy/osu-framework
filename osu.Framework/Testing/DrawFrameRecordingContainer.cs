// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Testing
{
    internal class DrawFrameRecordingContainer : Container
    {
        private readonly Bindable<RecordState> recordState = new Bindable<RecordState>();
        private readonly BindableInt currentFrame = new BindableInt();
        private readonly List<DrawNode> recordedFrames = new List<DrawNode>();

        protected override Container<Drawable> Content => content;

        private readonly Container content;

        public DrawFrameRecordingContainer()
        {
            InternalChildren = new Drawable[]
            {
                new InputCapturingDrawable { RelativeSizeAxes = Axes.Both },
                content = new Container { RelativeSizeAxes = Axes.Both }
            };
        }

        [BackgroundDependencyLoader(true)]
        private void load([CanBeNull] TestBrowser browser)
        {
            if (browser != null)
            {
                recordState.BindTo(browser.RecordState);
                currentFrame.BindTo(browser.CurrentFrame);
            }
        }

        protected override bool CanBeFlattened => false;

        internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            switch (recordState.Value)
            {
                default:
                case RecordState.Normal:
                    foreach (var drawNode in recordedFrames)
                        disposeRecursively(drawNode);

                    recordedFrames.Clear();

                    currentFrame.Value = currentFrame.MaxValue = 0;

                    return base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);

                case RecordState.Recording:
                    var node = base.GenerateDrawNodeSubtree(frame, treeIndex, true);

                    referenceRecursively(node);

                    recordedFrames.Add(node);
                    currentFrame.Value = currentFrame.MaxValue = recordedFrames.Count - 1;

                    return node;

                case RecordState.Stopped:
                    return recordedFrames[currentFrame.Value];
            }
        }

        private void referenceRecursively(DrawNode drawNode)
        {
            drawNode.Reference();

            if (!(drawNode is ICompositeDrawNode composite))
                return;

            foreach (var child in composite.Children)
                referenceRecursively(child);
        }

        private void disposeRecursively(DrawNode drawNode)
        {
            drawNode.Dispose();

            if (!(drawNode is ICompositeDrawNode composite))
                return;

            foreach (var child in composite.Children)
                disposeRecursively(child);
        }

        // An empty drawable which captures DrawVisualiser input in this container
        private class InputCapturingDrawable : Drawable
        {
            // Required for the DrawVisualiser to not treat this Drawable as an overlay input receptor
            // ReSharper disable once RedundantOverriddenMember
            protected override DrawNode CreateDrawNode() => base.CreateDrawNode();
        }
    }
}
