// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Testing;

namespace FlappyDon.Game.Tests.Visual
{
    public class TestSceneGameScreen : TestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(Pipe),
            typeof(PipeObstacle),
            typeof(Obstacles),
            typeof(BackdropSprite),
            typeof(GroundSprite),
            typeof(Backdrop),
            typeof(Bird)
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new GameScreen());
        }
    }
}
