// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace FlappyDon.Game.Tests.Visual
{
    public class TestSceneFlappyDonGame : TestScene
    {
        private FlappyDonGame game;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(FlappyDonGame),
        };

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            game = new FlappyDonGame();
            game.SetHost(host);

            Add(game);
        }
    }
}
