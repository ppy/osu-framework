// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestScenePlatformActionContainer : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new TestPlatformActionHandler
            {
                RelativeSizeAxes = Axes.Both
            });
        }

        private class TestPlatformActionHandler : CompositeDrawable
        {
            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(20),
                    Spacing = new Vector2(10),
                    ChildrenEnumerable = Enum.GetValues(typeof(PlatformAction))
                                             .Cast<PlatformAction>()
                                             .Select(action => new PlatformBindingBox(action))
                };
            }
        }

        private class PlatformBindingBox : CompositeDrawable, IKeyBindingHandler<PlatformAction>
        {
            private readonly PlatformAction platformAction;

            private Box background;

            public PlatformBindingBox(PlatformAction platformAction)
            {
                this.platformAction = platformAction;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;
                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.GreenDarker
                    },
                    new SpriteText
                    {
                        Text = platformAction.ToString(),
                        Margin = new MarginPadding(10)
                    }
                };
            }

            public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
            {
                if (e.Action != platformAction)
                    return false;

                background.FlashColour(FrameworkColour.YellowGreen, 250, Easing.OutQuint);
                return e.Action == PlatformAction.Exit;
            }

            public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
            {
            }
        }
    }
}
