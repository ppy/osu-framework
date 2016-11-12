using System;
namespace osu.Framework.Allocation
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BackgroundDependencyLoader : Attribute
    {
        public bool PermitNulls { get; private set; }

        /// <summary>
        /// Marks this method as the initializer for a class in the context of dependency injection.
        /// </summary>
        /// <param name="permitNulls">If true, the initializer may be passed null for the dependencies we can't fulfill.</param>
        public BackgroundDependencyLoader(bool permitNulls = true)
        {
            PermitNulls = permitNulls;
        }
    }
}