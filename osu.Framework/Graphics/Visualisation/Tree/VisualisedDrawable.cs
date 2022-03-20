// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Textures;

using osuTK;
using osuTK.Graphics;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Represents drawables and components in their corresponding scene graph.
    /// </summary>
    internal class VisualisedDrawable : ElementNode
    {
        public Drawable TargetDrawable { get; private set; } = null!;

        public override object? Target
        {
            get => TargetDrawable;
            protected set => TargetDrawable = (Drawable?)value ?? throw new ArgumentNullException(nameof(value));
        }

        public VisualisedDrawable(Drawable d)
            : base(d)
        {
            OnAddVisualiser += visualiser => visualiser.Depth = (visualiser as VisualisedDrawable)?.TargetDrawable?.Depth ?? 0;
        }

        protected override Drawable CreatePreviewBox(object? target)
        {
            Sprite? spriteTarget = target as Sprite;
            if (spriteTarget?.Texture == null)
                return base.CreatePreviewBox(target);

            return new Sprite
            {
                // It's fine to only bypass the ref count, because this sprite will dispose along with the original sprite
                Texture = new Texture(spriteTarget.Texture.TextureGL),
                Scale = new Vector2(spriteTarget.Texture.DisplayWidth / spriteTarget.Texture.DisplayHeight, 1),
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
            };
        }

        protected override void LoadFrontDecorations()
        {
            Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
                Spacing = new Vector2(1),
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    ActivityAutosize = new Box
                    {
                        Colour = Color4.Red,
                        Size = new Vector2(2, LINE_HEIGHT),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AlwaysPresent = true,
                        Alpha = 0
                    },
                    ActivityLayout = new Box
                    {
                        Colour = Color4.Orange,
                        Size = new Vector2(2, LINE_HEIGHT),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AlwaysPresent = true,
                        Alpha = 0
                    },
                    ActivityInvalidate = new Box
                    {
                        Colour = Color4.Yellow,
                        Size = new Vector2(2, LINE_HEIGHT),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AlwaysPresent = true,
                        Alpha = 0
                    },
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            attachEvents();
        }


        private void attachEvents()
        {
            TargetDrawable.Invalidated += onInvalidated;
            TargetDrawable.OnDispose += onDispose;

            if (TargetDrawable is CompositeDrawable da)
            {
                da.OnAutoSize += onAutoSize;
                da.ChildBecameAlive += AddChild;
                da.ChildDied += RemoveChild;
                da.ChildDepthChanged += depthChanged;
            }

            if (TargetDrawable is FlowContainer<Drawable> df) df.OnLayout += onLayout;
        }

        private void detachEvents()
        {
            if (TargetDrawable != null)
            {
                TargetDrawable.Invalidated -= onInvalidated;
                TargetDrawable.OnDispose -= onDispose;
            }

            if (TargetDrawable is CompositeDrawable da)
            {
                da.OnAutoSize -= onAutoSize;
                da.ChildBecameAlive -= AddChild;
                da.ChildDied -= RemoveChild;
                da.ChildDepthChanged -= depthChanged;
            }

            if (TargetDrawable is FlowContainer<Drawable> df) df.OnLayout -= onLayout;
        }


        private void onAutoSize() => ActivityAutosize.FadeOutFromOne(1);

        private void onLayout() => ActivityLayout.FadeOutFromOne(1);

        private void onInvalidated(Drawable d) => ActivityInvalidate.FadeOutFromOne(1);

        private void depthChanged(Drawable drawable)
        {
            var vis = (VisualisedDrawable)Tree.GetVisualiserFor(drawable);

            vis.CurrentContainer?.RemoveVisualiser(vis);
            vis.CurrentContainer?.AddVisualiser(vis);
        }

        private void onDispose()
        {
            // May come from the disposal thread, in which case they won't ever be reused and the container doesn't need to be reset
            Schedule(() => SetContainer(null));
        }

        protected override void Dispose(bool isDisposing)
        {
            detachEvents();
            base.Dispose(isDisposing);
        }

        protected override void UpdateContent()
        {
            PreviewBox.Alpha = Math.Max(0.2f, TargetDrawable.Alpha);
            PreviewBox.Colour = TargetDrawable.Colour;

            int childCount = (TargetDrawable as CompositeDrawable)?.InternalChildren.Count ?? 0;

            Text.Text = TargetDrawable.ToString();
            Text2.Text = $"({TargetDrawable.DrawPosition.X:#,0},{TargetDrawable.DrawPosition.Y:#,0}) {TargetDrawable.DrawSize.X:#,0}x{TargetDrawable.DrawSize.Y:#,0}"
                            + (!IsExpanded && childCount > 0 ? $@" ({childCount} children)" : string.Empty);

            Alpha = (TargetDrawable?.IsPresent ?? true) ? 1f : 0.3f;
        }

        protected override void UpdateChildren()
        {
            if (TargetDrawable is CompositeDrawable compositeTarget)
                compositeTarget.AliveInternalChildren.ForEach(AddChild);
        }

        protected internal override ElementNode? AddChildVisualiser(object? element)
        {
            // Make sure to never add the DrawVisualiser (recursive scenario)
            // The user can still bypass it through inspecting properties
            // but there's not much we can do here.
            if (element == Tree.Visualiser) return null;

            // Don't add individual characters of SpriteText
            if (Target is SpriteText) return null;

            return base.AddChildVisualiser(element);
        }
    }
}
