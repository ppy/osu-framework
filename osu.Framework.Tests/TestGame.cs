// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests
{
    internal class TestGame : Game
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new NamespacedResourceStore<byte[]>(new DllResourceStore(Assembly.GetExecutingAssembly().Location), "Resources"));
        }
    }
}
