//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Containers
{
    public class ClickableContainer : Container
    {
        public new event Func<bool> Click;

        protected override bool OnClick(InputState state)
        {
            if (Click?.Invoke() == true)
                return true;

            return base.OnClick(state);
        }
    }
}
