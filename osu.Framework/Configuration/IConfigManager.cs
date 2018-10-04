// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Configuration
{
    public interface IConfigManager
    {
        /// <summary>
        /// Loads this config.
        /// </summary>
        void Load();

        /// <summary>
        /// Saves this config.
        /// </summary>
        /// <returns>Whether the operation succeeded.</returns>
        bool Save();
    }
}
