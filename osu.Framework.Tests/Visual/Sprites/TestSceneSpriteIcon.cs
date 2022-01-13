﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    [TestFixture]
    public class TestSceneSpriteIcon : FrameworkTestScene
    {
        public TestSceneSpriteIcon()
        {
            FillFlowContainer flow;

            Add(new TooltipContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Teal,
                        RelativeSizeAxes = Axes.Both,
                    },
                    new BasicScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = flow = new FillFlowContainer
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Full,
                        },
                    }
                }
            });

            var weights = typeof(FontAwesome).GetNestedTypes();

            foreach (var w in weights)
            {
                flow.Add(new SpriteText
                {
                    Text = w.Name,
                    Scale = new Vector2(4),
                    RelativeSizeAxes = Axes.X,
                    Padding = new MarginPadding(10),
                });

                foreach (var p in w.GetProperties(BindingFlags.Public | BindingFlags.Static))
                {
                    var propValue = p.GetValue(null);
                    Debug.Assert(propValue != null);

                    flow.Add(new Icon($"{nameof(FontAwesome)}.{w.Name}.{p.Name}", (IconUsage)propValue));
                }
            }

            AddStep("toggle shadows", () => flow.Children.OfType<Icon>().ForEach(i => i.SpriteIcon.Shadow = !i.SpriteIcon.Shadow));
            AddStep("change icons", () => flow.Children.OfType<Icon>().ForEach(i => i.SpriteIcon.Icon = new IconUsage((char)(i.SpriteIcon.Icon.Icon + 1))));
        }

        private class Icon : Container, IHasTooltip
        {
            public LocalisableString TooltipText { get; }

            public SpriteIcon SpriteIcon { get; }

            public Icon(string name, IconUsage icon)
            {
                TooltipText = name;

                AutoSizeAxes = Axes.Both;
                Child = SpriteIcon = new SpriteIcon
                {
                    Icon = icon,
                    Size = new Vector2(60),
                };
            }
        }
    }
}
