// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Reflection;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Testing.Drawables
{
    public class AssemblyDropdown : BasicDropdown<Assembly>
    {
        public void AddAssembly(string name, Assembly assembly) => AddDropdownItem(name, assembly);
    }
}
