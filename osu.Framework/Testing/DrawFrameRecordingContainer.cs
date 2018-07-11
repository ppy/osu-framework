// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Testing
{
    internal class DrawFrameRecordingContainer : Container
    {
        private readonly Bindable<TestBrowser.PlaybackState> playback = new Bindable<TestBrowser.PlaybackState>();
        private readonly BindableInt currentFrame = new BindableInt();

        private readonly List<DrawNode> recordedFrames = new List<DrawNode>();

        [BackgroundDependencyLoader]
        private void load(TestBrowser.PlaybackBindable playback, TestBrowser.FrameBindable currentFrame)
        {
            this.playback.BindTo(playback);
            this.currentFrame.BindTo(currentFrame);
        }

        protected override bool CanBeFlattened => false;

        internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
        {
            switch (playback.Value)
            {
                default:
                case TestBrowser.PlaybackState.Normal:
                    recordedFrames.Clear();
                    currentFrame.Value = currentFrame.MaxValue = 0;

                    return base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);
                case TestBrowser.PlaybackState.Recording:
                    var node = base.GenerateDrawNodeSubtree(frame, treeIndex, true);

                    recordedFrames.Add(node);
                    currentFrame.Value = currentFrame.MaxValue = recordedFrames.Count - 1;

                    return node;
                case TestBrowser.PlaybackState.Stopped:
                    return recordedFrames[currentFrame.Value];
            }
        }
    }
}
