// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Foundation;
using osu.Framework;
using osu.Framework.iOS;
using System;
using osuTK.Input;

namespace SampleGame.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : GameAppDelegate
    {
        protected override Game CreateGame() => new SampleGameGame();


        override public void fileDrop(object sender, FileDropEventArgs e)
        {
            Console.WriteLine("File Drop event fired");
            Console.WriteLine(e.FileName); // This can be used to test SampleGame.iOS filedrop events. 
        }
    }
}
