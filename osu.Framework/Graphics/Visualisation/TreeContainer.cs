// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
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

        protected override Container Content => scroll;

        const float width = 300;

        private Vector2 positionVisible => Vector2.Zero;
        private Vector2 positionInvisible => new Vector2(10 - width, 0);

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
                        Delay(1000, true);
                        MoveTo(positionInvisible, 300);
                        Delay(200, true);
                        scroll.FadeOut(100);
                        break;
                    case TreeContainerStatus.Onscreen:
                        if (loadMessage == null)
                            scroll.FadeIn(200);
                        MoveTo(positionVisible, 300);
                        break;
                }
            }
        }

        public TreeContainer()
        {
            Masking = true;
            RelativeSizeAxes = Axes.Y;
            Anchor = Anchor.TopRight;
            Origin = Anchor.TopRight;
            Size = new Vector2(width, 1);
            Position = positionVisible;
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
                    Alpha = 0
                },
                loadMessage = new SpriteText
                {
                    Text = @"Click to load DrawVisualiser",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                }
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

            Remove(loadMessage);
            loadMessage = null;

            scroll.FadeIn(500);

            BeginRun?.Invoke();

            return true;
        }
    }
}
