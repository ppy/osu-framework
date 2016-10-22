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

        private void getNextAvatar()
        {
            flow.Add(new Avatar(loadId));
            loadId++;
            Scheduler.AddDelayed(getNextAvatar, 100);
        }
    }

    public class Avatar : Sprite
    {
        private readonly int userId;

        public Avatar(int userId)
        {
            this.userId = userId;
        }

        public override async void Load(BaseGame game)
        {
            base.Load(game);

            Texture texture = null;

            try
            {
                texture = await game.Textures.GetAsync($@"https://a.ppy.sh/{userId}");
            }
            catch { }


            Scheduler.Add(delegate
            {
                if (texture == null)
                {
                    Expire();
                    return;
                }

                Texture = texture;
                //override texture size
                Size = new Vector2(128);

                FadeInFromZero(500);
            });
        }
    }
}
