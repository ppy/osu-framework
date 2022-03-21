// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Allocation;
using osu.Framework.Input.Events;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Used to seperate sections of the tree and to segregate nodes.
    /// </summary>
    internal class Spacer : TreeNode
    {
        protected SpriteText Text = null!;

        public string SpacerText
        {
            get => Text?.Text.ToString() ?? string.Empty;
            set
            {
                if (Text != null)
                    Text.Text = value;
                else
                    Schedule(() => Text!.Text = value);
            }
        }

        public new ElementNode? Child
        {
            get => Flow.Child as ElementNode;
            set
            {
                Flow.EnsureFlowMutationAllowed();
                foreach (var child in Flow)
                    child.SetContainer(null);
                value?.SetContainer(this);
            }
        }

        public void Add(ElementNode element)
        {
            element.SetContainer(this);
        }

        public void Remove(ElementNode element)
        {
            if (!Flow.Contains(element))
                throw new InvalidOperationException("Target element is not in this container");
            element.SetContainer(null);
        }

        protected override bool OnDoubleClick(DoubleClickEvent e) => true;

        public Spacer()
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    Text = new SpriteText {
                        Font = FrameworkFont.Regular,
                        Colour = FrameworkColour.Yellow,
                    },
                }
            });
        }
    }
}
