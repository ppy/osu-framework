//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT License - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Lists
{
    interface IHasLifetime
    {
        double LifetimeStart { get; }
        double LifetimeEnd { get; }

        bool IsAlive { get; }
        bool LoadRequired { get; }
        bool RemoveWhenNotAlive { get; }

        void Load();
    }
}
