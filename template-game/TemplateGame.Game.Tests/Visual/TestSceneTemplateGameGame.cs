using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace TemplateGame.Game.Tests.Visual
{
    public class TestSceneTemplateGameGame : TestScene
    {
        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        private TemplateGameGame game;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(TemplateGameGame),
        };

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            game = new TemplateGameGame();
            game.SetHost(host);

            Add(game);
        }
    }
}
