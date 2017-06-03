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
using OpenTK.Input;

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

        private readonly Box background;
        private readonly SpriteText text;
        private readonly Drawable previewBox;
        private readonly Drawable activityInvalidate;
        private readonly Drawable activityAutosize;
        private readonly Drawable activityLayout;

        public Action HoverGained;
        public Action HoverLost;

        public Action<VisualisedDrawable> HighlightTarget;
        public Action RequestTarget;

        private const int line_height = 12;

        public FillFlowContainer<VisualisedDrawable> Flow;

        private readonly DrawVisualiser viz;
        private readonly TreeContainer tree;

        private readonly int nestingDepth;

        public VisualisedDrawable(VisualisedDrawable parent, Drawable d, DrawVisualiser viz)
        {
            this.viz = viz;
            tree = viz.treeContainer;

            nestingDepth = (parent?.nestingDepth ?? 0) + 1;
            Target = d;

            attachEvents();

            var sprite = Target as Sprite;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Add(new[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Alpha = 0f,
                    Colour = Color4.White.Opacity(0.9f),        // Never make full opacity background
                },
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
                text = new SpriteText
                {
                    Position = new Vector2(24, -3),
                    Scale = new Vector2(0.9f),
                },
                Flow = new FillFlowContainer<VisualisedDrawable>
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Position = new Vector2(10, 14)
                },
            });

            previewBox.Position = new Vector2(9, 0);
            previewBox.Size = new Vector2(line_height, line_height);

            // Start collapsed
            Collapse();
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
            background.FadeTo(0.05f, 100);
            return false;
        }

        protected override void OnHoverLost(InputState state)
        {
            HoverLost?.Invoke();
            background.FadeOut(100);
            base.OnHoverLost(state);
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            if (state.Mouse.IsPressed(MouseButton.Right))
            {
                HighlightTarget?.Invoke(this);
                return true;
            }

            return base.OnMouseDown(state, args);
        }

        protected override bool OnClick(InputState state)
        {
            if (Flow.IsPresent)
                Collapse();

            else Expand();
            
            return true;
        }

        protected override bool OnDoubleClick(InputState state)
        {
            RequestTarget?.Invoke();
            return true;
        }

        public void Expand()
        {
            Flow.Alpha = 1f;
            updateSpecifics();
        }
        public void Collapse()
        {
            if (viz.highlighted == this)
                return;

            Flow.Alpha = 0f;
            updateSpecifics();
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

            text.Colour = !Flow.IsPresent && ((Target as IContainerEnumerable<Drawable>)?.Children.Count() ?? 0) > 0 ? Color4.LightBlue : Color4.White;
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
