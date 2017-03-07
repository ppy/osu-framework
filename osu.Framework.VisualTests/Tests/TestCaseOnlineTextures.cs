// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Allocation;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseOnlineTextures : TestCase
    {
        private FillFlowContainer flow;

        private int loadId = 55;
        private Game game;

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new ScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        flow = new FillFlowContainer()
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        }
                    }
                }
            };

            getNextAvatar();
        }

        [BackgroundDependencyLoader]
        private void load(Game game)
        {
            this.game = game;
        }

        private void getNextAvatar()
        {
            new Avatar(loadId).LoadAsync(game, flow.Add);

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

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            Texture = textures.Get($@"https://a.ppy.sh/{userId}");
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
