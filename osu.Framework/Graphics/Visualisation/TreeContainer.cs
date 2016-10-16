// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Visualisation
{
    enum TreeContainerStatus
    {
        Onscreen,
        Offscreen
    }

    internal class TreeContainer : Container, IStateful<TreeContainerStatus>
    {
        ScrollContainer scroll;
        private SpriteText loadMessage;

        public Action BeginRun;

        public Action ChooseTarget;

        protected override Container Content => scroll;

        const float width = 300;

        private TreeContainerStatus state;
        public TreeContainerStatus State
        {
            get
            {
                return state;
            }

            set
            {
                state = value;

                switch (state)
                {
                    case TreeContainerStatus.Offscreen:
                        Delay(500, true);
                        FadeTo(0.7f, 300);
                        break;
                    case TreeContainerStatus.Onscreen:
                        FadeIn(300);
                        break;
                }
            }
        }

        public TreeContainer()
        {
            Masking = true;
            Position = new Vector2(100, 100);
            RelativeSizeAxes = Axes.Y;
            Size = new Vector2(width, 0.7f);
            AddInternal(new Drawable[]
            {
                new Box
                {
                    Colour = new Color4(30, 30, 30, 240),
                    RelativeSizeAxes = Axes.Both,
                    Depth = 0
                },
                scroll = new ScrollContainer()
                {
                    Alpha = 0,
                    Padding = new MarginPadding { Top = 50 },
                },
                loadMessage = new SpriteText
                {
                    Text = @"Click to load DrawVisualiser",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Size = new Vector2(1, 40),
                    Children = new Drawable[]
                    {
                        new Box {
                            Colour = new Color4(20, 20, 20, 255),
                            RelativeSizeAxes = Axes.Both,
                        },
                        new Button
                        {
                            Colour = Color4.DarkGray,
                            RelativeSizeAxes = Axes.Y,
                            Size = new Vector2(100, 1),
                            Text = @"Choose Target",
                            Action = delegate {
                                EnsureLoaded();
                                ChooseTarget?.Invoke();
                            }
                        }
                    }
                },
                new CursorContainer(),
            });
        }

        protected override bool OnHover(InputState state)
        {
            State = TreeContainerStatus.Onscreen;
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            State = TreeContainerStatus.Offscreen;
            base.OnHoverLost(state);
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);

            State = TreeContainerStatus.Offscreen;
        }

        protected override bool OnClick(InputState state)
        {
            if (loadMessage == null)
                return false;

            EnsureLoaded();

            return true;
        }

        public void EnsureLoaded()
        {
            if (loadMessage == null) return;

            Remove(loadMessage);
            loadMessage = null;

            scroll.FadeIn(500);

            BeginRun?.Invoke();
        }
    }
}
