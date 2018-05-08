// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using OpenTK.Input;
using osu.Framework.Graphics.Shapes;
using System.Collections.Generic;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Graphics.Visualisation
{
    internal class VisualisedDrawable : Container
    {
        public Drawable Target { get; }

        private bool isHighlighted;

        public bool IsHighlighted
        {
            get
            {
                return isHighlighted;
            }
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

        private readonly Box background;
        private readonly Box highlightBackground;
        private readonly SpriteText text;
        private readonly Drawable previewBox;
        private readonly Drawable activityInvalidate;
        private readonly Drawable activityAutosize;
        private readonly Drawable activityLayout;

        public Action<Drawable> RequestTarget;
        public Action<VisualisedDrawable> HighlightTarget;

        private const int line_height = 12;

        private readonly FillFlowContainer<VisualisedDrawable> flow;
        private readonly TreeContainer tree;

        public VisualisedDrawable(Drawable d, TreeContainer tree)
        {
            this.tree = tree;

            Target = d;

            attachEvents();

            var sprite = Target as Sprite;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
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
                flow = new FillFlowContainer<VisualisedDrawable>
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

        private void attachEvents()
        {
            Target.OnInvalidate += onInvalidate;

            if (Target is Container<Drawable> da)
            {
                da.OnAutoSize += onAutoSize;
                da.ChildBecameAlive += addChild;
                da.ChildDied += removeChild;
            }

            if (Target is FlowContainer<Drawable> df) df.OnLayout += onLayout;
        }

        private void detachEvents()
        {
            Target.OnInvalidate -= onInvalidate;

            if (Target is Container<Drawable> da)
            {
                da.OnAutoSize -= onAutoSize;
                da.ChildBecameAlive -= addChild;
                da.ChildDied -= removeChild;
            }

            if (Target is FlowContainer<Drawable> df) df.OnLayout -= onLayout;
        }

        private readonly Dictionary<Drawable, VisualisedDrawable> visCache = new Dictionary<Drawable, VisualisedDrawable>();
        private void addChild(Drawable drawable)
        {
            // Make sure to never add the DrawVisualiser (recursive scenario)
            if (drawable is DrawVisualiser) return;

            // Don't add individual characters of SpriteText
            if (Target is SpriteText) return;

            if (!visCache.TryGetValue(drawable, out VisualisedDrawable vis))
            {
                vis = visCache[drawable] = new VisualisedDrawable(drawable, tree)
                {
                    RequestTarget = d => RequestTarget?.Invoke(d),
                    HighlightTarget = d => HighlightTarget?.Invoke(d)
                };
            }

            flow.Add(vis);
        }

        private void removeChild(Drawable drawable)
        {
            if (!visCache.ContainsKey(drawable))
                return;
            flow.Remove(visCache[drawable]);
        }

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
            else Expand();

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
    }
}
