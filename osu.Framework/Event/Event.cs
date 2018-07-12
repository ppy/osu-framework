// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Event
{
    /// <summary>
    /// The base abstract class of all event types.
    /// </summary>
    public abstract class Event
    {
        protected Event()
        {
        }

        /// <summary>
        /// Create a shallow clone of this object.
        /// </summary>
        /// <returns>A cloned object. Its type is the same as the original object.</returns>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
