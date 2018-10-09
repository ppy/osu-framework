using System.Reflection;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Testing.Drawables
{
    class AssemblyDropdown : BasicDropdown<Assembly>
    {
        public void AddAssembly(string name, Assembly assembly) => AddDropdownItem(name, assembly);
    }
}
