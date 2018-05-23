// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Configuration
{
    public interface IUnbindable
    {
        /// <summary>
        /// Unbinds all bound events.
        /// </summary>
        void UnbindEvents();

        /// <summary>
        /// Unbinds all bound <see cref="IBindable"/>s.
        /// </summary>
        void UnbindBindings();

        /// <summary>
        /// Calls <see cref="UnbindEvents"/> and <see cref="UnbindBindings"/>
        /// </summary>
        void UnbindAll();
    }
}
