// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Testing
{
    internal class DrawFrameRecordingContainer : Container
    {
        private readonly Bindable<RecordState> recordState = new Bindable<RecordState>();
        private readonly BindableInt currentFrame = new BindableInt();

        private readonly List<DrawNode> recordedFrames = new List<DrawNode>();

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
                    recordedFrames.Clear();
                    currentFrame.Value = currentFrame.MaxValue = 0;

                    return base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);
                case RecordState.Recording:
                    var node = base.GenerateDrawNodeSubtree(frame, treeIndex, true);

                    recordedFrames.Add(node);
                    currentFrame.Value = currentFrame.MaxValue = recordedFrames.Count - 1;

                    return node;
                case RecordState.Stopped:
                    return recordedFrames[currentFrame.Value];
            }
        }
    }
}
