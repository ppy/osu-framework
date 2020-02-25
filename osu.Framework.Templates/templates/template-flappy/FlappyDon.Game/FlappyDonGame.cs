// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;

namespace FlappyDon.Game
{
    public class FlappyDonGame : FlappyDonGameBase
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            // Add the main screen to this container
            Add(new GameScreen());
        }
    }
}
