// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;

namespace osu.Framework.Localisation
{
    /// <summary>
    /// A Bindable string which stays up-to-date with the current locale choice for the specified key.
    /// </summary>
    public class LocalisedString : Bindable<string>
    {
        public readonly string Key;
        public LocalisedString(string key)
        {
            Key = key;
        }
    }
}
