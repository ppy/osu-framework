//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Lists
{
    public interface IHasLifetime
    {
        double LifetimeStart { get; }
        double LifetimeEnd { get; }

        bool IsAlive { get; }
        bool IsLoaded { get; }
        bool RemoveWhenNotAlive { get; }

        void Load();
    }
}
