// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Testing
{
    public abstract class DrawFrameTestCase : TestCase
    {
        protected override Container<Drawable> Content => frameContainer ?? base.Content;
        private readonly FrameContainer frameContainer;

        private readonly BindableInt frameBindable = new BindableInt
        {
            MinValue = 0,
            MaxValue = 0
        };

        private readonly BasicSliderBar<int> frameSlider;
        private readonly Button recordButton;

        protected DrawFrameTestCase()
        {

            base.Content.AddRange(new Drawable[]
            {
                frameContainer = new FrameContainer { RelativeSizeAxes = Axes.Both },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Y,
                    Width = 100,
                    Spacing = new Vector2(0, 5),
                    Children = new Drawable[]
                    {
                        recordButton = new Button
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 25,
                            BackgroundColour = Color4.SlateGray,
                            Text = "start recording",
                            Action = toggleRecording
                        },
                        frameSlider = new BasicSliderBar<int>
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 25,
                        },
                        new Button
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 25,
                            BackgroundColour = Color4.SlateGray,
                            Text = "prev",
                            Action = () => frameContainer.CurrentFrame--
                        },
                        new Button
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 25,
                            BackgroundColour = Color4.SlateGray,
                            Text = "next",
                            Action = () => frameContainer.CurrentFrame++
                        },
                    }
                }

            });

            frameSlider.Current.BindTo(frameBindable);
            frameBindable.BindValueChanged(v => frameContainer.CurrentFrame = v);
        }

        private void toggleRecording()
        {
            frameContainer.Recording = !frameContainer.Recording;

            if (frameContainer.Recording)
            {
                frameBindable.Value = 0;
                frameBindable.MaxValue = 0;
                recordButton.Text = "stop recording";
            }
            else
            {
                frameBindable.MaxValue = frameContainer.TotalFrames;
                recordButton.Text = "start recording";
            }
        }

        private class FrameContainer : Container
        {
            public int CurrentFrame;

            public int TotalFrames => frames.Count;
            private readonly List<DrawNode> frames = new List<DrawNode>();

            private bool recording;
            public bool Recording
            {
                get => recording;
                set
                {
                    recording = value;

                    if (value)
                        frames.Clear();
                }
            }

            protected override void Update()
            {
                base.Update();

                Invalidate(Invalidation.DrawNode);
            }

            protected override bool CanBeFlattened => false;

            internal override DrawNode GenerateDrawNodeSubtree(ulong frame, int treeIndex, bool forceNewDrawNode)
            {
                if (Recording)
                {
                    var node = base.GenerateDrawNodeSubtree(frame, treeIndex, true);
                    frames.Add(node);
                    return node;
                }

                if (frames.Count == 0)
                    return base.GenerateDrawNodeSubtree(frame, treeIndex, forceNewDrawNode);

                return frames[MathHelper.Clamp(CurrentFrame, 0, frames.Count - 1)];
            }
        }
    }
}
