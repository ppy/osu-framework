// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Input.Events;

using osuTK;
using osuTK.Graphics;
using osuTK.Input;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// A tree node that represents some entity.
    /// </summary>
    internal abstract class ElementNode : TreeNode
    {
        public abstract object? Target { get; protected set; }

        protected SpriteText Text = null!;
        protected SpriteText Text2 = null!;
        protected Drawable PreviewBox = null!;
        protected Drawable? ActivityInvalidate;
        protected Drawable? ActivityAutosize;
        protected Drawable? ActivityLayout;

        protected Action<ElementNode>? OnAddVisualiser;
        protected Action<ElementNode>? OnRemoveVisualiser;

        public ElementNode(object? d)
        {
            Target = d;
        }

        protected virtual void LoadFrontDecorations()
        {
        }

        protected virtual Drawable CreatePreviewBox(object? target)
            => new Box
            {
                Colour = Color4.White,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
            };

        [BackgroundDependencyLoader]
        private void load()
        {
            LoadFrontDecorations();

            AddRange(new Drawable[]
            {
                PreviewBox = CreatePreviewBox(Target),
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5),
                    Margin = new MarginPadding { Left = 2f },
                    Children = new Drawable[]
                    {
                        Text = new SpriteText { Font = FrameworkFont.Regular },
                        Text2 = new SpriteText { Font = FrameworkFont.Regular },
                    }
                },
            });

            PreviewBox.Size = new Vector2(LINE_HEIGHT);

            UpdateSpecifics();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            UpdateChildren();
            UpdateColours();
        }

        public bool TopLevel
        {
            set => ConnectionContainer.Alpha = value ? 0 : 1;
        }

        protected abstract void UpdateChildren();

        public override void AddVisualiser(TreeNode visualiser)
        {
            if (visualiser is ElementNode element)
            {
                OnAddVisualiser?.Invoke(element);
            }

            base.AddVisualiser(visualiser);
        }

        public override void RemoveVisualiser(TreeNode visualiser)
        {
            if (visualiser is ElementNode element)
            {
                OnRemoveVisualiser?.Invoke(element);
            }

            base.RemoveVisualiser(visualiser);
        }

        protected override bool OnHover(HoverEvent e)
        {
            UpdateColours();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            UpdateColours();
            base.OnHoverLost(e);
        }

        protected override void UpdateColours()
        {
            base.UpdateColours();

            if (IsHighlighted)
            {
                Text.Colour = FrameworkColour.Blue;
                Text2.Colour = FrameworkColour.Blue;
            }
            else if (IsHovered)
            {
                Text.Colour = Color4.White;
                Text2.Colour = FrameworkColour.YellowGreen;
            }
            else
            {
                Text.Colour = Color4.White;
                Text2.Colour = FrameworkColour.YellowGreen;
            }
        }

        protected override void UpdateSpecifics()
        {
            Vector2 posInTree = ToSpaceOfOtherDrawable(Vector2.Zero, Tree);

            if (posInTree.Y < -PreviewBox.DrawHeight || posInTree.Y > Tree.DrawHeight)
            {
                Text.Text = string.Empty;
                Text2.Text = string.Empty;
                return;
            }

            UpdateContent();
        }

        protected abstract void UpdateContent();

        protected override void Update()
        {
            UpdateSpecifics();
            base.Update();
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == MouseButton.Right)
            {
                Tree.HighlightTarget(this);
                return true;
            }

            return false;
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            Tree.RequestTarget(Target);
            return true;
        }
    }
}
