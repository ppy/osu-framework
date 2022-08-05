// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Visualisation
{
    internal class VisualisedDrawable : Container, IContainVisualisedDrawables, IHasFilterableChildren
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

                updateColours();
                if (value)
                    Expand();
            }
        }

        public IEnumerable<LocalisableString> FilterTerms => new LocalisableString[]
        {
            Target.ToString()
        };

        public IEnumerable<IFilterable> FilterableChildren => flow.Children;

        public bool FilteringActive { get; set; }

        private bool matchingFilter = true;

        public bool MatchingFilter
        {
            get => matchingFilter;
            set
            {
                bool wasPresent = IsPresent;

                matchingFilter = value;

                if (IsPresent != wasPresent)
                    Invalidate(Invalidation.Presence);
            }
        }

        public override bool IsPresent => base.IsPresent && MatchingFilter;

        public Action<Drawable> RequestTarget;
        public Action<VisualisedDrawable> HighlightTarget;

        private Box background;
        private SpriteText text;
        private SpriteText text2;
        private Drawable previewBox;
        private Drawable activityInvalidate;
        private Drawable activityAutosize;
        private Drawable activityLayout;
        private VisualisedDrawableFlow flow;
        private Container connectionContainer;

        private const float row_width = 10;
        private const float row_height = 20;

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

            AddRange(new Drawable[]
            {
                flow = new VisualisedDrawableFlow
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Position = new Vector2(row_width, row_height)
                },
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(100, 1), // a bit of a hack, but works well enough.
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = Color4.Transparent,
                        },
                        activityInvalidate = new Box
                        {
                            Colour = Color4.Yellow,
                            Size = new Vector2(2, line_height),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Position = new Vector2(6, 0),
                            Alpha = 0
                        },
                        activityLayout = new Box
                        {
                            Colour = Color4.Orange,
                            Size = new Vector2(2, line_height),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Position = new Vector2(3, 0),
                            Alpha = 0
                        },
                        activityAutosize = new Box
                        {
                            Colour = Color4.Red,
                            Size = new Vector2(2, line_height),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Position = new Vector2(0, 0),
                            Alpha = 0
                        },
                        previewBox = spriteTarget?.Texture == null
                            ? previewBox = new Box
                            {
                                Colour = Color4.White,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                            }
                            : new Sprite
                            {
                                // It's fine to only bypass the ref count, because this sprite will dispose along with the original sprite
                                Texture = new Texture(spriteTarget.Texture),
                                Scale = new Vector2(spriteTarget.Texture.DisplayWidth / spriteTarget.Texture.DisplayHeight, 1),
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                            },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5),
                            Position = new Vector2(24, 0),
                            Children = new Drawable[]
                            {
                                text = new SpriteText { Font = FrameworkFont.Regular },
                                text2 = new SpriteText { Font = FrameworkFont.Regular },
                            }
                        },
                    }
                },
            });

            const float connection_width = 1;

            AddInternal(connectionContainer = new Container
            {
                Colour = FrameworkColour.Green,
                RelativeSizeAxes = Axes.Y,
                Width = connection_width,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        EdgeSmoothness = new Vector2(0.5f),
                    },
                    new Box
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.CentreLeft,
                        Y = row_height / 2,
                        Width = row_width / 2,
                        EdgeSmoothness = new Vector2(0.5f),
                    }
                }
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
            updateColours();
        }

        public bool TopLevel
        {
            set => connectionContainer.Alpha = value ? 0 : 1;
        }

        private void attachEvents()
        {
            Target.Invalidated += onInvalidated;
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
            Target.Invalidated -= onInvalidated;
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
            detachEvents();
            base.Dispose(isDisposing);
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateColours();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateColours();
            base.OnHoverLost(e);
        }

        private void updateColours()
        {
            if (isHighlighted)
            {
                background.Colour = FrameworkColour.YellowGreen;
                text.Colour = FrameworkColour.Blue;
                text2.Colour = FrameworkColour.Blue;
            }
            else if (IsHovered)
            {
                background.Colour = FrameworkColour.BlueGreen;
                text.Colour = Color4.White;
                text2.Colour = FrameworkColour.YellowGreen;
            }
            else
            {
                background.Colour = Color4.Transparent;
                text.Colour = Color4.White;
                text2.Colour = FrameworkColour.YellowGreen;
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == MouseButton.Right)
            {
                HighlightTarget?.Invoke(this);
                return true;
            }

            return false;
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (isExpanded)
                Collapse();
            else
                Expand();
            return true;
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
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

        private void onAutoSize() => activityAutosize.FadeOutFromOne(1);

        private void onLayout() => activityLayout.FadeOutFromOne(1);

        private void onInvalidated(Drawable d) => activityInvalidate.FadeOutFromOne(1);

        private void onDispose()
        {
            // May come from the disposal thread, in which case they won't ever be reused and the container doesn't need to be reset
            Schedule(() => SetContainer(null));
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

            text.Text = Target.ToString();
            text2.Text = $"({Target.DrawPosition.X:#,0},{Target.DrawPosition.Y:#,0}) {Target.DrawSize.X:#,0}x{Target.DrawSize.Y:#,0}"
                         + (!isExpanded && childCount > 0 ? $@" ({childCount} children)" : string.Empty);

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
