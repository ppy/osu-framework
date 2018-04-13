// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Marks a method as the loader-Method of a <see cref="osu.Framework.Graphics.Drawable"/>, allowing for automatic injection of dependencies via the parameters of the method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class BackgroundDependencyLoader : Attribute
    {
        /// <summary>
        /// True if nulls are allowed to be passed to the method marked with this attribute.
        /// </summary>
        public bool PermitNulls { get; private set; }

        /// <summary>
        /// Marks this method as the initializer for a class in the context of dependency injection.
        /// </summary>
        /// <param name="permitNulls">If true, the initializer may be passed null for the dependencies we can't fulfill.</param>
        public BackgroundDependencyLoader(bool permitNulls = false)
        {
            PermitNulls = permitNulls;
        }
    }
}
