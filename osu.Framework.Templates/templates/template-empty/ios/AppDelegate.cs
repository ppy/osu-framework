// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Foundation;
using osu.Framework.iOS;
using GameApplication = Template.Game.Application;
using ofGame = osu.Framework.Game;

namespace Template.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : GameAppDelegate
    {
        protected override ofGame CreateGame() => new GameApplication();
    }
}
