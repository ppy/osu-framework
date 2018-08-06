// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using OpenTK.Input;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.States;

namespace osu.Framework.Graphics.Visualisation
{
    internal class VisualisedDrawable : Container, IContainVisualisedDrawables
    {
        private const int line_height = 12;

        public Drawable Target { get; }

        private bool isHighlighted;

        public bool IsHighlighted
        {
            get => isHighlighted;
            set
            {
                isHighlighted = value;

                if (value)
                {
                    highlightBackground.FadeIn();
                    Expand();
                }
                else
                {
                    highlightBackground.FadeOut();
                }
            }
        }

        public Action<Drawable> RequestTarget;
        public Action<VisualisedDrawable> HighlightTarget;

        private Box background;
        private Box highlightBackground;
        private SpriteText text;
        private Drawable previewBox;
        private Drawable activityInvalidate;
        private Drawable activityAutosize;
        private Drawable activityLayout;
        private VisualisedDrawableFlow flow;

        [Resolved]
        private DrawVisualiser visualiser { get; set; }

        [Resolved]
        private TreeContainer tree { get; set; }

        public VisualisedDrawable(Drawable d)
        {
            Target = d;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            var spriteTarget = Target as Sprite;

            AddRange(new[]
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
                previewBox = spriteTarget?.Texture == null
                    ? previewBox = new Box { Colour = Color4.White }
                    : new Sprite
                    {
                        Texture = spriteTarget.Texture,
                        Scale = new Vector2(spriteTarget.Texture.DisplayWidth / spriteTarget.Texture.DisplayHeight, 1),
                    },
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Position = new Vector2(24, -3),
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(1, 0.8f),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Colour = Color4.Transparent,
                        },
                        highlightBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(1, 0.8f),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Colour = Color4.Khaki.Opacity(0.4f),
                            Alpha = 0
                        },
                        text = new SpriteText()
                    }
                },
                flow = new VisualisedDrawableFlow
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Position = new Vector2(10, 14)
                },
            });

            previewBox.Position = new Vector2(9, 0);
            previewBox.Size = new Vector2(line_height, line_height);

            var compositeTarget = Target as CompositeDrawable;
            compositeTarget?.AliveInternalChildren.ForEach(addChild);

            updateSpecifics();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            attachEvents();
        }

        private void attachEvents()
        {
            Target.OnInvalidate += onInvalidate;
            Target.OnDispose += onDispose;

            if (Target is CompositeDrawable da)
            {
                da.OnAutoSize += onAutoSize;
                da.ChildBecameAlive += addChild;
                da.ChildDied += removeChild;
                da.ChildDepthChanged += depthChanged;
            }

            if (Target is FlowContainer<Drawable> df) df.OnLayout += onLayout;
        }

        private void detachEvents()
        {
            Target.OnInvalidate -= onInvalidate;
            Target.OnDispose -= onDispose;

            if (Target is CompositeDrawable da)
            {
                da.OnAutoSize -= onAutoSize;
                da.ChildBecameAlive -= addChild;
                da.ChildDied -= removeChild;
                da.ChildDepthChanged -= depthChanged;
            }

            if (Target is FlowContainer<Drawable> df) df.OnLayout -= onLayout;
        }

        private void addChild(Drawable drawable)
        {
            // Make sure to never add the DrawVisualiser (recursive scenario)
            if (drawable == visualiser) return;

            // Don't add individual characters of SpriteText
            if (Target is SpriteText) return;

            visualiser.GetVisualiserFor(drawable).SetContainer(this);
        }

        private void removeChild(Drawable drawable)
        {
            var vis = visualiser.GetVisualiserFor(drawable);
            if (vis.currentContainer == this)
                vis.SetContainer(null);
        }

        private void depthChanged(Drawable drawable)
        {
            var vis = visualiser.GetVisualiserFor(drawable);

            vis.currentContainer?.RemoveVisualiser(vis);
            vis.currentContainer?.AddVisualiser(vis);
        }

        void IContainVisualisedDrawables.AddVisualiser(VisualisedDrawable visualiser)
        {
            visualiser.RequestTarget = d => RequestTarget?.Invoke(d);
            visualiser.HighlightTarget = d => HighlightTarget?.Invoke(d);

            visualiser.Depth = visualiser.Target.Depth;

            flow.Add(visualiser);
        }

        void IContainVisualisedDrawables.RemoveVisualiser(VisualisedDrawable visualiser) => flow.Remove(visualiser);

        public VisualisedDrawable FindVisualisedDrawable(Drawable drawable)
        {
            if (drawable == Target)
                return this;

            foreach (var child in flow)
            {
                var vis = child.FindVisualisedDrawable(drawable);
                if (vis != null)
                    return vis;
            }

            return null;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            detachEvents();
        }

        protected override bool OnHover(InputState state)
        {
            background.Colour = Color4.PaleVioletRed.Opacity(0.7f);
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            background.Colour = Color4.Transparent;
            base.OnHoverLost(state);
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            if (args.Button == MouseButton.Right)
            {
                HighlightTarget?.Invoke(this);
                return true;
            }
            return false;
        }

        protected override bool OnClick(InputState state)
        {
            if (isExpanded)
                Collapse();
            else
                Expand();
            return true;
        }

        protected override bool OnDoubleClick(InputState state)
        {
            RequestTarget?.Invoke(Target);
            return true;
        }

        private bool isExpanded = true;

        public void Expand()
        {
            flow.FadeIn();
            updateSpecifics();

            isExpanded = true;
        }

        public void ExpandAll()
        {
            Expand();
            flow.ForEach(f => f.Expand());
        }

        public void Collapse()
        {
            flow.FadeOut();
            updateSpecifics();

            isExpanded = false;
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

        private void onDispose()
        {
            SetContainer(null);
            Dispose();
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
            previewBox.Colour = Target.Colour;

            int childCount = (Target as CompositeDrawable)?.InternalChildren.Count ?? 0;

            text.Text = Target + (!isExpanded && childCount > 0 ? $@" ({childCount} children)" : string.Empty);
            text.Colour = !isExpanded && childCount > 0 ? Color4.LightBlue : Color4.White;

            Alpha = Target.IsPresent ? 1 : 0.3f;
        }

        protected override void Update()
        {
            updateSpecifics();
            base.Update();
        }

        private IContainVisualisedDrawables currentContainer;

        /// <summary>
        /// Moves this <see cref="VisualisedDrawable"/> to be contained by another target.
        /// </summary>
        /// <remarks>
        /// The <see cref="VisualisedDrawable"/> is first removed from its current container via <see cref="IContainVisualisedDrawables.RemoveVisualiser"/>,
        /// prior to being added to the new container via <see cref="IContainVisualisedDrawables.AddVisualiser"/>.
        /// </remarks>
        /// <param name="container">The target which should contain this <see cref="VisualisedDrawable"/>.</param>
        public void SetContainer(IContainVisualisedDrawables container)
        {
            currentContainer?.RemoveVisualiser(this);

            // The visualised may have previously been within a container (e.g. flow), which repositioned it
            // We should make sure that the position is reset before it's added to another container
            Y = 0;

            container?.AddVisualiser(this);

            currentContainer = container;
        }

        private class VisualisedDrawableFlow : FillFlowContainer<VisualisedDrawable>
        {
            public override IEnumerable<Drawable> FlowingChildren => AliveInternalChildren.Where(d => d.IsPresent).OrderBy(d => -d.Depth).ThenBy(d => ((VisualisedDrawable)d).Target.ChildID);
        }
    }
}
