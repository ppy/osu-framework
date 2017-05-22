// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Visualisation
{
    internal class VisualisedDrawable : Container
    {
        public class NestingDepthComparer : IComparer<VisualisedDrawable>
        {
            public int Compare(VisualisedDrawable x, VisualisedDrawable y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                return x.nestingDepth.CompareTo(y.nestingDepth);
            }
        }

        public static IComparer<VisualisedDrawable> Comparer => new NestingDepthComparer();

        public Drawable Target { get; }

        private readonly Box textBg;
        private readonly SpriteText text;
        private readonly Drawable previewBox;
        private readonly Drawable activityInvalidate;
        private readonly Drawable activityAutosize;
        private readonly Drawable activityLayout;

        public Action HoverGained;
        public Action HoverLost;

        public Action RequestTarget;

        private const int line_height = 12;

        public FillFlowContainer<VisualisedDrawable> Flow;

        private readonly TreeContainer tree;

        private readonly int nestingDepth;

        public VisualisedDrawable(VisualisedDrawable parent, Drawable d, TreeContainer tree)
        {
            this.tree = tree;

            nestingDepth = (parent?.nestingDepth ?? 0) + 1;
            Target = d;

            attachEvents();

            var sprite = Target as Sprite;

            AutoSizeAxes = Axes.Both;
            Add(new[]
            {
                activityInvalidate = new Box
                {
                    Colour = Color4.Yellow,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(6, 0),
                    Alpha = 0
                },
                activityLayout = new Box
                {
                    Colour = Color4.Orange,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(3, 0),
                    Alpha = 0
                },
                activityAutosize = new Box
                {
                    Colour = Color4.Red,
                    Size = new Vector2(2, line_height),
                    Position = new Vector2(0, 0),
                    Alpha = 0
                },
                previewBox = sprite?.Texture == null
                    ? previewBox = new Box { Colour = Color4.White }
                    : new Sprite
                    {
                        Texture = sprite.Texture,
                        Scale = new Vector2(sprite.Texture.DisplayWidth / sprite.Texture.DisplayHeight, 1),
                    },
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Position = new Vector2(24, -3),
                    Children = new Drawable[]
                    {
                        textBg = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(1, 0.8f),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Colour = Color4.Transparent,
                        },
                        text = new SpriteText
                        {
                            Scale = new Vector2(0.9f),
                        },
                    }
                },
                Flow = new FillFlowContainer<VisualisedDrawable>
                {
                    Direction = FillDirection.Vertical,
                    AutoSizeAxes = Axes.Both,
                    Position = new Vector2(10, 14)
                },
            });

            previewBox.Position = new Vector2(9, 0);
            previewBox.Size = new Vector2(line_height, line_height);

            updateSpecifics();
        }

        private void attachEvents()
        {
            Target.OnInvalidate += onInvalidate;

            var da = Target as Container<Drawable>;
            if (da != null) da.OnAutoSize += onAutoSize;

            var df = Target as FlowContainer<Drawable>;
            if (df != null) df.OnLayout += onLayout;
        }

        private void detachEvents()
        {
            Target.OnInvalidate -= onInvalidate;

            var da = Target as Container<Drawable>;
            if (da != null) da.OnAutoSize -= onAutoSize;

            var df = Target as FlowContainer<Drawable>;
            if (df != null) df.OnLayout -= onLayout;
        }

        public override bool DisposeOnDeathRemoval => true;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            detachEvents();
        }

        protected override bool OnHover(InputState state)
        {
            HoverGained?.Invoke();
            textBg.Colour = Color4.PaleVioletRed.Opacity(0.7f);
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            HoverLost?.Invoke();
            textBg.Colour = Color4.Transparent;
            base.OnHoverLost(state);
        }

        protected override bool OnClick(InputState state)
        {
            Flow.Alpha = Flow.Alpha > 0 ? 0 : 1;
            updateSpecifics();
            return true;
        }

        protected override bool OnDoubleClick(InputState state)
        {
            RequestTarget?.Invoke();
            return true;
        }

        private void onAutoSize()
        {
            Scheduler.Add(() => activityAutosize.FadeOutFromOne(1));
        }

        private void onLayout()
        {
            Scheduler.Add(() => activityLayout.FadeOutFromOne(1));
        }

        private void onInvalidate(Drawable d)
        {
            Scheduler.Add(() => activityInvalidate.FadeOutFromOne(1));
        }

        private void updateSpecifics()
        {
            Vector2 posInTree = ToSpaceOfOtherDrawable(Vector2.Zero, tree);
            if (posInTree.Y < -previewBox.DrawHeight || posInTree.Y > tree.Height)
            {
                text.Text = string.Empty;
                return;
            }

            previewBox.Alpha = Math.Max(0.2f, Target.Alpha);
            previewBox.ColourInfo = Target.ColourInfo;

            int childCount = (Target as IContainerEnumerable<Drawable>)?.Children.Count() ?? 0;

            text.Text = Target + (!Flow.IsPresent && childCount > 0 ? $@" ({childCount} children)" : string.Empty);
        }

        protected override void Update()
        {
            updateSpecifics();

            text.Colour = !Flow.IsPresent ? Color4.LightBlue : Color4.White;
            base.Update();
        }

        public bool CheckExpiry()
        {
            if (!IsAlive) return false;

            if (!Target.IsAlive || Target.Parent == null || !Target.IsPresent)
            {
                Expire();
                return false;
            }

            Alpha = Target.IsPresent ? 1 : 0.3f;
            return true;
        }
    }
}
