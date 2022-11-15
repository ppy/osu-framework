// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    [TestFixture]
    public class TestSceneSpriteIcon : FrameworkTestScene
    {
        private ScheduledDelegate? scheduledDelegate;

        [Test]
        public void TestOneIconAtATime()
        {
            FillFlowContainer flow = null!;
            Icon[] icons = null!;

            int i = 0;

            AddStep("prepare test", () =>
            {
                i = 0;
                icons = getAllIcons().ToArray();
                scheduledDelegate?.Cancel();

                Child = new TooltipContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new BasicScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Child = flow = new FillFlowContainer
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Spacing = new Vector2(5),
                                Direction = FillDirection.Full,
                            },
                        }
                    }
                };
            });

            AddStep("start adding icons", () =>
            {
                scheduledDelegate = Scheduler.AddDelayed(() =>
                {
                    flow.Add(icons[i++]);

                    if (++i > icons.Length - 1)
                        scheduledDelegate?.Cancel();
                }, 50, true);
            });
        }

        [Test]
        public void TestLoadAllIcons()
        {
            Box background = null!;
            FillFlowContainer flow = null!;

            AddStep("prepare", () =>
            {
                scheduledDelegate?.Cancel();

                Child = new TooltipContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        background = new Box
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
                                Spacing = new Vector2(5),
                                Direction = FillDirection.Full,
                            },
                        }
                    }
                };

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

                    foreach (var icon in getAllIconsForWeight(w))
                        flow.Add(icon);
                }
            });

            AddStep("toggle shadows", () => flow.Children.OfType<Icon>().ForEach(i => i.SpriteIcon.Shadow = !i.SpriteIcon.Shadow));
            AddStep("change icons", () => flow.Children.OfType<Icon>().ForEach(i => i.SpriteIcon.Icon = new IconUsage((char)(i.SpriteIcon.Icon.Icon + 1))));
            AddStep("white background", () => background.FadeColour(Color4.White, 200));
            AddStep("move shadow offset", () => flow.Children.OfType<Icon>().ForEach(i => i.SpriteIcon.ShadowOffset += Vector2.One));
            AddStep("change shadow colour", () => flow.Children.OfType<Icon>().ForEach(i => i.SpriteIcon.ShadowColour = Color4.Pink));
            AddStep("add new icon with colour and offset", () =>
                flow.Add(new Icon("FontAwesome.Regular.Handshake", FontAwesome.Regular.Handshake)
                {
                    SpriteIcon = { Shadow = true, ShadowColour = Color4.Orange, ShadowOffset = new Vector2(5, 1) }
                }));
        }

        private IEnumerable<Icon> getAllIcons()
        {
            var weights = typeof(FontAwesome).GetNestedTypes();

            foreach (var w in weights)
            {
                foreach (var i in getAllIconsForWeight(w))
                    yield return i;
            }
        }

        private static IEnumerable<Icon> getAllIconsForWeight(Type weight)
        {
            foreach (var p in weight.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                object? propValue = p.GetValue(null);
                Debug.Assert(propValue != null);

                yield return new Icon($"{nameof(FontAwesome)}.{weight.Name}.{p.Name}", (IconUsage)propValue);
            }
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
