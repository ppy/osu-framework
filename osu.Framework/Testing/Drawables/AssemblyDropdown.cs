using System.Reflection;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Testing.Drawables
{
    public class AssemblyDropdown : BasicDropdown<Assembly>
    {
        public void AddAssembly(string name, Assembly assembly)
        {
            if(assembly == null) return;
            foreach(var item in MenuItems)
            {
                if(item.Text.Value.Contains("dynamic"))
                    RemoveDropdownItem(item.Value);
            }
            AddDropdownItem(name, assembly);
        }
    }
}
