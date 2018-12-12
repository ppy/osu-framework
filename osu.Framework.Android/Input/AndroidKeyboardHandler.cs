// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Android.Input
{
    public class AndroidKeyboardHandler : InputHandler
    {
        private readonly AndroidGameView view;

        public AndroidKeyboardHandler(AndroidGameView view)
        {
            this.view = view;
        }
        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host) => true;
    }
}
