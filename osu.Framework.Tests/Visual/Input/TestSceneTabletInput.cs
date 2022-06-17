// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

#if NET6_0_OR_GREATER
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers.Tablet;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneTabletInput : FrameworkTestScene
    {
        public TestSceneTabletInput()
        {
            var penButtonFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
            };

            var auxButtonFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y
            };

            for (int i = 0; i < 8; i++)
                penButtonFlow.Add(new PenButtonHandler(i));

            for (int i = 0; i < 16; i++)
                auxButtonFlow.Add(new AuxiliaryButtonHandler(i));

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Children = new[] { penButtonFlow, auxButtonFlow }
            };
        }

        [Resolved]
        private GameHost host { get; set; }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var tabletHandler = host.AvailableInputHandlers.OfType<OpenTabletDriverHandler>().FirstOrDefault();

            if (tabletHandler != null)
                AddToggleStep("toggle tablet handling", t => tabletHandler.Enabled.Value = t);
        }

        private class PenButtonHandler : CompositeDrawable
        {
            private readonly TabletPenButton button;
            private readonly Drawable background;

            public PenButtonHandler(int buttonIndex)
            {
                button = TabletPenButton.Primary + buttonIndex;

                Size = new Vector2(50);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGreen,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"B{buttonIndex + 1}"
                    }
                };
            }

            protected override bool OnTabletPenButtonPress(TabletPenButtonPressEvent e)
            {
                if (e.Button != button)
                    return base.OnTabletPenButtonPress(e);

                background.FadeIn(100, Easing.OutQuint);
                return true;
            }

            protected override void OnTabletPenButtonRelease(TabletPenButtonReleaseEvent e)
            {
                if (e.Button != button)
                {
                    base.OnTabletPenButtonRelease(e);
                    return;
                }

                background.FadeOut(100);
            }
        }

        private class AuxiliaryButtonHandler : CompositeDrawable
        {
            private readonly TabletAuxiliaryButton button;
            private readonly Drawable background;

            public AuxiliaryButtonHandler(int buttonIndex)
            {
                button = TabletAuxiliaryButton.Button1 + buttonIndex;

                Size = new Vector2(50);

                InternalChildren = new[]
                {
                    background = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.DarkGreen,
                        Alpha = 0,
                        Child = new Box { RelativeSizeAxes = Axes.Both }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"B{buttonIndex + 1}"
                    }
                };
            }

            protected override bool OnTabletAuxiliaryButtonPress(TabletAuxiliaryButtonPressEvent e)
            {
                if (e.Button != button)
                    return base.OnTabletAuxiliaryButtonPress(e);

                background.FadeIn(100, Easing.OutQuint);
                return true;
            }

            protected override void OnTabletAuxiliaryButtonRelease(TabletAuxiliaryButtonReleaseEvent e)
            {
                if (e.Button != button)
                {
                    base.OnTabletAuxiliaryButtonRelease(e);
                    return;
                }

                background.FadeOut(100);
            }
        }
    }
}
#endif
