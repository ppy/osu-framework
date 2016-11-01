// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using OpenTK;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseOnlineTextures : TestCase
    {
        public override string Name => @"Online Textures";

        private FlowContainer flow;

        int loadId = 55;
        private BaseGame game;

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new ScrollContainer
                {
                    Children = new Drawable[]
                    {
                        flow = new FlowContainer()
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        }
                    }
                }
            };

            getNextAvatar();
        }

        protected override void Load(BaseGame game)
        {
            this.game = game;
            base.Load(game);
        }

        private void getNextAvatar()
        {
            new Avatar(loadId).Preload(game, flow.Add);

            loadId++;
            Scheduler.AddDelayed(getNextAvatar, 400);
        }
    }

    public class Avatar : Sprite
    {
        private readonly int userId;

        public Avatar(int userId)
        {
            this.userId = userId;
        }

        protected override void Load(BaseGame game)
        {
            base.Load(game);
            Texture = game.Textures.Get($@"https://a.ppy.sh/{userId}");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (Texture == null)
            {
                Expire();
                return;
            }

            //override texture size
            Size = new Vector2(128);

            FadeInFromZero(500);
        }
    }
}
